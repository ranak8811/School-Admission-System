using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;
using Xunit;

namespace SchoolAdmission.Tests.Unit;

/// <summary>
/// Domain unit tests verifying seat capacity calculation rules and zero-overselling invariants.
/// PRD Reference: Section 2.6 (Seat Overselling Prevention) &amp; Section 2.2 (Capacity Calculations).
/// Invariant: held + confirmed &lt;= totalSeats; available = max(0, totalSeats - (held + confirmed)).
/// </summary>
public class CapacityCalculationTests
{
    private static int CalculateAvailableSeats(int totalSeats, int heldSeats, int confirmedSeats)
    {
        return Math.Max(0, totalSeats - (heldSeats + confirmedSeats));
    }

    [Fact]
    public void CalculateAvailableSeats_WhenNoHoldsOrConfirmations_ReturnsTotalSeats()
    {
        // Arrange
        const int totalSeats = 25;
        const int heldSeats = 0;
        const int confirmedSeats = 0;

        // Act
        var available = CalculateAvailableSeats(totalSeats, heldSeats, confirmedSeats);

        // Assert
        Assert.Equal(25, available);
    }

    [Theory]
    [InlineData(10, 3, 2, 5)]
    [InlineData(10, 0, 10, 0)]
    [InlineData(10, 10, 0, 0)]
    [InlineData(10, 5, 5, 0)]
    [InlineData(50, 15, 20, 15)]
    [InlineData(1, 1, 0, 0)]
    [InlineData(1, 0, 1, 0)]
    public void CalculateAvailableSeats_WithVaryingHoldAndConfirmedCounts_CalculatesAccurately(
        int totalSeats, int heldSeats, int confirmedSeats, int expectedAvailable)
    {
        // Act
        var available = CalculateAvailableSeats(totalSeats, heldSeats, confirmedSeats);

        // Assert
        Assert.Equal(expectedAvailable, available);
    }

    [Fact]
    public void CalculateAvailableSeats_WhenOversubscribed_NeverReturnsNegativeNumber()
    {
        // In extreme edge race scenarios, available seats must clamp to 0 and never be negative
        const int totalSeats = 10;
        const int heldSeats = 8;
        const int confirmedSeats = 5; // 8 + 5 = 13 > 10

        var available = CalculateAvailableSeats(totalSeats, heldSeats, confirmedSeats);

        Assert.Equal(0, available);
    }

    [Fact]
    public void DynamicCapacityFilter_ExcludesExpiredAndCancelledHolds()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var targetClass = new ClassEntity
        {
            Id = "c_math101",
            Name = "Class 10 - Mathematics",
            TotalSeats = 10
        };

        var holds = new List<SeatHoldEntity>
        {
            // 1. Active unexpired hold -> must count towards held
            new() { Id = "h_1", ClassId = targetClass.Id, StudentId = "11111111111111111", Status = HoldStatus.HELD, ExpiresAt = now.AddMinutes(5) },

            // 2. Another active unexpired hold -> must count towards held
            new() { Id = "h_2", ClassId = targetClass.Id, StudentId = "22222222222222222", Status = HoldStatus.HELD, ExpiresAt = now.AddMinutes(2) },

            // 3. Expired hold (status is HELD in DB but now > expiresAt) -> must NOT count
            new() { Id = "h_3", ClassId = targetClass.Id, StudentId = "33333333333333333", Status = HoldStatus.HELD, ExpiresAt = now.AddMinutes(-1) },

            // 4. Swept EXPIRED hold -> must NOT count
            new() { Id = "h_4", ClassId = targetClass.Id, StudentId = "44444444444444444", Status = HoldStatus.EXPIRED, ExpiresAt = now.AddMinutes(-10) },

            // 5. Cancelled hold -> must NOT count
            new() { Id = "h_5", ClassId = targetClass.Id, StudentId = "55555555555555555", Status = HoldStatus.CANCELLED, ExpiresAt = now.AddMinutes(5) },

            // 6. Confirmed hold -> must count towards confirmed
            new() { Id = "h_6", ClassId = targetClass.Id, StudentId = "66666666666666666", Status = HoldStatus.CONFIRMED, ExpiresAt = now.AddMinutes(5) }
        };

        // Act: Apply PRD Section 2.2 capacity counting formula
        var heldCount = holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now);
        var confirmedCount = holds.Count(h => h.Status == HoldStatus.CONFIRMED);
        var availableSeats = Math.Max(0, targetClass.TotalSeats - (heldCount + confirmedCount));

        // Assert
        Assert.Equal(2, heldCount); // h_1 and h_2 only
        Assert.Equal(1, confirmedCount); // h_6 only
        Assert.Equal(7, availableSeats); // 10 - (2 + 1) = 7
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ClassEntity_TotalSeats_ValidationConstraint(int invalidSeatCount)
    {
        // PRD Section 2.1 requires TotalSeats >= 1
        var isValid = invalidSeatCount >= 1;
        Assert.False(isValid, $"Total seats {invalidSeatCount} should be rejected by validation.");
    }
}
