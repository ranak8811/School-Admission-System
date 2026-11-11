# Architectural & Domain Assumptions

**Project:** School Admission System  
**Recruitment Evaluation:** OnnoRokom Projukti Limited — Assistant Software Engineer Assessment  
**Session:** Academic Year 2026–2027  
**Submission Date:** November 11, 2026  

---

## 1. Executive Summary

This document formalizes all architectural, domain modeling, and concurrency assumptions made where the recruitment problem statement left room for engineering discretion. Each assumption is accompanied by its technical justification, system invariants, and implementation safeguards.

---

## 2. Domain & Business Logic Assumptions

### 2.1 Student Identification & National Digital BRN (17 Digits)
* **Specification:** The PRD specifies an `X-Student-Id` header without defining its exact format or length.
* **Assumption:** In alignment with Bangladesh's national educational admission portal standards (Directorate of Secondary and Higher Education), student identities are modeled on the official **17-digit digital Birth Registration Number (BRN)** (e.g., `20261110000000001`).
* **Validation Rules:**
  * Must match the regular expression: `^\d{17}$`.
  * Non-digit characters and lengths $\ne 17$ are immediately rejected with `400 Bad Request`.
  * Database column: `VARCHAR(32)` enforced via a PostgreSQL check constraint:  
    `"student_id" ~ '^[0-9]{17}$'`.
  * Frontend input automatically sanitizes non-numeric characters and limits input length to 17 digits.

### 2.2 Strict Seat Allocation Invariant (Zero Overselling Guarantee)
* **Invariant:** At any point in time $t$, for any class offering $c$:
  $$\text{heldSeats}(c, t) + \text{confirmedSeats}(c, t) \le \text{totalSeats}(c)$$
  $$\text{availableSeats}(c, t) = \max\Big(0,\ \text{totalSeats}(c) - \big(\text{heldSeats}(c, t) + \text{confirmedSeats}(c, t)\big)\Big)$$
* **Active Hold Definition:** A hold record $h$ is counted as "active" if and only if:
  $$\text{Status} = \text{HELD} \quad \land \quad \text{ExpiresAt} > t_{\text{UTC}}$$
* **Guarantee:** Under no circumstances will the system allocate more seats than configured, even under heavy concurrent loads or distributed multi-instance deployment.

### 2.3 Single Active Seat Hold per Student per Offering
* **Specification:** The PRD states students reserve a hold before confirmation.
* **Assumption:** A student cannot hold multiple simultaneous active reservations for the *same class offering*. If a student attempts to reserve a seat in class $c$ while already possessing an unexpired `HELD` or `CONFIRMED` record in class $c$, the API returns `409 Conflict` with a descriptive message.
* **Database Constraint:** Composite index `ix_seat_holds_student_class` on `(student_id, class_id)` accelerates invariant checking during transactional seat reservation.

### 2.4 Hold Window Duration Configuration
* **Specification:** The PRD suggests a default hold duration of 10 minutes.
* **Assumption:** The duration is completely configurable via configuration:
  * Application setting: `AdmissionOptions:HoldDurationSeconds` in `appsettings.json`.
  * Environment variable: `HOLD_DURATION_SECONDS` (overrides configuration at container runtime).
  * Default: `600` seconds (10 minutes).
  * Boundary test cases ($t-1, t, t+1$) prove that at exactly 600.001 seconds, the hold is treated as expired.

### 2.5 Dual Expiration Strategy: Dynamic Evaluation + Background Sweeper
* **Architecture:** To guarantee real-time correctness without relying solely on background cron jobs:
  1. **Dynamic Evaluation (Read-Time CQRS):** Every query (`GET /classes`, `GET /classes/{id}`, `GET /holds/{id}`) dynamically evaluates `ExpiresAt > DateTime.UtcNow`. Even if the background worker has not run yet, an expired hold is **never** counted as occupying a seat.
  2. **Background Sweeper (`ExpiredHoldSweeper`):** An ASP.NET Core `BackgroundService` runs every 30 seconds and issues a high-performance bulk update:
     ```sql
     UPDATE seat_holds
     SET status = 'EXPIRED', updated_at = NOW()
     WHERE status = 'HELD' AND expires_at <= NOW();
     ```
     This cleans up database states, freeing up index memory and ensuring data hygiene.

