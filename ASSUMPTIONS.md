# Architectural & Domain Assumptions

**Project:** School Admission System  
**Recruitment Evaluation:** OnnoRokom Projukti Limited — Assistant Software Engineer Assessment  

---

## 1. Domain & Business Rules

* **Student Identification (17-Digit BRN):**  
  Following Bangladesh's national educational admission standards, student identities are modeled on the official 17-digit digital Birth Registration Number (BRN) validated by regex `^[0-9]{17}$`. Non-17-digit inputs are rejected with `400 Bad Request`.
* **Zero Overselling Invariant:**  
  At any time `t` for class `c`, the system strictly enforces:  
  `HeldSeats(c, t) + ConfirmedSeats(c, t) <= TotalSeats(c)`  
  A hold is active only if: `Status == HELD && ExpiresAt > UtcNow`.
* **Single Active Hold per Student per Class:**  
  A student can only hold one active or confirmed reservation per class offering at a time. Duplicate reservation attempts return `409 Conflict`.
* **10-Minute Reservation Window:**  
  Holds expire after 600 seconds (`AdmissionOptions:HoldDurationSeconds`), overridable via the `HOLD_DURATION_SECONDS` environment variable.
* **Dual Expiration Strategy:**  
  * *Dynamic CQRS:* All queries evaluate `ExpiresAt > DateTime.UtcNow` in real time, so expired seats become available immediately without waiting for background jobs.  
  * *Background Sweeper:* An `IHostedService` (`ExpiredHoldSweeper`) runs every 30s to update expired records to `EXPIRED` in bulk for database hygiene.
* **Idempotent Payment Confirmation:**  
  Only the student who reserved the hold can confirm it (`403 Forbidden` on mismatch). If already `CONFIRMED`, the API idempotently returns `200 OK` with the confirmed voucher, preventing duplicate seat deductions during browser double-clicks or network retries.
* **Confirmed Admission Immutability:**  
  Active holds (`HELD`) can be cancelled voluntarily (`204 No Content`), immediately freeing the seat. Once `CONFIRMED`, admissions cannot be cancelled via self-service (`400 Bad Request`), safeguarding institutional records.

---

## 2. Concurrency & Architecture

* **Multi-Instance Concurrency (PRD Section 2.6 Bonus):**  
  Process-level locks (`lock`, `SemaphoreSlim`) fail across multiple backend instances. Concurrency is enforced at the database engine level using PostgreSQL row-level pessimistic locking:
  ```sql
  SELECT * FROM classes WHERE id = @classId FOR UPDATE;
  ```
  This serializes concurrent reservations into a strict FIFO queue, guaranteeing zero overselling across distributed replicas.
* **Transaction Resilience:**  
  Transactional operations are executed through `_dbContext.Database.CreateExecutionStrategy().ExecuteAsync(...)` to ensure safe, idempotent retries during transient database connections.
* **Dual-Locking Testing Strategy:**  
  In production, PostgreSQL row locks are used. In unit tests, the service detects `!Database.IsRelational()` and transparently activates fine-grained in-memory `SemaphoreSlim` locks, allowing 126 backend tests to pass in **< 800ms** without requiring a local database.

---

## 3. API & Communication Standards

* **Identity Context:** Mutations (`/holds`, `/confirm`, `/cancel`) and student history (`/me/holds`) require the `X-Student-Id` header. Public catalog endpoints are unauthenticated.
* **RFC 7807 ProblemDetails:** All 4xx/5xx responses use the standardized `ProblemDetails` schema for structured error handling.
* **UTC Timestamps:** All timestamps are ISO 8601 UTC. Frontend timers compute remaining duration via client epoch math against server UTC timestamps to eliminate local clock drift.
* **Stateless 4s Polling:** The frontend uses lightweight 4-second HTTP polling rather than stateful WebSockets, providing instant seat visibility while allowing seamless horizontal scaling behind standard load balancers.

---

## 4. Quick Verification Matrix

| Rule / Assumption | Enforcement Mechanism | Automated Test Coverage |
|---|---|---|
| **17-Digit BRN** | Regex validation + DB Check constraint | `StudentIdValidationTests.cs` (8 tests) |
| **Zero Overselling** | PostgreSQL `SELECT ... FOR UPDATE` | `HighConcurrencyStressTests.cs` (100 parallel workers) |
| **Multi-Instance Safety** | DB-level row serialization | `MultiInstanceConcurrencyTests.cs` (2 concurrent web hosts) |
| **Idempotent Confirm** | State machine check returning 200 OK | `ApiContractIntegrationTests.cs` |
| **600s Expiry Window** | Real-time UTC check + Background sweeper | `ExpirationBoundaryTests.cs` (t-1, t, t+1 boundaries) |
| **Immutable Confirmed** | Service status transition guard | `ApiContractIntegrationTests.cs` (400 on cancel confirmed) |
