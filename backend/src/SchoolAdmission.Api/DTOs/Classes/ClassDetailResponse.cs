namespace SchoolAdmission.Api.DTOs.Classes;

/// <summary>
/// Detailed class capacity metric payload.
/// Matches PRD endpoint #2 and #3 response contracts.
/// </summary>
public record ClassDetailResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats
);
