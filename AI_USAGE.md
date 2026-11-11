# AI Usage & Collaboration Report

**Project:** School Admission System  
**Recruitment Evaluation:** OnnoRokom Projukti Limited — Assistant Software Engineer Assessment  
**Compliance:** PRD Section 2.7 Mandatory Disclosure  

---

## 1. Summary of AI Assistance

AI coding tools were used strictly as an accelerated pair programmer for high-velocity, repetitive tasks:
* **Boilerplate & Scaffolding:** Generating initial C# solution/project structures (`.sln`, `.csproj`), package lists, and Vite React directory templates.
* **DTO & Entity Drafting:** Drafting repetitive C# record definitions (`CreateClassRequest`, `HoldResponse`) and initial EF Core model properties.
* **Mock Test Data:** Generating sample class catalogs and arrays of valid/invalid 17-digit student IDs for boundary tests.
* **Docker Templates:** Creating initial draft Dockerfiles for multi-stage .NET and Node.js builds.

---

## 2. Critical AI Flaws Caught & Corrected by Developer

AI models frequently generated code with severe concurrency bugs, edge-case vulnerabilities, or architectural anti-patterns. Below are the six major AI-generated flaws that were caught and manually corrected during development:

### 1. In-Memory Locking vs Multi-Instance Concurrency (PRD Section 2.6 Bonus)
* **AI Proposal:** Suggested C# in-memory `lock (_seatLock)` to synchronize seat holds.
* **Why It Was Flawed:** In-memory locks only protect a single process. Across multiple backend instances (replicas), independent processes do not share memory, leading to severe seat overselling under concurrent load.
* **Developer Fix:** Architected database-level serialization using PostgreSQL pessimistic row locking (`SELECT ... FOR UPDATE`), proven across multiple simultaneously running web host instances in `MultiInstanceConcurrencyTests.cs`.

### 2. Npgsql Retrying Strategy vs User Transactions
* **AI Proposal:** Added `EnableRetryOnFailure()` in PostgreSQL options to handle container cold-starts.
* **Why It Was Flawed:** When active, EF Core switches to `NpgsqlRetryingExecutionStrategy`, which strictly disallows manual `BeginTransactionAsync()` calls and throws runtime `InvalidOperationException` on `POST /classes/{id}/holds`.
* **Developer Fix:** Wrapped transactional operations in `_dbContext.Database.CreateExecutionStrategy().ExecuteAsync(...)` and coordinated startup via Docker Compose healthchecks (`depends_on: db: condition: service_healthy`).

### 3. Non-Idempotent Admission Confirmation
* **AI Proposal:** Returned `409 Conflict ("Already confirmed")` if a student re-submitted confirmation for an already confirmed hold.
* **Why It Was Flawed:** Network retries, mobile disconnects, or browser double-clicks would cause students to receive an error screen despite their payment succeeding.
* **Developer Fix:** Made the state transition strictly idempotent—if already confirmed by the same student, the API safely returns `200 OK` with the confirmation voucher without re-allocating seats.

### 4. Timezone Discrepancies & Expiry Skew
* **AI Proposal:** Used `DateTime.Now` (local machine time) for hold expiry comparisons.
* **Why It Was Flawed:** Mismatches between local system time (BST / UTC+6) and container UTC time created 6-hour discrepancies where holds either expired immediately or never expired.
* **Developer Fix:** Enforced strict UTC normalization (`DateTime.UtcNow`, `timestamp with time zone`, ISO 8601 UTC) across backend, database, and frontend countdown logic.

### 5. Single Page Application 404s & Alpine IPv6 Healthcheck
* **AI Proposal:** Generated a basic Nginx config lacking SPA fallback and listening only on IPv4.
* **Why It Was Flawed:** Browser refreshes on client-side routes triggered Nginx 404 errors, and Alpine Linux resolved `localhost` to IPv6 (`::1`), causing container healthchecks to fail with connection refused.
* **Developer Fix:** Added `try_files $uri $uri/ /index.html;`, dual-stack listening (`listen 3000; listen [::]:3000;`), and a dedicated `/healthz` endpoint.

### 6. Generic "AI-Looking" UI Overhaul
* **AI Proposal:** Produced generic purple-gradient UI templates with artificial buzzwords.
* **Developer Fix:** Replaced with a human-crafted design following **OnnoRokom Projukti Limited's** corporate visual identity (`#0F172A`, `#1E293B`, `#0284C7`), featuring realistic 17-digit BRN validation, Morning/Day shift schedules, fee notices, and 1-click printable admission slips.

---

## 3. Engineering Governance

* **Architectural Ownership:** All architectural designs, database schemas, mathematical invariants (`Held + Confirmed <= TotalSeats`), and API error contracts were designed and owned by the human developer.
* **Verification:** Every AI-assisted code section was verified against **126 automated backend tests** (unit, integration, high-concurrency, multi-instance) and **11 frontend Vitest tests**.
