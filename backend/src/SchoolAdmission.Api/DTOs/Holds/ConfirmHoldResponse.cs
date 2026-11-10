namespace SchoolAdmission.Api.DTOs.Holds;

/// <summary>
/// Response payload returned after successfully confirming admission fee payment (HTTP 200).
/// Matches PRD endpoint #6 response contract.
/// </summary>
public record ConfirmHoldResponse(
    string HoldId,
    string Status
);
