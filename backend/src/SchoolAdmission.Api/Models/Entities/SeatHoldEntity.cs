using SchoolAdmission.Api.Models.Enums;

namespace SchoolAdmission.Api.Models.Entities;

/// <summary>
/// Represents an atomic seat reservation, confirmed admission, or cancelled hold.
/// </summary>
public class SeatHoldEntity
{
    public string Id { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public HoldStatus Status { get; set; } = HoldStatus.HELD;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property pointing to the parent class
    public ClassEntity? Class { get; set; }
}
