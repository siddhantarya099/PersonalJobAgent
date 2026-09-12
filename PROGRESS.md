# Personal AI Job Agent — Development Progress

## Current Status: Phase 3 In Progress 🚀

---

## ✅ Completed

### Phase 1 — Core Deterministic Engine

#### Step 1: Canonical Skills & Aliases
- `ISkillCanonicalizer` interface defined.
- `SkillCanonicalizer` implemented with comprehensive alias mappings:
  - Orchestration: `Apache Airflow` / `Airflow`, `ADF` / `Azure Data Factory`, `dbt` / `data build tool`, Prefect, Dagster, NiFi, Luigi.
  - Big Data: `PySpark` / `Python Spark`, `Spark SQL`, `Apache Kafka`, Flink, Hadoop, Hive, `Azure Databricks` / `Databricks`, Delta Lake.
  - Cloud: `AWS`, `GCP`, `Azure`, `ADLS` / `Azure Data Lake Storage`, S3, GCS.
  - Warehouses: Snowflake, BigQuery, Redshift, `Azure Synapse`, Trino/Presto.
  - Databases: `PostgreSQL` / `Postgres`, MySQL, `SQL Server` / `MSSQL`, Oracle, MongoDB, Cassandra, Redis, Elasticsearch.
  - Languages: Python, Scala, Java, `C#` / `.NET`, Go, R.
  - DevOps: Docker, `Kubernetes` / `K8s`, Git, `CI/CD`, Terraform.
  - BI: `Power BI` / `PowerBI`, Tableau, Looker.
- Fallback sanitization for unknown skills (case-insensitive, punctuation-stripped).
- Integrated into `JobMatchingEngine`.

#### Step 2: Deterministic Matching Engine
- `JobMatchingEngine` fully implemented with:
  - Technical Score (required 75% + preferred 25% weighting).
  - Experience Score + hard blocker (≥2 yr gap = LOW_PRIORITY cap at 59).
  - Salary Score (min/target LPA bands).
  - Location Score (Delhi NCR priority, Remote fallback, Relocation fallback).
  - Overall Score weighting: Technical 40%, Experience 25%, Salary 15%, Location 10%, Reserve 10%.
  - Recommendation bands: `STRONGLY_RECOMMENDED` ≥85, `RECOMMENDED` ≥70, `POTENTIAL` ≥55, `LOW_PRIORITY` below.
- `IJobMatchingEngine` / `JobMatchingEngine` registered in DI.
- REST endpoint: `POST /api/job-matching/evaluate` (in-memory, no DB).
- **59 automated tests passing** (58 unit + 1 integration).

---

### Phase 2 — Persistence & Data Layer

#### Step 3: PostgreSQL via Docker
- `docker-compose.yml` added — PostgreSQL 16 Alpine, port 5432, persistent volume, healthcheck.
- Connection string added to `appsettings.json` and `appsettings.Development.json`.

> **To start the database:**
> ```powershell
> docker-compose up -d
> ```

#### Step 4: EF Core DbContext & Configurations
- `PersonalJobAgentDbContext` with 6 `DbSet`s: `Candidates`, `CandidateSkills`, `Jobs`, `JobMatches`, `Applications`, `ApplicationEvents`.
- Fluent API entity configurations:
  - `candidates` — decimal precision, cascade-delete skills, property access via field.
  - `candidate_skills` — max lengths, numeric precision, index on `candidate_id`.
  - `jobs` — max lengths, nullable salary decimals, unique indexes on `external_id` and `content_hash`.
  - `job_matches` — JSONB columns for skill arrays, unique index on `(job_id, candidate_id)`, enum stored as string.
  - `applications` — enum stored as string, cascade-delete events, indexes.
  - `application_events` — enum stored as string, index on `application_id`.
- `ApplicationStatus` enum fully defined: `Discovered`, `Shortlisted`, `ReadyToApply`, `Applied`, `Assessment`, `RecruiterContacted`, `Interview`, `Offer`, `Rejected`, `Withdrawn`, `Closed`.
- `Application` entity fully implemented with `UpdateStatus()` domain method.
- `ApplicationEvent` entity created for full lifecycle audit trail.

#### Step 5: Repositories, Unit of Work & DI
- Interfaces in Application layer: `ICandidateRepository`, `IJobRepository`, `IJobMatchRepository`, `IUnitOfWork`.
- Implementations in Infrastructure: `CandidateRepository`, `JobRepository`, `JobMatchRepository`, `UnitOfWork`.
- `DependencyInjection.cs` extension method `AddInfrastructureServices()` registered in `Program.cs`.
- `dotnet-ef` local tool installed (`dotnet-tools.json`).
- **`InitialCreate` migration generated** in `src/PersonalJobAgent.Infrastructure/Persistence/Migrations/`.

> **To apply the migration to the database:**
> ```powershell
> dotnet dotnet-ef database update --project src\PersonalJobAgent.Infrastructure --startup-project src\PersonalJobAgent.Api
> ```

#### Step 6: Transition API to DB-Backed Job Analysis ✅
- Implemented `JobAnalysisService` with async job analysis pipeline:
  - Loads active candidate from DB with skills
  - Converts candidate entity to `CandidateProfile`
  - Runs `JobMatchingEngine.Evaluate()` with job profile
  - Persists `JobMatch` result with JSON-serialized skill data
