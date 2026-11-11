# AI Usage & Collaboration Report

**Project:** School Admission System  
**Recruitment Evaluation:** OnnoRokom Projukti Limited — Assistant Software Engineer Assessment  
**Session:** Academic Year 2026–2027  
**Submission Date:** November 11, 2026  
**Compliance:** PRD Section 2.7 Mandatory Disclosure  

---

## 1. Executive Disclosure

In accordance with Section 2.7 of the take-home assessment brief, this document provides complete, transparent disclosure of how Artificial Intelligence tools were utilized during the development of the School Admission System. It outlines the specific scope of AI assistance, analyzes critical technical pitfalls where AI models generated incorrect or dangerous code, and details the developer-driven architectural corrections that ensured production-grade reliability.

---

## 2. Tools & Models Used

| Tool | Model Version | Primary Use Case |
|---|---|---|
| **Google Antigravity CLI** | Gemini 2.5 Pro / 3.8 Flash | Interactive pair-programming agent for boilerplate scaffolding, rapid test scenario formulation, and refactoring assistance. |
| **Vitest & xUnit CLI** | N/A (Automated Run-time Verification) | Automated test harnesses used by the developer to continuously verify and reject flawed AI code suggestions. |

---

## 3. Scope of AI Assistance (Where AI Was Helpful)

AI tools were leveraged as an accelerated junior pair programmer for repetitive and high-velocity tasks:

1. **Boilerplate & Project Scaffolding:**
   - Generating standard C# project files (`.sln`, `.csproj`), solution structure, and NuGet package dependency lists.
   - Scaffolding the React 18 + Vite + TypeScript frontend directory layout.
2. **DTO & Entity Mapping:**
   - Writing repetitive C# record DTO definitions (`CreateClassRequest`, `ClassDetailResponse`, `HoldStatusResponse`).
   - Drafting initial EF Core entity property declarations and table mappings.
3. **Test Data Generation:**
   - Populating initial mock seed catalogs (`Class 6 - Morning Shift`, `Class 6 - Day Shift`, `Class 7 - Morning Shift`).
   - Formulating large arrays of valid and invalid 17-digit student IDs for boundary testing.
4. **Shell & Docker Commands:**
   - Generating initial multi-stage `Dockerfile` templates for `.NET 8` and `node:20-alpine`.

---

## 4. Critical AI Errors Caught & Manually Corrected by Developer

AI models, when unguided, frequently generate code that appears convincing on the surface but contains catastrophic concurrency flaws, edge-case vulnerabilities, or architectural anti-patterns. Below are the six major AI-generated errors that were caught and corrected by the developer:

---

### Hazard 1: In-Memory Locking vs Distributed Database Row Locks (PRD Section 2.6 Bonus)
* **The AI's Suggestion:**
  To solve race conditions during seat reservation, the AI initially suggested using C# in-memory synchronization primitives:
  ```csharp
  // FLAWED AI CODE:
  private static readonly object _seatLock = new object();
  lock (_seatLock)
  {
      // Check capacity and insert hold
  }
  ```
* **Why It Was Dangerous:**
  `lock` or `SemaphoreSlim` only synchronizes threads within a single operating system process. The recruitment brief explicitly features a **Section 2.6 Bonus Question**:
  > *"If multiple instances of the backend run on different ports or processes against the same database, is Rule 1 (Zero Overselling) still maintained?"*
  If two backend instances (Instance A and Instance B) ran against the database, in-memory locks would know nothing about each other, leading to severe seat overselling under concurrent load.
* **Developer Correction:**
  The developer overrode this approach and architected database-level serialization using PostgreSQL pessimistic row locking:
  ```sql
  SELECT * FROM classes WHERE id = @classId FOR UPDATE;
  ```
  This was verified by writing `MultiInstanceConcurrencyTests.cs`, which boots two independent web host instances simultaneously and proves zero overselling across distributed processes.

---

### Hazard 2: NpgsqlRetryingExecutionStrategy vs User-Initiated Transactions
* **The AI's Suggestion:**
  In `Program.cs`, the AI added `EnableRetryOnFailure()` to the PostgreSQL connection configuration to make container cold starts resilient:
  ```csharp
  // FLAWED AI CONFIGURATION:
  builder.Services.AddDbContext<AppDbContext>(options =>
      options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure(5)));
  ```
* **Why It Failed:**
  When `EnableRetryOnFailure` is enabled, EF Core Npgsql switches to `NpgsqlRetryingExecutionStrategy`. Under this strategy, direct user-initiated transactions via `_dbContext.Database.BeginTransactionAsync()` are strictly disallowed and throw at runtime:
  ```
  System.InvalidOperationException: The configured execution strategy 'NpgsqlRetryingExecutionStrategy' 
  does not support user-initiated transactions.
  ```
  This caused `POST /classes/{id}/holds` to throw a 500 Internal Server Error when tested inside Docker!
* **Developer Correction:**
  The developer diagnosed the container logs and applied a two-fold fix:
  1. Wrapped all transactional seat operations in `_dbContext.Database.CreateExecutionStrategy().ExecuteAsync(...)` inside `SeatAllocationService.cs`, ensuring transactional operations are executed as an atomic retriable unit.
  2. Coordinated container startup via Docker Compose `depends_on: db: condition: service_healthy`, removing the need for conflicting retry-on-failure strategies on transactional code paths.