### 2.6 Payment Confirmation Simulation & Idempotency
* **Specification:** PRD Endpoint #6 specifies confirming an admission hold.
* **Assumption:** Payment gateway integration (e.g., bKash, Nagad, SSLCommerz) is simulated. When a student submits a valid payment request:
  1. Ownership is verified: Only the student who created the hold (`X-Student-Id`) can confirm it (`403 Forbidden` if mismatched).
  2. State validation: If the hold is `EXPIRED` or `CANCELLED`, confirmation is rejected with `409 Conflict`.
  3. **Strict Idempotency:** If the hold is already `CONFIRMED` by the same student, the API idempotently returns `200 OK` with `{"holdId": "...", "status": "CONFIRMED"}` without error and without duplicating allocated seats. This safely handles network retries, browser double-clicks, and webhook replays.

### 2.7 Cancellation Lifecycle & Immutability of Confirmed Seats
* **Specification:** PRD Endpoint #7 specifies cancelling a hold.
* **Assumption:**
  * Active holds (`HELD`) can be released voluntarily by the student at any time, returning `204 No Content` and immediately returning the seat to the available pool.
  * **Confirmed Admissions Are Immutable:** Once a student has paid and confirmed their admission (`CONFIRMED`), the hold cannot be cancelled via the public self-service API. Calling `DELETE /holds/{id}` on a confirmed admission returns `400 Bad Request`, protecting school accounting records from accidental student cancellation.

---

## 3. Architectural & Concurrency Assumptions

### 3.1 Multi-Process / Multi-Instance Concurrency Guarantee
* **Challenge:** PRD Section 2.6 presents a critical architectural question:
  > *"Bonus: If multiple instances of the backend run on different ports or processes against the same database, is Rule 1 (Zero Overselling) still maintained?"*
* **Architecture:**
  * **In-Memory Locks Are Insufficient:** Process-level locks (`lock`, `Monitor`, `SemaphoreSlim`) only serialize threads within a single operating system process. Across multiple backend instances (e.g., in a Kubernetes cluster or Docker Compose replicas), process memory is isolated.
  * **PostgreSQL Engine-Level Row Locking:** Serialization is enforced at the database layer using pessimistic row-level locking:
    ```sql
    SELECT * FROM classes WHERE id = @classId FOR UPDATE;
    ```
  * When Backend Instance A and Backend Instance B execute `CreateHoldAsync` for the same class concurrently, PostgreSQL grants an exclusive row lock to the first transaction. The second transaction pauses until the first commits or rolls back.
  * Because the capacity calculation occurs inside this locked transaction, both instances see the exact serial state, mathematically guaranteeing **zero overselling** across distributed containers.

### 3.2 Transaction Isolation Level
* **Assumption:** Transactions use `IsolationLevel.ReadCommitted` in conjunction with `SELECT ... FOR UPDATE`. This provides maximum throughput while preventing dirty reads, non-repeatable reads on locked rows, and phantom capacity allocations.

### 3.3 Test In-Memory Synchronization Fallbacks
* **Assumption:** Automated unit and integration test suites can run in two modes:
  1. Relational Mode (PostgreSQL): Executes native `SELECT ... FOR UPDATE` transactions.
  2. In-Memory Mode (`UseInMemoryDatabase`): EF Core's InMemory provider does not support raw SQL transactions. The `SeatAllocationService` detects `_dbContext.Database.IsRelational()` and transparently activates fine-grained in-memory `SemaphoreSlim` locks (`InMemoryClassLocks`, `InMemoryHoldLocks`).
  * This dual-mode design allows the 126 automated backend tests to execute in under **800 milliseconds** in sandboxed CI/CD pipelines without requiring a local PostgreSQL daemon.

