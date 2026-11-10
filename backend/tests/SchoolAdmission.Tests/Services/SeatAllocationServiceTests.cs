using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;
using SchoolAdmission.Api.Services;
using Xunit;

namespace SchoolAdmission.Tests.Services;

public class SeatAllocationServiceTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private SeatAllocationService CreateService(AppDbContext dbContext, int holdDurationSeconds = 600)
    {
        var options = Options.Create(new AdmissionOptions
        {
            HoldDurationSeconds = holdDurationSeconds,
            SweeperIntervalSeconds = 30
        });

        return new SeatAllocationService(
            dbContext,
            options,
            NullLogger<SeatAllocationService>.Instance
        );
    }

    [Fact]
    public async Task CreateHoldAsync_WhenSeatsAvailable_ReservesSeatSuccessfully_AndSetsHoldStatusAndExpiration()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = CreateService(context, holdDurationSeconds: 600);

        var testClass = new ClassEntity
        {
            Id = "c_test1",
            Name = "Class 6 Morning",
            TotalSeats = 10
        };
        context.Classes.Add(testClass);
        await context.SaveChangesAsync();

        var studentId = "20152691234567890";

        // Act
        var result = await service.CreateHoldAsync(testClass.Id, studentId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Hold);
        Assert.StartsWith("h_", result.Hold.Id);
        Assert.Equal("c_test1", result.Hold.ClassId);
        Assert.Equal(studentId, result.Hold.StudentId);
        Assert.Equal(HoldStatus.HELD, result.Hold.Status);
        Assert.True(result.Hold.ExpiresAt > DateTime.UtcNow.AddMinutes(9));
        Assert.True(result.Hold.ExpiresAt <= DateTime.UtcNow.AddMinutes(11));

        // Verify entity persisted in database
        var savedHold = await context.SeatHolds.FindAsync(result.Hold.Id);
        Assert.NotNull(savedHold);
        Assert.Equal(HoldStatus.HELD, savedHold.Status);
    }

    [Fact]
    public async Task CreateHoldAsync_WhenSeatsExhausted_ReturnsConflict_AndDoesNotOversell()
    {
        // Arrange: Class has totalSeats = 1, already occupied by an active hold
        using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var testClass = new ClassEntity
        {
            Id = "c_test2",
            Name = "Class 6 Day",
            TotalSeats = 1
        };
        context.Classes.Add(testClass);

        var existingHold = new SeatHoldEntity
        {
            Id = "h_existing",
            ClassId = "c_test2",
            StudentId = "20152691234567801",
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        context.SeatHolds.Add(existingHold);
        await context.SaveChangesAsync();

        // Act: Second student attempts to reserve seat in full class
        var result = await service.CreateHoldAsync("c_test2", "20152691234567802", CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("No seats available", result.ErrorMessage);

        // Assert only 1 hold exists in DB (zero oversell)
        var totalHolds = await context.SeatHolds.CountAsync(h => h.ClassId == "c_test2");
        Assert.Equal(1, totalHolds);
    }

    [Fact]
    public async Task CreateHoldAsync_WhenStudentAlreadyHasActiveHoldInClass_ReturnsConflict()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var testClass = new ClassEntity
        {
            Id = "c_test3",
            Name = "Class 7 Morning",
            TotalSeats = 50
        };
        context.Classes.Add(testClass);

        var studentId = "20152691234567855";
        var activeHold = new SeatHoldEntity
        {
            Id = "h_student_active",
            ClassId = "c_test3",
            StudentId = studentId,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(8)
        };
        context.SeatHolds.Add(activeHold);
        await context.SaveChangesAsync();

        // Act: Same student attempts second application for same class
        var result = await service.CreateHoldAsync("c_test3", studentId, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("already has an active hold or confirmed seat", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateHoldAsync_WhenStudentAlreadyHasConfirmedSeatInClass_ReturnsConflict()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var testClass = new ClassEntity
        {
            Id = "c_test4",
            Name = "Class 8 Morning",
            TotalSeats = 50
        };
        context.Classes.Add(testClass);

        var studentId = "20152691234567866";
        var confirmedHold = new SeatHoldEntity
        {
            Id = "h_student_confirmed",
            ClassId = "c_test4",
            StudentId = studentId,
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-20)
        };
        context.SeatHolds.Add(confirmedHold);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CreateHoldAsync("c_test4", studentId, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("already has an active hold or confirmed seat", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateHoldAsync_WhenPreviousHoldExpired_AllowsStudentToHoldAgain()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var testClass = new ClassEntity
        {
            Id = "c_test5",
            Name = "Class 9 Morning",
            TotalSeats = 50
        };
        context.Classes.Add(testClass);

        var studentId = "20152691234567877";
        var expiredHold = new SeatHoldEntity
        {
            Id = "h_student_expired",
            ClassId = "c_test5",
            StudentId = studentId,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired 5 mins ago
        };
        context.SeatHolds.Add(expiredHold);
        await context.SaveChangesAsync();

        // Act: Student can apply again because the previous hold has expired
        var result = await service.CreateHoldAsync("c_test5", studentId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Hold);
        Assert.NotEqual("h_student_expired", result.Hold.Id);
    }

    [Fact]
    public async Task CreateHoldAsync_WhenClassDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        // Act
        var result = await service.CreateHoldAsync("c_nonexistent", "20152691234567899", CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task SweepExpiredHoldsAsync_TransitionsOnlyExpiredHoldsToEXPIRED()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var testClass = new ClassEntity { Id = "c_test6", Name = "Class 10", TotalSeats = 100 };
        context.Classes.Add(testClass);

        // Hold 1: Expired HELD (should be swept)
        var expiredHold = new SeatHoldEntity
        {
            Id = "h_sweep_1",
            ClassId = "c_test6",
            StudentId = "20152691234567801",
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-2)
        };

        // Hold 2: Active HELD (should NOT be swept)
        var activeHold = new SeatHoldEntity
        {
            Id = "h_sweep_2",
            ClassId = "c_test6",
            StudentId = "20152691234567802",
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        // Hold 3: Confirmed seat (should NOT be swept)
        var confirmedHold = new SeatHoldEntity
        {
            Id = "h_sweep_3",
            ClassId = "c_test6",
            StudentId = "20152691234567803",
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };

        context.SeatHolds.AddRange(expiredHold, activeHold, confirmedHold);
        await context.SaveChangesAsync();

        // Act
        var sweptCount = await service.SweepExpiredHoldsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, sweptCount);

        var refreshedExpired = await context.SeatHolds.FindAsync("h_sweep_1");
        var refreshedActive = await context.SeatHolds.FindAsync("h_sweep_2");
        var refreshedConfirmed = await context.SeatHolds.FindAsync("h_sweep_3");

        Assert.Equal(HoldStatus.EXPIRED, refreshedExpired!.Status);
        Assert.Equal(HoldStatus.HELD, refreshedActive!.Status);
        Assert.Equal(HoldStatus.CONFIRMED, refreshedConfirmed!.Status);
    }
}
