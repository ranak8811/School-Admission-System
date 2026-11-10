using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs.Holds;
using SchoolAdmission.Api.Models.Enums;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public class HoldsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<HoldsController> _logger;

    public HoldsController(AppDbContext dbContext, ILogger<HoldsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// GET /holds/{holdId}
    /// Retrieves the real-time status of a seat hold.
    /// Dynamically evaluates expiration: if status is HELD but expires_at has passed, returns EXPIRED.
    /// PRD Endpoint #5.
    /// </summary>
    [HttpGet("{holdId}")]
    [ProducesResponseType(typeof(HoldStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHoldById(string holdId, CancellationToken ct)
    {
        var hold = await _dbContext.SeatHolds
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == holdId, ct);

        if (hold == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Hold '{holdId}' does not exist."
            });
        }

        // Dynamic expiration evaluation:
        // If status is recorded as HELD in DB but current time is past expires_at,
        // dynamically report EXPIRED without requiring an immediate database write transaction.
        var effectiveStatus = (hold.Status == HoldStatus.HELD && DateTime.UtcNow > hold.ExpiresAt)
            ? "EXPIRED"
            : hold.Status.ToString();

        var expiresAtUtc = DateTime.SpecifyKind(hold.ExpiresAt, DateTimeKind.Utc);

        var response = new HoldStatusResponse(
            hold.Id,
            hold.ClassId,
            effectiveStatus,
            expiresAtUtc
        );

        return Ok(response);
    }
}
