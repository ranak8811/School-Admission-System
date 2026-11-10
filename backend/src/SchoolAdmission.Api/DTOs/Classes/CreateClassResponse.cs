namespace SchoolAdmission.Api.DTOs.Classes;

/// <summary>
/// Response payload returned after successfully creating a class (HTTP 201).
/// Matches PRD endpoint #1 response shape.
/// </summary>
public record CreateClassResponse(
    string Id,
    string Name,
    int TotalSeats
);
