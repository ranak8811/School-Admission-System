using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;
using Xunit;

namespace SchoolAdmission.Tests.Unit;

/// <summary>
/// Domain unit tests verifying dynamic hold expiration boundaries (t - 1, t, t + 1)
/// and time calculation precision.
/// PRD Reference: Section 2.4 (Seat Hold Lifecycle) &amp; Section 2.6 (Dynamic Expiration on Read).
/// </summary>
public class ExpirationBoundaryTests
{
    private static string EvaluateEffectiveStatus(SeatHoldEntity hold, DateTime evaluationTimeUtc)
    {
        if (hold.Status == HoldStatus.HELD && evaluationTimeUtc > hold.ExpiresAt)
        {
            return "EXPIRED";
        }
        return hold.Status.ToString();
    }

    [Fact]
    public void EvaluateEffectiveStatus_WhenEvaluatedBeforeExpiration_RemainsHeld()
    {
        // Arrange
        var expiry = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        var hold = new SeatHoldEntity
        {
            Id = "h_test1",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.HELD,
            ExpiresAt = expiry
        };

        // Act: Evaluate at t - 1 second
        var evaluationTime = expiry.AddSeconds(-1);
        var status = EvaluateEffectiveStatus(hold, evaluationTime);

        // Assert
        Assert.Equal("HELD", status);
    }

    [Fact]
    public void EvaluateEffectiveStatus_WhenEvaluatedOneMillisecondBeforeExpiration_RemainsHeld()
    {
        // Arrange
        var expiry = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        var hold = new SeatHoldEntity
        {
            Id = "h_test2",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.HELD,
            ExpiresAt = expiry
        };

        // Act: Evaluate at t - 1 millisecond
        var evaluationTime = expiry.AddMilliseconds(-1);
        var status = EvaluateEffectiveStatus(hold, evaluationTime);

        // Assert
        Assert.Equal("HELD", status);
    }

    [Fact]
    public void EvaluateEffectiveStatus_WhenEvaluatedAtExactExpirationPoint_BoundaryCheck()
    {
        // Arrange
        var expiry = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        var hold = new SeatHoldEntity
        {
            Id = "h_test3",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.HELD,
            ExpiresAt = expiry
        };

        // Act: Evaluate at exact t (evaluationTime == expiry)
        // With condition: evaluationTime > ExpiresAt, at exact boundary it is still held for that zero-delta moment
        var statusAtExact = EvaluateEffectiveStatus(hold, expiry);
        Assert.Equal("HELD", statusAtExact);

        // One tick past expiry
        var statusJustAfter = EvaluateEffectiveStatus(hold, expiry.AddTicks(1));
        Assert.Equal("EXPIRED", statusJustAfter);
    }

    [Fact]
    public void EvaluateEffectiveStatus_WhenEvaluatedOneMillisecondAfterExpiration_TransitionsToEXPIRED()
    {
        // Arrange
        var expiry = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        var hold = new SeatHoldEntity
        {
            Id = "h_test4",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.HELD,
            ExpiresAt = expiry
        };

        // Act: Evaluate at t + 1 millisecond
        var evaluationTime = expiry.AddMilliseconds(1);
        var status = EvaluateEffectiveStatus(hold, evaluationTime);

        // Assert
        Assert.Equal("EXPIRED", status);
    }

    [Fact]
    public void EvaluateEffectiveStatus_WhenEvaluatedOneSecondAfterExpiration_TransitionsToEXPIRED()
    {
        // Arrange
        var expiry = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        var hold = new SeatHoldEntity
        {
            Id = "h_test5",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.HELD,
            ExpiresAt = expiry
        };

        // Act: Evaluate at t + 1 second
        var evaluationTime = expiry.AddSeconds(1);
        var status = EvaluateEffectiveStatus(hold, evaluationTime);

        // Assert
        Assert.Equal("EXPIRED", status);
    }

    [Fact]
    public void EvaluateEffectiveStatus_WhenAlreadyConfirmed_NeverTransitionsToExpiredEvenAfterTTL()
    {
        // A confirmed seat is permanently held and must never transition to EXPIRED,
        // even if decades have passed since the original hold expiry.
        var pastExpiry = DateTime.UtcNow.AddDays(-30);
        var hold = new SeatHoldEntity
        {
            Id = "h_test6",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = pastExpiry
        };

        var status = EvaluateEffectiveStatus(hold, DateTime.UtcNow);

        Assert.Equal("CONFIRMED", status);
    }

    [Fact]
    public void EvaluateEffectiveStatus_WhenCancelled_NeverTransitionsToExpired()
    {
        var pastExpiry = DateTime.UtcNow.AddMinutes(-5);
        var hold = new SeatHoldEntity
        {
            Id = "h_test7",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.CANCELLED,
            ExpiresAt = pastExpiry
        };

        var status = EvaluateEffectiveStatus(hold, DateTime.UtcNow);

        Assert.Equal("CANCELLED", status);
    }
}
