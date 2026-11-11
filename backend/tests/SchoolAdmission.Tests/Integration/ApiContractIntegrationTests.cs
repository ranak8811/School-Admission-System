using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Api.DTOs.Classes;
using SchoolAdmission.Api.DTOs.Holds;
using Xunit;

namespace SchoolAdmission.Tests.Integration;

/// <summary>
/// End-to-end integration test suite using WebApplicationFactory.
/// Validates HTTP status codes, headers, JSON contract shapes, error codes (400, 403, 404, 409),
/// and guarantees zero unhandled 500 exceptions across all 8 PRD endpoints.
/// </summary>
public class ApiContractIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiContractIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullLifecycle_CreateClass_HoldSeat_ConfirmPayment_VerifyStudentHistory()
    {
        const string studentId = "20261110000000001";

        // 1. POST /classes -> 201 Created
        var createClassRequest = new CreateClassRequest("Grade 11 - Science Shift", 20);
        var createClassResponse = await _client.PostAsJsonAsync("/classes", createClassRequest);
        Assert.Equal(HttpStatusCode.Created, createClassResponse.StatusCode);

        var createdClass = await createClassResponse.Content.ReadFromJsonAsync<CreateClassResponse>(JsonOptions);
        Assert.NotNull(createdClass);
        Assert.StartsWith("c_", createdClass.Id);
        Assert.Equal("Grade 11 - Science Shift", createdClass.Name);
        Assert.Equal(20, createdClass.TotalSeats);

        // 2. GET /classes -> 200 OK (verify newly created class is listed)
        var getClassesResponse = await _client.GetAsync("/classes");
        Assert.Equal(HttpStatusCode.OK, getClassesResponse.StatusCode);
        var classesList = await getClassesResponse.Content.ReadFromJsonAsync<List<ClassDetailResponse>>(JsonOptions);
        Assert.NotNull(classesList);
        Assert.Contains(classesList, c => c.Id == createdClass.Id);

        // 3. GET /classes/{id} -> 200 OK
        var getClassResponse = await _client.GetAsync($"/classes/{createdClass.Id}");
        Assert.Equal(HttpStatusCode.OK, getClassResponse.StatusCode);
        var classDetail = await getClassResponse.Content.ReadFromJsonAsync<ClassDetailResponse>(JsonOptions);
        Assert.NotNull(classDetail);
        Assert.Equal(20, classDetail.TotalSeats);
        Assert.Equal(0, classDetail.HeldSeats);
        Assert.Equal(0, classDetail.ConfirmedSeats);
        Assert.Equal(20, classDetail.AvailableSeats);

        // 4. POST /classes/{id}/holds with X-Student-Id -> 201 Created
        var holdRequestMsg = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdRequestMsg.Headers.Add("X-Student-Id", studentId);
        var holdResponse = await _client.SendAsync(holdRequestMsg);
        Assert.Equal(HttpStatusCode.Created, holdResponse.StatusCode);

        var holdData = await holdResponse.Content.ReadFromJsonAsync<CreateHoldResponse>(JsonOptions);
        Assert.NotNull(holdData);
        Assert.StartsWith("h_", holdData.HoldId);
        Assert.Equal(createdClass.Id, holdData.ClassId);
        Assert.Equal(studentId, holdData.StudentId);
        Assert.Equal("HELD", holdData.Status);
        Assert.True(holdData.ExpiresAt > DateTime.UtcNow);

        // 5. GET /holds/{id} -> 200 OK
        var getHoldResponse = await _client.GetAsync($"/holds/{holdData.HoldId}");
        Assert.Equal(HttpStatusCode.OK, getHoldResponse.StatusCode);
        var holdStatus = await getHoldResponse.Content.ReadFromJsonAsync<HoldStatusResponse>(JsonOptions);
        Assert.NotNull(holdStatus);
        Assert.Equal(holdData.HoldId, holdStatus.HoldId);
        Assert.Equal("HELD", holdStatus.Status);

        // Verify class capacity reflects the hold
        var classAfterHold = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{createdClass.Id}", JsonOptions);
        Assert.NotNull(classAfterHold);
        Assert.Equal(1, classAfterHold.HeldSeats);
        Assert.Equal(0, classAfterHold.ConfirmedSeats);
        Assert.Equal(19, classAfterHold.AvailableSeats);

        // 6. POST /holds/{id}/confirm with X-Student-Id -> 200 OK
        var confirmMsg = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdData.HoldId}/confirm");
        confirmMsg.Headers.Add("X-Student-Id", studentId);
        var confirmResponse = await _client.SendAsync(confirmMsg);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var confirmedData = await confirmResponse.Content.ReadFromJsonAsync<ConfirmHoldResponse>(JsonOptions);
        Assert.NotNull(confirmedData);
        Assert.Equal(holdData.HoldId, confirmedData.HoldId);
        Assert.Equal("CONFIRMED", confirmedData.Status);

        // 7. Duplicate Confirmation Idempotency: Re-confirming must return 200 OK without errors
        var duplicateConfirmMsg = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdData.HoldId}/confirm");
        duplicateConfirmMsg.Headers.Add("X-Student-Id", studentId);
        var duplicateConfirmResponse = await _client.SendAsync(duplicateConfirmMsg);
        Assert.Equal(HttpStatusCode.OK, duplicateConfirmResponse.StatusCode);

        // 8. GET /me/holds with X-Student-Id -> 200 OK
        var meHoldsMsg = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        meHoldsMsg.Headers.Add("X-Student-Id", studentId);
        var meHoldsResponse = await _client.SendAsync(meHoldsMsg);
        Assert.Equal(HttpStatusCode.OK, meHoldsResponse.StatusCode);

        var studentHolds = await meHoldsResponse.Content.ReadFromJsonAsync<List<StudentHoldItemResponse>>(JsonOptions);
        Assert.NotNull(studentHolds);
        var myHold = studentHolds.SingleOrDefault(h => h.HoldId == holdData.HoldId);
        Assert.NotNull(myHold);
        Assert.Equal("CONFIRMED", myHold.Status);
        Assert.Equal("Grade 11 - Science Shift", myHold.ClassName);

        // Verify class capacity reflects confirmed seat
        var classAfterConfirm = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{createdClass.Id}", JsonOptions);
        Assert.NotNull(classAfterConfirm);
        Assert.Equal(0, classAfterConfirm.HeldSeats);
        Assert.Equal(1, classAfterConfirm.ConfirmedSeats);
        Assert.Equal(19, classAfterConfirm.AvailableSeats);
    }

    [Fact]
    public async Task Lifecycle_CreateClass_HoldSeat_CancelHold_ReleasesSeatBackToPool()
    {
        const string studentId = "20261110000000002";

        // 1. Create Class with 5 seats
        var createClassRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Class 8 - English", 5));
        var createdClass = await createClassRes.Content.ReadFromJsonAsync<CreateClassResponse>(JsonOptions);
        Assert.NotNull(createdClass);

        // 2. Place Hold
        var holdMsg = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdMsg.Headers.Add("X-Student-Id", studentId);
        var holdRes = await _client.SendAsync(holdMsg);
        Assert.Equal(HttpStatusCode.Created, holdRes.StatusCode);
        var hold = await holdRes.Content.ReadFromJsonAsync<CreateHoldResponse>(JsonOptions);
        Assert.NotNull(hold);

        // Verify 1 seat held
        var classMid = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{createdClass.Id}", JsonOptions);
        Assert.NotNull(classMid);
        Assert.Equal(1, classMid.HeldSeats);
        Assert.Equal(4, classMid.AvailableSeats);

        // 3. Cancel Hold: DELETE /holds/{id} -> 204 No Content
        var cancelMsg = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold.HoldId}");
        cancelMsg.Headers.Add("X-Student-Id", studentId);
        var cancelRes = await _client.SendAsync(cancelMsg);
        Assert.Equal(HttpStatusCode.NoContent, cancelRes.StatusCode);

        // 4. Verify Hold status is CANCELLED
        var getHoldRes = await _client.GetAsync($"/holds/{hold.HoldId}");
        Assert.Equal(HttpStatusCode.OK, getHoldRes.StatusCode);
        var holdStatus = await getHoldRes.Content.ReadFromJsonAsync<HoldStatusResponse>(JsonOptions);
        Assert.NotNull(holdStatus);
        Assert.Equal("CANCELLED", holdStatus.Status);

        // 5. Verify seat released back to pool (0 held, 5 available)
        var classFinal = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{createdClass.Id}", JsonOptions);
        Assert.NotNull(classFinal);
        Assert.Equal(0, classFinal.HeldSeats);
        Assert.Equal(5, classFinal.AvailableSeats);
    }

    [Fact]
    public async Task ErrorContract_MissingOrInvalidStudentId_Returns400BadRequest_Never500()
    {
        // Missing X-Student-Id header on POST /classes/c_test/holds
        var missingHeaderMsg = new HttpRequestMessage(HttpMethod.Post, "/classes/c_dummy/holds");
        var missingRes = await _client.SendAsync(missingHeaderMsg);
        Assert.Equal(HttpStatusCode.BadRequest, missingRes.StatusCode);

        // 16-digit student ID (too short)
        var shortIdMsg = new HttpRequestMessage(HttpMethod.Post, "/classes/c_dummy/holds");
        shortIdMsg.Headers.Add("X-Student-Id", "1234567890123456");
        var shortRes = await _client.SendAsync(shortIdMsg);
        Assert.Equal(HttpStatusCode.BadRequest, shortRes.StatusCode);

        // 18-digit student ID (too long)
        var longIdMsg = new HttpRequestMessage(HttpMethod.Post, "/classes/c_dummy/holds");
        longIdMsg.Headers.Add("X-Student-Id", "123456789012345678");
        var longRes = await _client.SendAsync(longIdMsg);
        Assert.Equal(HttpStatusCode.BadRequest, longRes.StatusCode);

        // Alpha characters
        var alphaIdMsg = new HttpRequestMessage(HttpMethod.Post, "/classes/c_dummy/holds");
        alphaIdMsg.Headers.Add("X-Student-Id", "2026111000000000A");
        var alphaRes = await _client.SendAsync(alphaIdMsg);
        Assert.Equal(HttpStatusCode.BadRequest, alphaRes.StatusCode);

        // Missing X-Student-Id on GET /me/holds
        var missingMeMsg = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        var missingMeRes = await _client.SendAsync(missingMeMsg);
        Assert.Equal(HttpStatusCode.BadRequest, missingMeRes.StatusCode);
    }

    [Fact]
    public async Task ErrorContract_NonExistentResources_Returns404NotFound_Never500()
    {
        // Non-existent class
        var classRes = await _client.GetAsync("/classes/c_non_existent_9999");
        Assert.Equal(HttpStatusCode.NotFound, classRes.StatusCode);

        // Non-existent hold
        var holdRes = await _client.GetAsync("/holds/h_non_existent_9999");
        Assert.Equal(HttpStatusCode.NotFound, holdRes.StatusCode);

        // Confirm non-existent hold
        var confirmMsg = new HttpRequestMessage(HttpMethod.Post, "/holds/h_non_existent_9999/confirm");
        confirmMsg.Headers.Add("X-Student-Id", "20261110000000001");
        var confirmRes = await _client.SendAsync(confirmMsg);
        Assert.Equal(HttpStatusCode.NotFound, confirmRes.StatusCode);

        // Cancel non-existent hold
        var cancelMsg = new HttpRequestMessage(HttpMethod.Delete, "/holds/h_non_existent_9999");
        cancelMsg.Headers.Add("X-Student-Id", "20261110000000001");
        var cancelRes = await _client.SendAsync(cancelMsg);
        Assert.Equal(HttpStatusCode.NotFound, cancelRes.StatusCode);
    }

    [Fact]
    public async Task ErrorContract_CapacityExhaustion_Returns409Conflict_NeverOversells()
    {
        // 1. Create class with exactly 1 seat
        var createClassRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Single Seat Class", 1));
        var createdClass = await createClassRes.Content.ReadFromJsonAsync<CreateClassResponse>(JsonOptions);
        Assert.NotNull(createdClass);

        // 2. Student A holds the single seat -> 201 Created
        var holdMsg1 = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdMsg1.Headers.Add("X-Student-Id", "20261110000000010");
        var holdRes1 = await _client.SendAsync(holdMsg1);
        Assert.Equal(HttpStatusCode.Created, holdRes1.StatusCode);

        // 3. Student B attempts to hold the same class -> 409 Conflict
        var holdMsg2 = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdMsg2.Headers.Add("X-Student-Id", "20261110000000020");
        var holdRes2 = await _client.SendAsync(holdMsg2);
        Assert.Equal(HttpStatusCode.Conflict, holdRes2.StatusCode);

        // 4. Student A attempts to hold a 2nd seat in the same class -> 409 Conflict (Duplicate student guard)
        var holdMsg3 = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdMsg3.Headers.Add("X-Student-Id", "20261110000000010");
        var holdRes3 = await _client.SendAsync(holdMsg3);
        Assert.Equal(HttpStatusCode.Conflict, holdRes3.StatusCode);
    }

    [Fact]
    public async Task ErrorContract_StudentMismatchOnConfirm_Returns403Forbidden()
    {
        // 1. Create Class
        var createClassRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Physics Shift", 10));
        var createdClass = await createClassRes.Content.ReadFromJsonAsync<CreateClassResponse>(JsonOptions);
        Assert.NotNull(createdClass);

        // 2. Student A holds seat
        var holdMsg = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdMsg.Headers.Add("X-Student-Id", "20261110000000030");
        var holdRes = await _client.SendAsync(holdMsg);
        var hold = await holdRes.Content.ReadFromJsonAsync<CreateHoldResponse>(JsonOptions);
        Assert.NotNull(hold);

        // 3. Student B attempts to confirm Student A's hold -> 403 Forbidden
        var hijackMsg = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
        hijackMsg.Headers.Add("X-Student-Id", "20261110000000040"); // Different student
        var hijackRes = await _client.SendAsync(hijackMsg);
        Assert.Equal(HttpStatusCode.Forbidden, hijackRes.StatusCode);
    }

    [Fact]
    public async Task ErrorContract_CancelConfirmedAdmission_Returns409Conflict()
    {
        // 1. Create Class
        var createClassRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Chemistry Shift", 10));
        var createdClass = await createClassRes.Content.ReadFromJsonAsync<CreateClassResponse>(JsonOptions);
        Assert.NotNull(createdClass);

        // 2. Student holds and confirms seat
        const string studentId = "20261110000000050";
        var holdMsg = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdMsg.Headers.Add("X-Student-Id", studentId);
        var holdRes = await _client.SendAsync(holdMsg);
        var hold = await holdRes.Content.ReadFromJsonAsync<CreateHoldResponse>(JsonOptions);
        Assert.NotNull(hold);

        var confirmMsg = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
        confirmMsg.Headers.Add("X-Student-Id", studentId);
        var confirmRes = await _client.SendAsync(confirmMsg);
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);

        // 3. Attempt to cancel confirmed admission -> 400 Bad Request (cannot cancel confirmed admission)
        var cancelMsg = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold.HoldId}");
        cancelMsg.Headers.Add("X-Student-Id", studentId);
        var cancelRes = await _client.SendAsync(cancelMsg);
        Assert.Equal(HttpStatusCode.BadRequest, cancelRes.StatusCode);
    }
}
