using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Api.DTOs.Classes;
using SchoolAdmission.Api.DTOs.Holds;
using SchoolAdmission.Tests.Integration;
using Xunit;

namespace SchoolAdmission.Tests.Concurrency;

/// <summary>
/// Mandatory Automated High-Concurrency Stress Test proving Zero Seat Overselling.
/// PRD Reference: Section 2.7 (Concurrency Testing Mandate) &amp; Section 2.6 (Zero Overselling Guarantee).
/// 
/// Mathematical Proof:
/// For any class with totalSeats = N:
/// When M concurrent workers (M &gt;&gt; N, e.g. M = 100, N = 10) simultaneously request a seat hold,
/// exactly N requests must succeed (HTTP 201), exactly (M - N) requests must be rejected (HTTP 409),
/// and final database state must satisfy: heldSeats == N, availableSeats == 0, and heldSeats + confirmedSeats &lt;= totalSeats.
/// </summary>
public class HighConcurrencyStressTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public HighConcurrencyStressTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData(10, 50)]  // 50 concurrent students competing for 10 seats
    [InlineData(10, 100)] // 100 concurrent students competing for 10 seats (10x over-subscription)
    public async Task ConcurrentHoldRequests_NeverOversell_ExactlyMatchesCapacity(int totalSeats, int totalConcurrentWorkers)
    {
        // 1. Arrange: Create a class with a fixed small capacity (e.g. 10 seats)
        var className = $"Concurrency Stress Class ({totalSeats} seats vs {totalConcurrentWorkers} workers)";
        var createClassRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest(className, totalSeats));
        Assert.Equal(HttpStatusCode.Created, createClassRes.StatusCode);

        var createdClass = await createClassRes.Content.ReadFromJsonAsync<CreateClassResponse>(JsonOptions);
        Assert.NotNull(createdClass);
        var classId = createdClass.Id;

        // Verify initial catalog state
        var initialClassDetail = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classId}", JsonOptions);
        Assert.NotNull(initialClassDetail);
        Assert.Equal(totalSeats, initialClassDetail.TotalSeats);
        Assert.Equal(0, initialClassDetail.HeldSeats);
        Assert.Equal(totalSeats, initialClassDetail.AvailableSeats);

        // 2. Prepare 50-100 distinct student IDs (valid 17-digit birth registration numbers)
        var studentIds = Enumerable.Range(1, totalConcurrentWorkers)
            .Select(i => $"20261110{i:D9}") // e.g. "20261110000000001", "20261110000000002", etc.
            .ToList();

        // 3. Setup a TaskCompletionSource Synchronization Gate
        // Ensures all workers release and fire their HTTP request at the exact same microsecond, maximizing contention!
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var results = new ConcurrentBag<(string StudentId, HttpStatusCode StatusCode, string? HoldId)>();

        var workerTasks = studentIds.Select(async studentId =>
        {
            // Wait at the starting gate
            await startGate.Task;

            // Fire HTTP POST /classes/{id}/holds
            using var requestMsg = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
            requestMsg.Headers.Add("X-Student-Id", studentId);

            var response = await _client.SendAsync(requestMsg);
            string? holdId = null;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var holdData = await response.Content.ReadFromJsonAsync<CreateHoldResponse>(JsonOptions);
                holdId = holdData?.HoldId;
            }

            results.Add((studentId, response.StatusCode, holdId));
        }).ToList();

        // 4. Act: Release the starting gate to fire all requests simultaneously
        var stopwatch = Stopwatch.StartNew();
        startGate.SetResult();

        // Await all concurrent tasks
        await Task.WhenAll(workerTasks);
        stopwatch.Stop();

        // 5. Assert: Evaluate Results
        var responseList = results.ToList();
        Assert.Equal(totalConcurrentWorkers, responseList.Count);

        var successCount = responseList.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responseList.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        var otherCount = responseList.Count(r => r.StatusCode != HttpStatusCode.Created && r.StatusCode != HttpStatusCode.Conflict);

        // Zero unexpected errors (no 500, no 400, no network drop)
        Assert.Equal(0, otherCount);

        // Mathematical Invariant #1: Exactly totalSeats requests succeeded with HTTP 201 Created
        Assert.Equal(totalSeats, successCount);

        // Mathematical Invariant #2: Remaining (totalWorkers - totalSeats) failed with HTTP 409 Conflict
        Assert.Equal(totalConcurrentWorkers - totalSeats, conflictCount);

        // Mathematical Invariant #3: All allocated hold IDs are unique (no two students got the same seat hold)
        var allocatedHoldIds = responseList
            .Where(r => r.StatusCode == HttpStatusCode.Created && r.HoldId != null)
            .Select(r => r.HoldId)
            .Distinct()
            .ToList();
        Assert.Equal(totalSeats, allocatedHoldIds.Count);

        // 6. Query final capacity from database via GET /classes/{id}
        var finalClassDetail = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classId}", JsonOptions);
        Assert.NotNull(finalClassDetail);

        // Mathematical Invariant #4: Database seat counts reflect 100% capacity utilization with ZERO overselling
        Assert.Equal(totalSeats, finalClassDetail.TotalSeats);
        Assert.Equal(totalSeats, finalClassDetail.HeldSeats);
        Assert.Equal(0, finalClassDetail.ConfirmedSeats);
        Assert.Equal(0, finalClassDetail.AvailableSeats);

        // Mathematical Proof: held + confirmed <= totalSeats
        Assert.True(
            finalClassDetail.HeldSeats + finalClassDetail.ConfirmedSeats <= finalClassDetail.TotalSeats,
            $"CRITICAL INVARIANT VIOLATION: Overselling detected! Held ({finalClassDetail.HeldSeats}) + Confirmed ({finalClassDetail.ConfirmedSeats}) > Total ({finalClassDetail.TotalSeats})"
        );
    }

    [Fact]
    public async Task ConcurrentPaymentConfirmation_NeverDoubleCountsSeats()
    {
        const int totalSeats = 5;

        // 1. Create class with 5 seats
        var createRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Concurrent Payment Class", totalSeats));
        var createdClass = await createRes.Content.ReadFromJsonAsync<CreateClassResponse>(JsonOptions);
        Assert.NotNull(createdClass);

        // 2. Reserve all 5 seats
        var holds = new List<(string StudentId, string HoldId)>();
        for (int i = 1; i <= totalSeats; i++)
        {
            var studentId = $"2026111099{i:D7}";
            using var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
            holdReq.Headers.Add("X-Student-Id", studentId);
            var holdRes = await _client.SendAsync(holdReq);
            Assert.Equal(HttpStatusCode.Created, holdRes.StatusCode);
            var holdData = await holdRes.Content.ReadFromJsonAsync<CreateHoldResponse>(JsonOptions);
            Assert.NotNull(holdData);
            holds.Add((studentId, holdData.HoldId));
        }

        // 3. Concurrently confirm all 5 holds simultaneously, each repeating 5 times to test idempotency race
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var confirmTasks = holds.SelectMany(h => Enumerable.Range(1, 5).Select(async _ =>
        {
            await startGate.Task;
            using var req = new HttpRequestMessage(HttpMethod.Post, $"/holds/{h.HoldId}/confirm");
            req.Headers.Add("X-Student-Id", h.StudentId);
            var res = await _client.SendAsync(req);
            return res.StatusCode;
        })).ToList();

        startGate.SetResult();
        var statusCodes = await Task.WhenAll(confirmTasks);

        // All 25 confirmation requests (5 holds x 5 concurrent retries) must return 200 OK
        Assert.All(statusCodes, code => Assert.Equal(HttpStatusCode.OK, code));

        // Final class capacity check
        var finalClass = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{createdClass.Id}", JsonOptions);
        Assert.NotNull(finalClass);
        Assert.Equal(totalSeats, finalClass.TotalSeats);
        Assert.Equal(0, finalClass.HeldSeats);
        Assert.Equal(totalSeats, finalClass.ConfirmedSeats);
        Assert.Equal(0, finalClass.AvailableSeats);

        // Zero oversell invariant
        Assert.True(finalClass.HeldSeats + finalClass.ConfirmedSeats <= finalClass.TotalSeats);
    }
}
