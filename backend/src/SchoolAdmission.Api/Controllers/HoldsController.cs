using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs.Holds;
using SchoolAdmission.Api.Models.Enums;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public class HoldsController : ControllerBase
{
    private static readonly Regex StudentIdRegex = new(@"^\d{17}$", RegexOptions.Compiled);

    private readonly AppDbContext _dbContext;
    private readonly ISeatAllocationService _seatAllocationService;
    private readonly ILogger<HoldsController> _logger;

    public HoldsController(
        AppDbContext dbContext,
        ISeatAllocationService seatAllocationService,
        ILogger<HoldsController> logger)
    {
        _dbContext = dbContext;
        _seatAllocationService = seatAllocationService;
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

    /// <summary>
    /// POST /holds/{holdId}/confirm
    /// Confirms admission and permanently finalizes seat confirmation.
    /// Idempotent: repeated calls safely return 200 OK without side effects.
    /// PRD Endpoint #6.
    /// </summary>
    [HttpPost("{holdId}/confirm")]
    [ProducesResponseType(typeof(ConfirmHoldResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmHold(
        string holdId,
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

        var result = await _seatAllocationService.ConfirmHoldAsync(holdId, studentId.Trim(), ct);

        if (!result.IsSuccess)
        {
            return result.StatusCode switch
            {
                StatusCodes.Status403Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = result.ErrorMessage
                }),
                StatusCodes.Status404NotFound => NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.ErrorMessage
                }),
                StatusCodes.Status409Conflict => Conflict(new ProblemDetails
                {
                    Title = "Conflict",
                    Status = StatusCodes.Status409Conflict,
                    Detail = result.ErrorMessage
                }),
                _ => StatusCode(result.StatusCode, new ProblemDetails
                {
                    Title = "Error",
                    Status = result.StatusCode,
                    Detail = result.ErrorMessage
                })
            };
        }

        var response = new ConfirmHoldResponse(result.Hold!.Id, result.Hold!.Status.ToString());
        return Ok(response);
    }

    /// <summary>
    /// DELETE /holds/{holdId}
    /// Cancels an active unconfirmed seat hold and releases the reserved seat back to the pool.
    /// PRD Endpoint #7.
    /// </summary>
    [HttpDelete("{holdId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelHold(
        string holdId,
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

        var result = await _seatAllocationService.CancelHoldAsync(holdId, studentId.Trim(), ct);

        if (!result.IsSuccess)
        {
            return result.StatusCode switch
            {
                StatusCodes.Status400BadRequest => BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = result.ErrorMessage
                }),
                StatusCodes.Status403Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = result.ErrorMessage
                }),
                StatusCodes.Status404NotFound => NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.ErrorMessage
                }),
                StatusCodes.Status409Conflict => Conflict(new ProblemDetails
                {
                    Title = "Conflict",
                    Status = StatusCodes.Status409Conflict,
                    Detail = result.ErrorMessage
                }),
                _ => StatusCode(result.StatusCode, new ProblemDetails
                {
                    Title = "Error",
                    Status = result.StatusCode,
                    Detail = result.ErrorMessage
                })
            };
        }

        return NoContent();
    }
}
