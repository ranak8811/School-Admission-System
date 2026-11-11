using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace SchoolAdmission.Tests.Unit;

/// <summary>
/// Domain unit tests validating 17-digit Student ID regex parsing, edge cases, and performance.
/// PRD Reference: Section 2.4 (X-Student-Id Header) &amp; Section 1.5 (Student ID Format).
/// Rule: Must be exactly 17 numerical digits (e.g. Bangladesh 17-digit Birth Registration Number).
/// </summary>
public class StudentIdValidationTests
{
    private static readonly Regex StudentIdRegex = new(@"^\d{17}$", RegexOptions.Compiled);

    public static bool IsValidStudentId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        return StudentIdRegex.IsMatch(input.Trim());
    }

    [Theory]
    [InlineData("20261110000000001")]
    [InlineData("00000000000000000")]
    [InlineData("99999999999999999")]
    [InlineData("12345678901234567")]
    [InlineData(" 20261110000000001 ")] // With leading/trailing whitespace after trim
    public void IsValidStudentId_Valid17DigitStrings_ReturnsTrue(string validId)
    {
        // Act
        var result = IsValidStudentId(validId);

        // Assert
        Assert.True(result, $"Student ID '{validId}' should be valid.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1")]
    [InlineData("1234567890123456")] // 16 digits (too short)
    [InlineData("123456789012345678")] // 18 digits (too long)
    [InlineData("2026111000000000A")] // Contains letter
    [InlineData("2026-11-100000000")] // Contains dash
    [InlineData("2026 1110 0000 001")] // Contains spaces in between
    [InlineData("abcdefghijklmnopq")] // 17 non-digit characters
    [InlineData("!@#$%^&*()!@#$%^&")] // 17 special characters
    [InlineData("202611100000000.1")] // Contains dot
    [InlineData("-2026111000000001")] // Negative sign
    public void IsValidStudentId_InvalidInputs_ReturnsFalse(string? invalidId)
    {
        // Act
        var result = IsValidStudentId(invalidId);

        // Assert
        Assert.False(result, $"Student ID '{invalidId}' should be rejected as invalid.");
    }

    [Fact]
    public void RegexCompiled_ExecutesTenThousandValidations_InLessThanFiftyMilliseconds()
    {
        // Unit test speed requirement: High-throughput regex must not bottleneck request pipeline
        const int iterations = 10_000;
        const string testId = "20261110000000001";

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var isValid = StudentIdRegex.IsMatch(testId);
            Assert.True(isValid);
        }

        stopwatch.Stop();

        // 10,000 matches should take < 50ms (usually 2-5ms on modern CPUs)
        Assert.True(stopwatch.ElapsedMilliseconds < 50,
            $"10,000 regex matches took {stopwatch.ElapsedMilliseconds}ms, which exceeds 50ms.");
    }
}
