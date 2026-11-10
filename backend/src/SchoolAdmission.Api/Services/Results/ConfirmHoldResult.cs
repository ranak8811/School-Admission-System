using Microsoft.AspNetCore.Http;
using SchoolAdmission.Api.Models.Entities;

namespace SchoolAdmission.Api.Services.Results;

/// <summary>
/// Domain operation outcome for admission payment confirmation workflows.
/// </summary>
public record ConfirmHoldResult
{
    public bool IsSuccess { get; init; }
    public SeatHoldEntity? Hold { get; init; }
    public int StatusCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static ConfirmHoldResult Success(SeatHoldEntity hold) =>
        new()
        {
            IsSuccess = true,
            Hold = hold,
            StatusCode = StatusCodes.Status200OK
        };

    public static ConfirmHoldResult Conflict(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status409Conflict
        };

    public static ConfirmHoldResult NotFound(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status404NotFound
        };

    public static ConfirmHoldResult Forbidden(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status403Forbidden
        };

    public static ConfirmHoldResult BadRequest(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status400BadRequest
        };
}