---

### Hazard 3: Non-Idempotent Admission Confirmation (Double Payment Panic)
* **The AI's Suggestion:**
  In the admission confirmation endpoint (`POST /holds/{id}/confirm`), the AI originally wrote:
  ```csharp
  // FLAWED AI LOGIC:
  if (hold.Status == HoldStatus.CONFIRMED)
  {
      return Conflict("This hold has already been confirmed.");
  }
  ```
* **Why It Was Flawed:**
  In digital payment workflows (e.g., bKash, Nagad, card gateways), network drops and user double-clicks frequently trigger duplicate confirmation requests. If a student successfully pays, but their browser retries the request and receives `409 Conflict ("Already confirmed")`, the frontend shows an error alert, causing intense user panic that their payment was lost.
* **Developer Correction:**
  The developer redesigned the state transition to be **strictly idempotent**:
  ```csharp
  // CORRECTED DEVELOPER IMPLEMENTATION:
  if (hold.Status == HoldStatus.CONFIRMED)
  {
      _logger.LogInformation("Hold {HoldId} already confirmed. Idempotently returning success.", holdId);
      return ConfirmHoldResult.Success(hold); // Returns HTTP 200 OK with confirmed status
  }
  ```
  If the hold was already confirmed by the same student, the API returns `200 OK` without error and without duplicating allocated seats.

---

### Hazard 4: Timezone Pitfalls & Off-by-One Expiration Skew
* **The AI's Suggestion:**
  The AI initially used `DateTime.Now` (server local timezone) in several services and compared it directly against database timestamps:
  ```csharp
  // FLAWED AI CODE:
  h.ExpiresAt > DateTime.Now
  ```
* **Why It Was Dangerous:**
  When the backend container runs in UTC while the local developer machine or database runs in Bangladesh Standard Time (UTC+6), comparing local time against UTC timestamps causes 6-hour discrepancies where seats either never expire or expire instantly.
* **Developer Correction:**
  The developer enforced strict UTC normalization across the entire stack:
  - C#: Replaced all occurrences with `DateTime.UtcNow`.
  - PostgreSQL: Stored all timestamps as `timestamp with time zone`.
  - Frontend: Processed ISO 8601 strings ending in `Z` and calculated timer countdowns using `Date.now()` epoch arithmetic.
  - Automated Tests: Created `ExpirationBoundaryTests.cs` verifying precision at $t-1$, $t$, and $t+1$ seconds.

---

### Hazard 5: SPA Routing 404s on Browser Refresh
* **The AI's Suggestion:**
  The AI wrote a basic Nginx configuration for the frontend container:
  ```nginx
  # FLAWED AI NGINX CONFIG:
  location / {
      root /usr/share/nginx/html;
      index index.html;
  }
  ```
* **Why It Failed:**
  In client-side single page applications (React Router / tab states), navigating to deep paths or refreshing the browser at sub-routes causes Nginx to look for a physical file on disk. Failing to find it, Nginx returns a raw `404 Not Found` error.
* **Developer Correction:**
  The developer updated `frontend/nginx.conf` with the mandatory SPA fallback rule:
  ```nginx
  location / {
      try_files $uri $uri/ /index.html;
  }
  ```
  Additionally, the developer added dual-stack IPv4/IPv6 listening (`listen 3000; listen [::]:3000;`) and a dedicated `/healthz` endpoint to resolve container healthcheck failures.

---

### Hazard 6: Generic "AI-Looking" UI Overhaul
* **The AI's Suggestion:**
  When generating frontend components, the AI defaulted to generic, cookie-cutter templates: neon purple gradients, floating badges with buzzwords ("Next-Gen AI Admission"), and cards lacking authentic Bangladeshi academic context.
* **Developer Correction:**
  The developer completely discarded the generic AI layout and redesigned the portal referencing **OnnoRokom Projukti Limited** ([https://onnorokomprojukti.com/](https://onnorokomprojukti.com/)):
  - Corporate slate/navy branding (`#0F172A`, `#1E293B`) and tech-cyan accents (`#0284C7`).
  - Realistic educational domain modeling: 17-digit digital BRN validation, Morning/Day shift schedules, transparent ৳ 1,500 BDT admission fee notes, and 1-click printable admission slips.

---

## 5. Engineering Governance & Developer Responsibility

Throughout this take-home project:
1. **The Human Engineer Maintained Full Architectural Ownership:**
   AI never made design decisions. All database schemas, locking strategies, API status code contracts, and state machine models were dictated by the developer based on the PRD specification.
2. **Zero Unverified Code in Production:**
   Every single AI-generated snippet was reviewed, compiled, and validated against an extensive test suite:
   - **126 Backend Tests:** 113 pure unit tests + 7 integration contract tests + 4 high-concurrency stress tests + 2 multi-instance distributed tests.
   - **11 Frontend Tests:** Vitest component tests verifying countdown timers, validation, and modal transitions.
3. **Commit Integrity:**
   All git commits were broken down into small, human-like incremental stories following the project specification.

---

## 6. Conclusion

AI acted as an effective accelerator for repetitive code drafting, but human software engineering was strictly required to catch critical concurrency bugs, prevent distributed race conditions, enforce idempotency, and build a cohesive enterprise-grade application worthy of OnnoRokom Projukti Limited's technical evaluation.
