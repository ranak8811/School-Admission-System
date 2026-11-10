using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs.Classes;
using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ClassesController> _logger;

    public ClassesController(AppDbContext dbContext, ILogger<ClassesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// POST /classes
    /// Creates a new class offering with seat capacity.
    /// PRD Endpoint #1.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateClassResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Class name cannot be empty."
            });
        }

        if (request.TotalSeats <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Total seats must be at least 1."
            });
        }

        var newClass = new ClassEntity
        {
            Id = "c_" + Guid.NewGuid().ToString("N")[..6],
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Classes.Add(newClass);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Created new class '{ClassName}' with ID {ClassId} and {TotalSeats} seats",
            newClass.Name, newClass.Id, newClass.TotalSeats);

        var response = new CreateClassResponse(newClass.Id, newClass.Name, newClass.TotalSeats);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// GET /classes
    /// Lists all classes with real-time dynamic seat calculations.
    /// PRD Endpoint #2.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClassDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClasses(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var classes = await _dbContext.Classes
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new ClassDetailResponse(
                c.Id,
                c.Name,
                c.TotalSeats,
                c.Holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now),
                c.Holds.Count(h => h.Status == HoldStatus.CONFIRMED),
                Math.Max(0, c.TotalSeats - (
                    c.Holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now) +
                    c.Holds.Count(h => h.Status == HoldStatus.CONFIRMED)
                ))
            ))
            .ToListAsync(ct);

        return Ok(classes);
    }

    /// <summary>
    /// GET /classes/{classId}
    /// Retrieves capacity metrics for a single class.
    /// PRD Endpoint #3.
    /// </summary>
    [HttpGet("{classId}")]
    [ProducesResponseType(typeof(ClassDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClassById(string classId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var classDetail = await _dbContext.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new ClassDetailResponse(
                c.Id,
                c.Name,
                c.TotalSeats,
                c.Holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now),
                c.Holds.Count(h => h.Status == HoldStatus.CONFIRMED),
                Math.Max(0, c.TotalSeats - (
                    c.Holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now) +
                    c.Holds.Count(h => h.Status == HoldStatus.CONFIRMED)
                ))
            ))
            .FirstOrDefaultAsync(ct);

        if (classDetail == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Class '{classId}' does not exist."
            });
        }

        return Ok(classDetail);
    }
}
