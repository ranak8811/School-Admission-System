namespace SchoolAdmission.Api.DTOs.Holds;

/// <summary>
/// Student application record item payload.
/// Matches PRD endpoint #8 response contract.
/// </summary>
public record StudentHoldItemResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    DateTime ExpiresAt
);
