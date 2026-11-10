using SchoolAdmission.Api.Services.Results;

namespace SchoolAdmission.Api.Services;

/// <summary>
/// Core domain service responsible for seat allocations, pessimistic concurrency locks,
/// capacity validation, and hold expiration cleanup.
/// </summary>
public interface ISeatAllocationService
{
    /// <summary>
    /// Atomically acquires a seat hold on the target class using pessimistic row-locking (SELECT ... FOR UPDATE).
    /// </summary>
    /// <param name="classId">Target class identifier (e.g. c_c6m).</param>
    /// <param name="studentId">17-digit student birth certificate identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>SeatHoldResult with either 201 Created entity or 409 Conflict / 404 Not Found error.</returns>
    Task<SeatHoldResult> CreateHoldAsync(string classId, string studentId, CancellationToken ct = default);

    /// <summary>
    /// Periodically sweeps and updates stale seat holds whose expires_at timestamp has passed.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of expired holds updated.</returns>
    Task<int> SweepExpiredHoldsAsync(CancellationToken ct = default);
}
