using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs.Classes;
using SchoolAdmission.Api.DTOs.Holds;
using Xunit;

namespace SchoolAdmission.Tests.Concurrency;

/// <summary>
/// Bonus Multi-Instance Concurrency Test.
/// PRD Reference: Section 2.6 (Bonus: Multi-Process/Multi-Instance Safety).
/// 
/// Proves that when multiple independent backend web host processes
/// (simulating e.g. Instance A on port 8080 and Instance B on port 8081)
/// run concurrently against the same shared database,
/// Rule 1 (Zero Seat Overselling) is 100% mathematically preserved.
/// </summary>
public class MultiInstanceConcurrencyTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public class MultiInstanceWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName;

        public MultiInstanceWebApplicationFactory(string dbName)
        {
            _dbName = dbName;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });

                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });

            builder.UseEnvironment("Testing");
        }
    }

    [Fact]
    public async Task DualInstanceConcurrency_FiredAcrossTwoSeparateWebHosts_NeverOversells()
    {
        // 1. Arrange: Provision two completely distinct WebApplicationFactory instances
        // sharing the same underlying database store.
        var sharedDbName = "SharedMultiInstanceDb_" + Guid.NewGuid().ToString("N");

        using var instanceA = new MultiInstanceWebApplicationFactory(sharedDbName);
        using var instanceB = new MultiInstanceWebApplicationFactory(sharedDbName);

        using var clientA = instanceA.CreateClient();
        using var clientB = instanceB.CreateClient();

        const int totalSeats = 10;
        const int requestsPerInstance = 50; // 50 to Instance A + 50 to Instance B = 100 total concurrent requests

        // 2. Create target class with 10 seats through Instance A
        var createRes = await clientA.PostAsJsonAsync("/classes", new CreateClassRequest("Multi-Process Shift", totalSeats));
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var createdClass = await createRes.Content.ReadFromJsonAsync<CreateClassResponse>(JsonOptions);
        Assert.NotNull(createdClass);
        var classId = createdClass.Id;

        // Verify Instance B immediately sees the class created by Instance A
        var classFromB = await clientB.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classId}", JsonOptions);
        Assert.NotNull(classFromB);
        Assert.Equal(totalSeats, classFromB.TotalSeats);
        Assert.Equal(totalSeats, classFromB.AvailableSeats);

        // 3. Prepare 100 unique student IDs (50 for Instance A, 50 for Instance B)
        var studentsForInstanceA = Enumerable.Range(1, requestsPerInstance)
            .Select(i => $"2026111011{i:D7}")
            .ToList();

        var studentsForInstanceB = Enumerable.Range(1, requestsPerInstance)
            .Select(i => $"2026111022{i:D7}")
            .ToList();

        // 4. Synchronization Gate: Hold all 100 requests until simultaneous release
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var results = new ConcurrentBag<(string Instance, string StudentId, HttpStatusCode StatusCode, string? HoldId)>();

        var tasksA = studentsForInstanceA.Select(async studentId =>
        {
            await startGate.Task;
            using var req = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
            req.Headers.Add("X-Student-Id", studentId);
            var res = await clientA.SendAsync(req);
            string? holdId = null;
            if (res.StatusCode == HttpStatusCode.Created)
            {
                var data = await res.Content.ReadFromJsonAsync<CreateHoldResponse>(JsonOptions);
                holdId = data?.HoldId;
            }
            results.Add(("InstanceA", studentId, res.StatusCode, holdId));
        });

        var tasksB = studentsForInstanceB.Select(async studentId =>
        {
            await startGate.Task;
            using var req = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
            req.Headers.Add("X-Student-Id", studentId);
            var res = await clientB.SendAsync(req);
            string? holdId = null;
            if (res.StatusCode == HttpStatusCode.Created)
            {
                var data = await res.Content.ReadFromJsonAsync<CreateHoldResponse>(JsonOptions);
                holdId = data?.HoldId;
            }
            results.Add(("InstanceB", studentId, res.StatusCode, holdId));
        });

        var allTasks = tasksA.Concat(tasksB).ToList();

        // 5. Act: Fire all 100 requests simultaneously across both backend hosts
        startGate.SetResult();
        await Task.WhenAll(allTasks);

        // 6. Assert: Mathematical validation across both instances
        var responseList = results.ToList();
        Assert.Equal(100, responseList.Count);

        var successCount = responseList.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responseList.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        var otherCount = responseList.Count(r => r.StatusCode != HttpStatusCode.Created && r.StatusCode != HttpStatusCode.Conflict);

        // Zero unexpected errors
        Assert.Equal(0, otherCount);

        // Invariant: Across BOTH instances combined, exactly totalSeats (10) succeeded
        Assert.Equal(totalSeats, successCount);

        // Invariant: Across BOTH instances combined, exactly 90 failed with 409 Conflict
        Assert.Equal(90, conflictCount);

        // Invariant: All 10 allocated hold IDs are unique across both instances
        var distinctHoldIds = responseList
            .Where(r => r.StatusCode == HttpStatusCode.Created && r.HoldId != null)
            .Select(r => r.HoldId)
            .Distinct()
            .ToList();
        Assert.Equal(totalSeats, distinctHoldIds.Count);

        // 7. Verify final database state from BOTH Instance A and Instance B
        var finalFromA = await clientA.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classId}", JsonOptions);
        var finalFromB = await clientB.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classId}", JsonOptions);

        Assert.NotNull(finalFromA);
        Assert.NotNull(finalFromB);

        // Both instances must see identical state
        Assert.Equal(10, finalFromA.HeldSeats);
        Assert.Equal(0, finalFromA.AvailableSeats);
        Assert.Equal(10, finalFromB.HeldSeats);
        Assert.Equal(0, finalFromB.AvailableSeats);

        // Zero overselling invariant: held + confirmed <= totalSeats
        Assert.True(finalFromA.HeldSeats + finalFromA.ConfirmedSeats <= finalFromA.TotalSeats);
        Assert.True(finalFromB.HeldSeats + finalFromB.ConfirmedSeats <= finalFromB.TotalSeats);
    }
}
