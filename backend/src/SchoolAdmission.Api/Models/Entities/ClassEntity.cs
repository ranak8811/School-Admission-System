namespace SchoolAdmission.Api.Models.Entities;

/// <summary>
/// Represents a specific class and shift offering (e.g., 'Class 6 - Morning Shift').
/// Each shift functions as an independent class entity with distinct seat capacity.
/// </summary>
public class ClassEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for seat holds and admissions in this class
    public ICollection<SeatHoldEntity> Holds { get; set; } = new List<SeatHoldEntity>();
}
