namespace SchoolAdmission.Api.DTOs.Holds;

/// <summary>
/// Response payload for querying real-time hold status.
/// Matches PRD endpoint #5 response contract.
/// </summary>
public record HoldStatusResponse(
    string HoldId,
    string ClassId,
    string Status,
    DateTime ExpiresAt
);
