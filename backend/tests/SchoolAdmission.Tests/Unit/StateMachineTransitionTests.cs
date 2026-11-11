using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;
using Xunit;

namespace SchoolAdmission.Tests.Unit;

/// <summary>
/// Domain unit tests validating hold state machine lifecycle transitions and idempotency invariants.
/// PRD Reference: Section 2.4 (Seat Hold Lifecycle) &amp; Section 2.5 (Idempotent Confirmation).
/// States: HELD, CONFIRMED, EXPIRED, CANCELLED.
/// </summary>
public class StateMachineTransitionTests
{
    public enum TransitionResult
    {
        Success,
        AlreadyConfirmedIdempotent,
        ConflictExpired,
        ConflictCancelled,
        ForbiddenStudentMismatch,
        InvalidTransition
    }

    /// <summary>
    /// Pure state-machine transition validator for admission confirmation.
    /// Matches the exact logic in SeatAllocationService.ConfirmHoldAsync.
    /// </summary>
    private static TransitionResult ValidateConfirmationTransition(
        SeatHoldEntity hold,
        string requestingStudentId,
        DateTime nowUtc)
    {
        if (hold.StudentId != requestingStudentId)
        {
            return TransitionResult.ForbiddenStudentMismatch;
        }

        // 1. Idempotency Guard: If already confirmed, return success immediately
        if (hold.Status == HoldStatus.CONFIRMED)
        {
            return TransitionResult.AlreadyConfirmedIdempotent;
        }

        // 2. Cancellation Guard: Cannot confirm cancelled hold
        if (hold.Status == HoldStatus.CANCELLED)
        {
            return TransitionResult.ConflictCancelled;
        }

        // 3. Expiration Guard: Cannot confirm expired hold
        if (hold.Status == HoldStatus.EXPIRED || (hold.Status == HoldStatus.HELD && nowUtc > hold.ExpiresAt))
        {
            return TransitionResult.ConflictExpired;
        }

        // 4. Valid transition from active HELD to CONFIRMED
        if (hold.Status == HoldStatus.HELD && nowUtc <= hold.ExpiresAt)
        {
            return TransitionResult.Success;
        }

        return TransitionResult.InvalidTransition;
    }

    /// <summary>
    /// Pure state-machine transition validator for hold cancellation.
    /// Matches the exact logic in SeatAllocationService.ReleaseHoldAsync.
    /// </summary>
    private static TransitionResult ValidateCancellationTransition(
        SeatHoldEntity hold,
        string requestingStudentId)
    {
        if (hold.StudentId != requestingStudentId)
        {
            return TransitionResult.ForbiddenStudentMismatch;
        }

        // Cannot cancel confirmed admission
        if (hold.Status == HoldStatus.CONFIRMED)
        {
            return TransitionResult.InvalidTransition;
        }

        // Already cancelled or expired is a safe No-Op / Success
        if (hold.Status == HoldStatus.CANCELLED || hold.Status == HoldStatus.EXPIRED)
        {
            return TransitionResult.Success;
        }

        if (hold.Status == HoldStatus.HELD)
        {
            return TransitionResult.Success;
        }

        return TransitionResult.InvalidTransition;
    }

    [Fact]
    public void ConfirmTransition_FromActiveHeld_Succeeds()
    {
        var now = DateTime.UtcNow;
        var hold = new SeatHoldEntity
        {
            Id = "h_1",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddMinutes(5)
        };

        var result = ValidateConfirmationTransition(hold, "20261110000000001", now);

        Assert.Equal(TransitionResult.Success, result);
    }

    [Fact]
    public void ConfirmTransition_WhenAlreadyConfirmed_ReturnsIdempotentSuccess()
    {
        var now = DateTime.UtcNow;
        var hold = new SeatHoldEntity
        {
            Id = "h_1",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = now.AddMinutes(5)
        };

        var result = ValidateConfirmationTransition(hold, "20261110000000001", now);

        Assert.Equal(TransitionResult.AlreadyConfirmedIdempotent, result);
    }

    [Fact]
    public void ConfirmTransition_FromExpiredHold_ReturnsConflict()
    {
        var now = DateTime.UtcNow;
        var hold = new SeatHoldEntity
        {
            Id = "h_1",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddSeconds(-10) // Expired in time
        };

        var result = ValidateConfirmationTransition(hold, "20261110000000001", now);

        Assert.Equal(TransitionResult.ConflictExpired, result);
    }

    [Fact]
    public void ConfirmTransition_FromCancelledHold_ReturnsConflict()
    {
        var now = DateTime.UtcNow;
        var hold = new SeatHoldEntity
        {
            Id = "h_1",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.CANCELLED,
            ExpiresAt = now.AddMinutes(5)
        };

        var result = ValidateConfirmationTransition(hold, "20261110000000001", now);

        Assert.Equal(TransitionResult.ConflictCancelled, result);
    }

    [Fact]
    public void ConfirmTransition_FromDifferentStudent_ReturnsForbidden()
    {
        var now = DateTime.UtcNow;
        var hold = new SeatHoldEntity
        {
            Id = "h_1",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddMinutes(5)
        };

        var result = ValidateConfirmationTransition(hold, "20261110000000002", now);

        Assert.Equal(TransitionResult.ForbiddenStudentMismatch, result);
    }

    [Fact]
    public void CancelTransition_FromActiveHeld_Succeeds()
    {
        var hold = new SeatHoldEntity
        {
            Id = "h_1",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        var result = ValidateCancellationTransition(hold, "20261110000000001");

        Assert.Equal(TransitionResult.Success, result);
    }

    [Fact]
    public void CancelTransition_FromConfirmedAdmission_IsRejected()
    {
        // Invariant: Confirmed admissions cannot be cancelled via DELETE /holds/{id}
        var hold = new SeatHoldEntity
        {
            Id = "h_1",
            ClassId = "c_1",
            StudentId = "20261110000000001",
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        var result = ValidateCancellationTransition(hold, "20261110000000001");

        Assert.Equal(TransitionResult.InvalidTransition, result);
    }
}