### 3.4 Execution Strategy Resilience
* **Assumption:** When running with custom execution strategies, transactional blocks are wrapped in `_dbContext.Database.CreateExecutionStrategy().ExecuteAsync(...)`. This ensures compatibility with both standard non-retrying execution strategies and retrying execution strategies, preventing `InvalidOperationException` while maintaining atomic ACID guarantees.

---

## 4. API & Interface Assumptions

### 4.1 Header-Based Identity Context (`X-Student-Id`)
* All student-facing mutations (`POST /classes/{id}/holds`, `POST /holds/{id}/confirm`, `DELETE /holds/{id}`) and personalized history queries (`GET /me/holds`) require the `X-Student-Id` header.
* Public catalog queries (`GET /classes`, `GET /classes/{id}`) do not require authentication headers.

### 4.2 RFC 7807 ProblemDetails Standard
* All 4xx and 5xx responses conform strictly to the **RFC 7807 ProblemDetails** specification:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
    "title": "Conflict",
    "status": 409,
    "detail": "No seats available in this class."
  }
  ```
* This prevents client crashes from unexpected string bodies and provides standardized error handling across the React frontend.

### 4.3 ISO 8601 UTC Timestamps
* All timestamps (`createdAt`, `updatedAt`, `expiresAt`) are stored, computed, and transmitted in ISO 8601 UTC format with trailing `Z` (e.g., `2026-11-11T06:00:00Z`).
* The frontend client computes countdown timer intervals by subtracting `Date.now()` (client epoch) from the server UTC timestamp, eliminating client clock skew issues.

### 4.4 Polling vs WebSockets for Live Updates
* **Assumption:** The frontend uses **HTTP short polling** every 4 seconds (`setInterval`) instead of WebSockets or SSE.
* **Justification:**
  * WebSockets introduce stateful connection overhead, firewall complications, and sticky-session load balancer requirements.
  * 4-second polling against lightweight `.AsNoTracking()` endpoints provides instant seat visibility for high-concurrency admission portals while maintaining horizontal stateless scalability.

---

## 5. Infrastructure & Deployment Assumptions

### 5.1 Multi-Container Docker Orchestration
* **Architecture:** The production deployment runs via `docker-compose.yml` with 3 isolated containers on internal network `school_admission_network`:
  1. `db`: PostgreSQL 16 Alpine with `postgres_data` persistent named volume and `pg_isready` health check.
  2. `backend`: ASP.NET Core 8 Web API running on port `8080`, configured with `depends_on: db: condition: service_healthy`.
  3. `frontend`: React 18 + Vite SPA compiled into static assets and served by Nginx Alpine on port `3000`.

### 5.2 Single Page Application (SPA) Fallback Routing
* In `frontend/nginx.conf`, the directive `try_files $uri $uri/ /index.html;` is mandatory. This ensures that browser refreshes on client-side routes (or deep links) return the application bundle rather than Nginx HTTP 404 Not Found errors.

---

## 6. Verification Matrix

| Assumption | Enforced By | Automated Test Verification |
|---|---|---|
| **17-Digit BRN Format** | Regex + DB Check Constraint | `StudentIdValidationTests.cs` (8 tests) |
| **Zero Overselling ($\le$ Total)** | PostgreSQL `SELECT ... FOR UPDATE` | `HighConcurrencyStressTests.cs` (100 concurrent workers) |
| **Multi-Instance Safety** | DB-Level Pessimistic Locking | `MultiInstanceConcurrencyTests.cs` (Dual Web Hosts) |
| **Idempotent Confirmation** | State Machine Guard | `ApiContractIntegrationTests.cs` (Multiple confirmations) |
| **600s Expiration Window** | UTC Timestamp + Sweeper | `ExpirationBoundaryTests.cs` ($t-1, t, t+1$ boundaries) |
| **Immutable Confirmed Seats** | Service Transition Guard | `ApiContractIntegrationTests.cs` (Cancel confirmed rejection) |
