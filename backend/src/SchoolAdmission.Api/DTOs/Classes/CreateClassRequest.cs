using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Api.DTOs.Classes;

/// <summary>
/// Payload contract for creating a new class or shift.
/// </summary>
public record CreateClassRequest(
    [Required(ErrorMessage = "Class name is required.")]
    [MinLength(1, ErrorMessage = "Class name cannot be empty.")]
    string Name,

    [Range(1, int.MaxValue, ErrorMessage = "Total seats must be at least 1.")]
    int TotalSeats
);
