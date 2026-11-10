using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;
using SchoolAdmission.Api.Services.Results;

namespace SchoolAdmission.Api.Services;

/// <summary>
/// Domain service orchestrating seat hold reservations with pessimistic row-level locking.
/// Protects against race conditions and seat overselling across distributed instances.
/// </summary>
public class SeatAllocationService : ISeatAllocationService
{
    private readonly AppDbContext _dbContext;
    private readonly AdmissionOptions _options;
    private readonly ILogger<SeatAllocationService> _logger;

    public SeatAllocationService(
        AppDbContext dbContext,
        IOptions<AdmissionOptions> options,
        ILogger<SeatAllocationService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SeatHoldResult> CreateHoldAsync(string classId, string studentId, CancellationToken ct = default)
    {
        // 1. Begin atomic transaction when using relational database (PostgreSQL)
        var isRelational = _dbContext.Database.IsRelational();
        await using var tx = isRelational
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct)
            : null;

        try
        {
            // 2. Acquire PostgreSQL pessimistic row-level lock on the target class row.
            // Under concurrent requests, this serializes transactions touching the same class.
            ClassEntity? targetClass;
            if (isRelational)
            {
                targetClass = await _dbContext.Classes
                    .FromSqlInterpolated($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
                    .SingleOrDefaultAsync(ct);
            }
            else
            {
                // Fallback for in-memory provider unit tests
                targetClass = await _dbContext.Classes
                    .SingleOrDefaultAsync(c => c.Id == classId, ct);
            }

            if (targetClass == null)
            {
                if (tx != null) await tx.RollbackAsync(ct);
                return SeatHoldResult.NotFound($"Class '{classId}' does not exist.");
            }

            var now = DateTime.UtcNow;

            // 3. Invariant Check: Check if this student already holds or confirmed a seat in this class
            var alreadyEnrolled = await _dbContext.SeatHolds
                .AnyAsync(h => h.ClassId == classId &&
                               h.StudentId == studentId &&
                               (h.Status == HoldStatus.CONFIRMED ||
                               (h.Status == HoldStatus.HELD && h.ExpiresAt > now)), ct);

            if (alreadyEnrolled)
            {
                if (tx != null) await tx.RollbackAsync(ct);
                return SeatHoldResult.Conflict("Student already has an active hold or confirmed seat in this class.");
            }

            // 4. Capacity Calculation: Count unexpired active holds and confirmed seats
            var heldCount = await _dbContext.SeatHolds
                .CountAsync(h => h.ClassId == classId &&
                                 h.Status == HoldStatus.HELD &&
                                 h.ExpiresAt > now, ct);

            var confirmedCount = await _dbContext.SeatHolds
                .CountAsync(h => h.ClassId == classId &&
                                 h.Status == HoldStatus.CONFIRMED, ct);

            // Invariant: held + confirmed must be strictly less than totalSeats
            if (heldCount + confirmedCount >= targetClass.TotalSeats)
            {
                if (tx != null) await tx.RollbackAsync(ct);
                _logger.LogWarning("Seat hold rejected for student {StudentId} in class {ClassId}. Class full ({Held} held + {Confirmed} confirmed >= {Total} total)",
                    studentId, classId, heldCount, confirmedCount, targetClass.TotalSeats);
                return SeatHoldResult.Conflict("No seats available in this class.");
            }

            // 5. Reserve hold with configured duration
            var holdDuration = TimeSpan.FromSeconds(_options.HoldDurationSeconds > 0 ? _options.HoldDurationSeconds : 600);
            var newHold = new SeatHoldEntity
            {
                Id = "h_" + Guid.NewGuid().ToString("N")[..8],
                ClassId = classId,
                StudentId = studentId,
                Status = HoldStatus.HELD,
                ExpiresAt = now.Add(holdDuration),
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.SeatHolds.Add(newHold);
            await _dbContext.SaveChangesAsync(ct);
            if (tx != null) await tx.CommitAsync(ct);

            _logger.LogInformation("Seat hold {HoldId} reserved for student {StudentId} in class {ClassId}. Expires at {ExpiresAt}",
                newHold.Id, newHold.StudentId, newHold.ClassId, newHold.ExpiresAt);

            return SeatHoldResult.Success(newHold);
        }
        catch (Exception ex)
        {
            if (tx != null) await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Unexpected error occurred while allocating seat for student {StudentId} in class {ClassId}",
                studentId, classId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> SweepExpiredHoldsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        if (_dbContext.Database.IsRelational())
        {
            // Direct bulk UPDATE query generated by EF Core 8:
            // UPDATE seat_holds SET status = 'EXPIRED', updated_at = @now WHERE status = 'HELD' AND expires_at <= @now
            var updatedCount = await _dbContext.SeatHolds
                .Where(h => h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(h => h.Status, HoldStatus.EXPIRED)
                    .SetProperty(h => h.UpdatedAt, now), ct);

            if (updatedCount > 0)
            {
                _logger.LogInformation("Expired hold sweeper cleaned up {Count} expired seat holds", updatedCount);
            }

            return updatedCount;
        }
        else
        {
            // Fallback for InMemory provider in unit tests
            var staleHolds = await _dbContext.SeatHolds
                .Where(h => h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
                .ToListAsync(ct);

            foreach (var hold in staleHolds)
            {
                hold.Status = HoldStatus.EXPIRED;
                hold.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(ct);

            if (staleHolds.Count > 0)
            {
                _logger.LogInformation("Expired hold sweeper cleaned up {Count} expired seat holds (in-memory)", staleHolds.Count);
            }

            return staleHolds.Count;
        }
    }
}
