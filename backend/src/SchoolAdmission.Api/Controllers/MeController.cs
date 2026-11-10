using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs.Holds;
using SchoolAdmission.Api.Models.Enums;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me")]
public class MeController : ControllerBase
{
    private static readonly Regex StudentIdRegex = new(@"^\d{17}$", RegexOptions.Compiled);

    private readonly AppDbContext _dbContext;
    private readonly ILogger<MeController> _logger;

    public MeController(AppDbContext dbContext, ILogger<MeController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// GET /me/holds
    /// Returns all admission seat holds and application history associated with the requesting student.
    /// PRD Endpoint #8.
    /// </summary>
    [HttpGet("holds")]
    [ProducesResponseType(typeof(IEnumerable<StudentHoldItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyHolds(
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(studentId) || !StudentIdRegex.IsMatch(studentId.Trim()))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Header X-Student-Id is missing or must be 17 digits."
            });
        }

        var now = DateTime.UtcNow;

        var holds = await _dbContext.SeatHolds
            .AsNoTracking()
            .Include(h => h.Class)
            .Where(h => h.StudentId == studentId.Trim())
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new StudentHoldItemResponse(
                h.Id,
                h.ClassId,
                h.Class != null ? h.Class.Name : string.Empty,
                (h.Status == HoldStatus.HELD && now > h.ExpiresAt) ? "EXPIRED" : h.Status.ToString(),
                DateTime.SpecifyKind(h.ExpiresAt, DateTimeKind.Utc)
            ))
            .ToListAsync(ct);

        _logger.LogInformation("Retrieved {Count} application records for student {StudentId}", holds.Count, studentId);

        return Ok(holds);
    }
}