- Created DTOs: `CreateJobRequest`, `JobResponse`, `AnalyzeJobRequest`, `JobMatchResponse`
- Updated `JobMatchingController` with three new endpoints:
  - `POST /api/job-matching/jobs` — Create and persist a job
  - `POST /api/job-matching/jobs/{jobId}/analyze` — Analyze job against active candidate
  - `GET /api/job-matching/jobs/{jobId}/match` — Retrieve persisted match result
- Implemented `DataSeeder` to populate initial candidate profile with 14 pre-seeded skills
- Registered `IJobAnalysisService` in Program.cs DI container
- Seeding runs automatically on app startup in Development environment
- **Build successful, no compilation errors**

---

## 🔜 Next Up

### Phase 3 — AI JD Parser

#### Step 7: AI JD Parser
- Defined `IJdParserService` and implemented `JdParserService` using the existing `IAiService` abstraction.
- Added a focused extraction prompt that returns a structured `JobProfile` and preserves the source description.
- Added validation for blank job descriptions and three passing unit tests.
- Registered `IJdParserService` in the API dependency injection container.
- **Concrete AI provider configuration is still required** before exposing a live parsing endpoint.
- Input: raw job description text.
- Output: structured `JobProfile` DTO (skills categorized into `RequiredSkills`, `PreferredSkills`, `MandatorySkills`, experience, salary, location).

#### Step 8: Combined Pipeline
- Added `POST /api/job-matching/analyze-raw`, combining `JdParserService`, `JobMatchingEngine`, and database persistence.
- Added an OpenAI `IAiService` implementation using `gpt-4o-mini` with configuration-based API key loading.

#### Step 9: Job Discovery Agent
- Added source-agnostic `IJobSource` and `IJobDiscoveryService` abstractions.
- Added deduplicating import logic using `ExternalId` and SHA-256 `ContentHash`.
- Added repository support for content-hash lookups.
- Added focused tests for new imports, duplicate external IDs, duplicate content, and malformed records; **3 tests passing**.
- Added `POST /api/job-matching/discover` for manual or integration-provided job batches.
- Added opt-in `JsonJobFeedSource` for permitted HTTPS JSON APIs/feeds.
- Added opt-in `JobDiscoveryWorker` with configurable interval; disabled by default in application settings.
- **Remaining:** configure a selected approved feed and add an integration-test harness.

---

### Phase 4 — Autonomous Agents

#### Step 9: Job Discovery Agent
- Scheduled background service to ingest jobs from official APIs/feeds.
- Deduplication via `ExternalId` and `ContentHash`.

#### Step 10: Resume Agent
- Added `IResumeTailoringService` and grounded AI tailoring using the master resume, structured job profile, and persisted match result.
- Added `POST /api/resumes/{resumeId}/tailor`, creating a new immutable `AI` resume version without overwriting the master.
- Prompt explicitly forbids invented employers, dates, achievements, skills, metrics, or education.
- Added tailoring tests for master-content grounding and empty AI output; **2 tests passing**.
- Linked applications to an exact resume version with candidate ownership validation.
- **Remaining:** add email integration and interview preparation, or begin the mobile client once the API contract is accepted.

#### Step 11: Application Agent
- Added application persistence repository and DTOs.
- Added `POST /api/applications` to create an application for the active candidate.
- Added `GET /api/applications/{applicationId}` with full event history.
- Added `PATCH /api/applications/{applicationId}/status` with audit-event creation.
- Added lifecycle transition validation in the domain and clear `400` responses for invalid transitions.
- Added paginated, status-filtered `GET /api/applications` for dashboard use.
- Added optional next-action due dates to status updates.
- Added `GET /api/applications/pending` for due or overdue actions, excluding closed and rejected applications.
- Added tests for lifecycle progression, invalid transitions, terminal states, and pending-action due dates; **4 tests passing**.
- Added provider-agnostic `INotificationService` with a safe logging implementation.
- Added manual `POST /api/applications/pending/notify` trigger.
- Added persistent notification deduplication through application events; **1 deduplication test passing**.
- **Remaining:** choose a real notification channel and add a scheduled trigger.

#### Step 12: Email Agent
- Scans inbox for recruiter messages, classifies, maps to `Application` status, logs `ApplicationEvent`.

---

### Phase 5 — UI & Analytics

#### Step 13: React Native Mobile UI
- Started separate Expo TypeScript app in `mobile/`.
- Added initial dashboard for pasting raw job descriptions, calling the analysis API, viewing match results, and browsing saved jobs.
- Added API base URL configuration in the initial screen; currently targets local development at `http://localhost:5251/api`.
- Extracted jobs, analysis, and application-summary calls into `mobile/src/api.ts`.
- Connected live application summary data to Home metrics and the Applications tab.
- Connected live application list data to the Applications tab, including status, next action, and timeline event count.
- **Remaining:** add application status actions, resume loading/version UI, and support device-network configuration.
- Pending actions feed.
- Push notifications for status changes.

#### Step 14: Interview Prep Agent
- Generates custom interview questions and prep materials based on `JobProfile` and candidate skill gaps.

#### Step 15: Career Analytics
- Score trends over time, application funnel metrics, skill gap reports.

