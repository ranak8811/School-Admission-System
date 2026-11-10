namespace SchoolAdmission.Api.DTOs.Holds;

/// <summary>
/// Response payload returned after successfully reserving a seat hold (HTTP 201).
/// Matches PRD endpoint #4 response contract.
/// </summary>
public record CreateHoldResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    DateTime ExpiresAt
);
