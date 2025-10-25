# Data Storage

**Document Version:** 1.0  
**Last Updated:** October 24, 2025  
**System Version:** 0.2.0

---

## Table of Contents

1. [Overview](#overview)
2. [PostgreSQL Schema](#postgresql-schema)
3. [Multi-Tenant Data Isolation](#multi-tenant-data-isolation)
4. [Redis Caching Strategy](#redis-caching-strategy)
5. [Data Migration Strategy](#data-migration-strategy)
6. [Backup and Recovery](#backup-and-recovery)

---

## 1. Overview

EduMind.AI uses a **dual-storage architecture** combining PostgreSQL for persistent data and Redis for caching and session management.

### Storage Components

| Component | Technology | Purpose | Data Retention |
|-----------|------------|---------|----------------|
| **Primary Database** | PostgreSQL 16+ | Student data, assessments, responses | Indefinite (active data) |
| **Cache Layer** | Redis 7+ | Session state, query caching, SignalR backplane | Ephemeral (TTL-based) |
| **Blob Storage** | Azure Blob (future) | Question attachments, student uploads | Per retention policy |
| **Backup Storage** | PostgreSQL PITR + Azure Blob | Disaster recovery | 7-35 days |

### Data Flow Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Application Layer                       │
│            (Web API, Dashboard, Student App)                │
└─────────────────────────────────────────────────────────────┘
                        ↓                ↑
         ┌──────────────┴────────────────┴──────────────┐
         │                                               │
    [Write Path]                                  [Read Path]
         │                                               │
         ↓                                               ↑
┌────────────────────┐                       ┌───────────────────┐
│   Redis Cache      │←─────────[Cache]──────│  Query Result     │
│   (Read-Through)   │                       │  Set TTL: 5 min   │
└────────────────────┘                       └───────────────────┘
         │                                               ↑
    [Cache Miss]                                   [Cache Hit]
         │                                               │
         ↓                                               │
┌────────────────────────────────────────────────────────────────┐
│                    PostgreSQL Database                         │
│                                                                │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐    │
│  │ Self-Service  │  │  School DB 1  │  │  School DB N  │    │
│  │   Database    │  │   (Isolated)  │  │   (Isolated)  │    │
│  └───────────────┘  └───────────────┘  └───────────────┘    │
│                                                                │
│  [Physical Database Isolation for B2B Schools]                │
└────────────────────────────────────────────────────────────────┘
```

---

## 2. PostgreSQL Schema

### 2.1 Database Structure

**Entity Framework Core Context:** `AcademicContext`  
**Migration History:** 2 migrations applied  
**Total Tables:** 13 core tables + 4 orchestration tables

### 2.2 Core Tables

#### users

Student/teacher/admin authentication and role management.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| `id` | UUID | PRIMARY KEY | User identifier |
| `email` | VARCHAR(255) | NOT NULL, UNIQUE | Login email |
| `full_name` | VARCHAR(255) | NOT NULL | Display name |
| `role` | INT | NOT NULL | UserRole enum (0-5) |
| `school_id` | UUID | NULL | School affiliation |
| `is_active` | BOOLEAN | NOT NULL | Account status |
| `external_id` | VARCHAR(255) | UNIQUE | Azure AD B2C subject |
| `created_at` | TIMESTAMPTZ | NOT NULL | Account creation |
| `updated_at` | TIMESTAMPTZ | NOT NULL | Last modified |

**Indexes:**

- `email` (UNIQUE)
- `external_id` (UNIQUE)
- `school_id`
- `role`

---

#### schools

Educational institutions with dedicated databases.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| `id` | UUID | PRIMARY KEY | School identifier |
| `name` | VARCHAR(255) | NOT NULL | School name |
| `code` | VARCHAR(50) | NOT NULL, UNIQUE | Short identifier |
| `address` | VARCHAR(500) | NOT NULL | Physical location |
| `contact_email` | VARCHAR(255) | NOT NULL | Admin contact |
| `contact_phone` | VARCHAR(50) | NULL | Phone number |
| `is_active` | BOOLEAN | NOT NULL | Subscription status |
| `created_at` | TIMESTAMPTZ | NOT NULL | Onboarding date |
| `updated_at` | TIMESTAMPTZ | NOT NULL | Last modified |

**Computed Properties (not stored):**

- `connection_string_key` = `"School-{Id}-ConnectionString"`
- `database_name` = `"edumind_school_{code}_{id}"`

**Indexes:**

- `code` (UNIQUE)
- `is_active`

---

#### classes

Student groups with teacher assignments.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| `id` | UUID | PRIMARY KEY | Class identifier |
| `school_id` | UUID | NOT NULL | Parent school |
| `name` | VARCHAR(255) | NOT NULL | Class name |
| `code` | VARCHAR(50) | NOT NULL | School-unique code |
| `grade_level` | INT | NOT NULL | GradeLevel enum (6-12) |
| `subject` | INT | NOT NULL | Subject enum |
| `teacher_ids` | TEXT | NOT NULL | JSON array of UUIDs |
| `student_ids` | TEXT | NOT NULL | JSON array of UUIDs |
| `academic_year` | VARCHAR(20) | NOT NULL | "2024-2025" |
| `is_active` | BOOLEAN | NOT NULL | Class status |
| `created_at` | TIMESTAMPTZ | NOT NULL | Class creation |
| `updated_at` | TIMESTAMPTZ | NOT NULL | Last modified |

**Indexes:**

- `school_id`
- `(school_id, code)` (UNIQUE)
- `grade_level`
- `subject`

---

#### students

Learners with gamification features.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| `id` | UUID | PRIMARY KEY | Student identifier |
| `user_id` | UUID | NOT NULL, UNIQUE | References users.id |
| `school_id` | UUID | NULL | NULL for self-service |
| `class_ids` | TEXT | NOT NULL | JSON array of UUIDs |
| `grade_level` | INT | NOT NULL | GradeLevel enum |
| `date_of_birth` | DATE | NULL | COPPA compliance |
| `parental_consent_granted` | BOOLEAN | NOT NULL | Under-13 consent |
| `parent_email` | VARCHAR(255) | NULL | Parent contact |
| `subscription_tier` | INT | NOT NULL | SubscriptionTier enum |
| `subscription_expires_at` | TIMESTAMPTZ | NULL | Subscription end |
| `level` | INT | NOT NULL | Gamification level |
| `xp_points` | INT | NOT NULL | Experience points |
| `daily_streak` | INT | NOT NULL | Consecutive days |
| `last_activity_date` | DATE | NULL | Last active date |
| `created_at` | TIMESTAMPTZ | NOT NULL | Enrollment date |
| `updated_at` | TIMESTAMPTZ | NOT NULL | Last modified |

**Indexes:**

- `user_id` (UNIQUE)
- `school_id`
- `grade_level`
- `subscription_tier`

---

#### courses

Curriculum definitions (global, not school-specific).

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| `id` | UUID | PRIMARY KEY | Course identifier |
| `name` | VARCHAR(255) | NOT NULL | Course name |
| `code` | VARCHAR(50) | NOT NULL, UNIQUE | Unique identifier |
| `subject` | INT | NOT NULL | Subject enum |
| `grade_level` | INT | NOT NULL | GradeLevel enum |
| `description` | TEXT(2000) | NOT NULL | Course description |
| `learning_objectives` | TEXT | NOT NULL | JSON array of strings |
| `topics` | TEXT | NOT NULL | JSON array of strings |
| `board_name` | VARCHAR(100) | NULL | "CBSE", "ICSE", "IB" |
| `module_name` | VARCHAR(200) | NULL | Topic grouping |
| `metadata` | JSONB | NOT NULL | Flexible key-value pairs |
| `is_active` | BOOLEAN | NOT NULL | Course status |
| `course_admin_id` | UUID | NULL | Owner |
| `created_at` | TIMESTAMPTZ | NOT NULL | Course creation |
| `updated_at` | TIMESTAMPTZ | NOT NULL | Last modified |

**Indexes:**

- `code` (UNIQUE)
- `subject`
- `grade_level`
- `board_name`
- `module_name`

---

#### assessments

Collections of questions.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| `id` | UUID | PRIMARY KEY | Assessment identifier |
| `course_id` | UUID | NOT NULL | Parent course |
| `school_id` | UUID | NULL | NULL for global |
| `title` | VARCHAR(255) | NOT NULL | Assessment title |
| `description` | TEXT(2000) | NOT NULL | Instructions |
| `assessment_type` | INT | NOT NULL | AssessmentType enum |
| `subject` | INT | NOT NULL | Subject enum |
| `grade_level` | INT | NOT NULL | GradeLevel enum |
| `topics` | TEXT | NOT NULL | JSON array of strings |
| `question_ids` | TEXT | NOT NULL | JSON array of UUIDs (ordered) |
| `total_points` | INT | NOT NULL | Sum of question points |
| `time_limit_minutes` | INT | NULL | NULL = untimed |
| `passing_score_percentage` | INT | NOT NULL | Default: 70 |
| `is_active` | BOOLEAN | NOT NULL | Assessment status |
| `created_at` | TIMESTAMPTZ | NOT NULL | Creation date |
| `updated_at` | TIMESTAMPTZ | NOT NULL | Last modified |

**Indexes:**

- `course_id`
- `school_id`
- `assessment_type`
- `subject`
- `grade_level`

---

#### questions

Individual assessment questions with IRT parameters.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| `id` | UUID | PRIMARY KEY | Question identifier |
| `course_id` | UUID | NOT NULL | Parent course |
| `question_text` | TEXT(4000) | NOT NULL | Question content |
| `question_type` | INT | NOT NULL | QuestionType enum |
| `subject` | INT | NOT NULL | Subject enum |
| `grade_level` | INT | NOT NULL | GradeLevel enum |
| `difficulty_level` | INT | NOT NULL | DifficultyLevel enum |
| `topics` | TEXT | NOT NULL | JSON array of strings |
| `learning_objectives` | TEXT | NOT NULL | JSON array of strings |
| `answer_options` | TEXT(4000) | NULL | JSON array (for MCQ) |
| `correct_answer` | TEXT(4000) | NOT NULL | JSON (type-dependent) |
| `explanation` | TEXT(4000) | NULL | Answer explanation |
| `points` | INT | NOT NULL | Score value |
| `irt_discrimination` | DOUBLE | NULL | IRT 'a' parameter |
| `irt_difficulty` | DOUBLE | NULL | IRT 'b' parameter |
| `irt_guessing` | DOUBLE | NULL | IRT 'c' parameter |
| `board_name` | VARCHAR(100) | NULL | Educational board |
| `module_name` | VARCHAR(200) | NULL | Module grouping |
| `metadata` | JSONB | NOT NULL | Flexible key-value pairs |
| `is_active` | BOOLEAN | NOT NULL | Question status |
| `times_answered` | INT | NOT NULL | Usage count |
| `times_correct` | INT | NOT NULL | Correct count |
| `is_ai_generated` | BOOLEAN | NOT NULL | Generated by LLM |
| `content_hash` | VARCHAR(64) | NULL | SHA256 (duplicate detection) |
| `created_at` | TIMESTAMPTZ | NOT NULL | Creation date |
| `updated_at` | TIMESTAMPTZ | NOT NULL | Last modified |

**Indexes:**

- `course_id`
- `subject`
- `grade_level`
- `difficulty_level`
- `content_hash`
- `is_ai_generated`
- `board_name`
- `module_name`

---

#### student_assessments

Assessment attempts by students.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| `id` | UUID | PRIMARY KEY | Attempt identifier |
| `student_id` | UUID | NOT NULL | Student taking assessment |
| `assessment_id` | UUID | NOT NULL | Assessment being taken |
| `school_id` | UUID | NULL | School context |
| `class_id` | UUID | NULL | Class context |
| `status` | INT | NOT NULL | AssessmentStatus enum |
| `started_at` | TIMESTAMPTZ | NULL | Start time |
| `completed_at` | TIMESTAMPTZ | NULL | Completion time |
| `score` | INT | NULL | Final score |
| `max_score` | INT | NOT NULL | Total possible points |
| `passed` | BOOLEAN | NULL | Pass/fail |
| `current_question_index` | INT | NOT NULL | Question position (0-based) |
| `estimated_ability` | DOUBLE | NULL | IRT theta |
| `time_spent_seconds` | INT | NULL | Total time |
| `correct_answers` | INT | NOT NULL | Correct count |
| `incorrect_answers` | INT | NOT NULL | Incorrect count |
| `skipped_questions` | INT | NOT NULL | Skipped count |
| `feedback` | TEXT(4000) | NULL | Overall feedback |
| `recommendations` | TEXT | NOT NULL | JSON array of strings |
| `xp_earned` | INT | NOT NULL | Experience points awarded |
| `created_at` | TIMESTAMPTZ | NOT NULL | Attempt creation |
| `updated_at` | TIMESTAMPTZ | NOT NULL | Last modified |

**Indexes:**

- `student_id`
- `assessment_id`
- `(student_id, assessment_id)`
- `school_id`
- `class_id`
- `status`
- `completed_at`

---

#### student_responses

Individual question answers.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| `id` | UUID | PRIMARY KEY | Response identifier |
| `student_assessment_id` | UUID | NOT NULL | Parent attempt |
| `student_id` | UUID | NOT NULL | Student |
| `question_id` | UUID | NOT NULL | Question answered |
| `school_id` | UUID | NULL | School context |
| `student_answer` | TEXT(4000) | NOT NULL | Answer JSON |
| `is_correct` | BOOLEAN | NOT NULL | Correctness |
| `points_earned` | INT | NOT NULL | Points awarded |
| `max_points` | INT | NOT NULL | Total possible |
| `time_spent_seconds` | INT | NOT NULL | Time on question |
| `question_order` | INT | NOT NULL | 0-based position |
| `ability_at_time` | DOUBLE | NULL | IRT theta snapshot |
| `feedback` | TEXT(4000) | NULL | Per-question feedback |
| `ai_explanation` | TEXT(4000) | NULL | LLM-generated explanation |
| `submitted_at` | TIMESTAMPTZ | NOT NULL | Answer submission |
| `created_at` | TIMESTAMPTZ | NOT NULL | Record creation |

**Indexes:**

- `student_assessment_id`
- `student_id`
- `question_id`
- `(student_assessment_id, question_id)` (UNIQUE)
- `school_id`

---

### 2.3 Orchestration Tables

#### workflow_executions

Multi-agent workflow tracking.

| Column | Type | Purpose |
|--------|------|---------|
| `execution_id` | VARCHAR(100) | PRIMARY KEY |
| `tenant_id` | VARCHAR(100) | Multi-tenant isolation |
| `workflow_name` | VARCHAR(200) | Workflow type |
| `status` | VARCHAR(50) | Pending/Running/Completed/Failed |
| `workflow_definition_json` | TEXT | Workflow configuration |
| `started_at` | TIMESTAMPTZ | Execution start |
| `completed_at` | TIMESTAMPTZ | Execution end |
| `initiated_by` | VARCHAR(100) | User/agent |
| `tags` | VARCHAR(500) | Searchable metadata |

---

#### circuit_breaker_states

Agent circuit breaker tracking (resilience).

| Column | Type | Purpose |
|--------|------|---------|
| `agent_key` | VARCHAR(200) | PRIMARY KEY (agent identifier) |
| `tenant_id` | VARCHAR(100) | Multi-tenant isolation |
| `state` | VARCHAR(20) | Closed/Open/HalfOpen |
| `failure_count` | INT | Consecutive failures |
| `last_failure_at` | TIMESTAMPTZ | Last failure time |
| `last_updated` | TIMESTAMPTZ | State change time |

---

#### routing_decisions

Agent selection audit trail.

| Column | Type | Purpose |
|--------|------|---------|
| `id` | UUID | PRIMARY KEY |
| `tenant_id` | VARCHAR(100) | Multi-tenant isolation |
| `task_id` | VARCHAR(100) | Task being routed |
| `task_type` | VARCHAR(100) | Task category |
| `required_capability` | VARCHAR(100) | Required skill |
| `selected_agent` | VARCHAR(200) | Chosen agent |
| `reason` | TEXT | Selection justification |
| `routing_score` | DOUBLE | Routing algorithm score |
| `workflow_execution_id` | VARCHAR(100) | Parent workflow |
| `workflow_step_id` | VARCHAR(100) | Step identifier |
| `routed_at` | TIMESTAMPTZ | Decision time |

---

#### routing_statistics

Agent performance metrics.

| Column | Type | Purpose |
|--------|------|---------|
| `agent_key` | VARCHAR(200) | PRIMARY KEY (agent identifier) |
| `tenant_id` | VARCHAR(100) | Multi-tenant isolation |
| `total_tasks` | INT | Total tasks assigned |
| `successful_tasks` | INT | Completed successfully |
| `failed_tasks` | INT | Failed tasks |
| `average_duration_ms` | INT | Avg execution time |
| `success_rate` | DOUBLE | Success percentage |
| `last_updated` | TIMESTAMPTZ | Stats update time |

---

## 3. Multi-Tenant Data Isolation

### 3.1 Physical Database Partitioning (B2B Schools)

Each school gets a dedicated database for absolute isolation:

```
PostgreSQL Server (Flexible Server)
│
├── Database: edumind_selfservice          # B2C self-service students
│   └── All 13 tables with SchoolId = NULL
│
├── Database: edumind_school_ABC_00001     # School 1
│   └── All 13 tables (SchoolId = 00001)
│
└── Database: edumind_school_XYZ_00002     # School 2
    └── All 13 tables (SchoolId = 00002)
```

**Benefits:**

- **Impossible to leak data** across schools (physical isolation)
- **Independent scaling** per school
- **Dedicated backups** per school
- **Compliance friendly** (data residency, GDPR right-to-be-forgotten)

### 3.2 Row-Level Security (Self-Service)

Self-service students share the `edumind_selfservice` database with automatic query filters:

```csharp
// AcademicContext.ApplyTenantFilters()
modelBuilder.Entity<Student>()
    .HasQueryFilter(e => e.SchoolId == _tenantContext.SchoolId || 
                         _tenantContext.IsSelfService && e.SchoolId == null);
```

**Filter Rules:**

- **System/Business Admins:** Bypass all filters
- **School Users:** See only their school's data (`SchoolId` match)
- **Self-Service Students:** See only own data (`UserId` match)

### 3.3 Connection String Management

```csharp
// Multi-database routing
var connectionString = school.Id == null 
    ? GetConnectionString("SelfService")
    : GetConnectionString($"School-{school.Id}");

services.AddDbContext<AcademicContext>(options =>
    options.UseNpgsql(connectionString));
```

**Connection Strings Stored In:**

- **Development:** `appsettings.Development.json`
- **Production:** Azure Key Vault (retrieved at runtime)

---

## 4. Redis Caching Strategy

### 4.1 Cache Architecture

```
┌────────────────────────────────────────────────────────────┐
│                  Redis Cache (localhost:6379)              │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  [Session Store]                                           │
│  • User sessions (30-minute sliding expiration)           │
│  • Authentication tokens                                   │
│  • CSRF tokens                                             │
│                                                            │
│  [Query Cache]                                             │
│  • Student progress (5-minute TTL)                         │
│  • Assessment metadata (15-minute TTL)                     │
│  • Class rosters (10-minute TTL)                           │
│  • Course/question lists (1-hour TTL)                      │
│                                                            │
│  [SignalR Backplane]                                       │
│  • Real-time message distribution                          │
│  • Connection tracking                                     │
│  • Group subscriptions                                     │
│                                                            │
│  [Distributed Lock]                                        │
│  • Concurrent assessment prevention                        │
│  • Circuit breaker state                                   │
│  • Agent routing coordination                              │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 4.2 Cache Keys Convention

| Key Pattern | Example | TTL | Purpose |
|-------------|---------|-----|---------|
| `session:{userId}` | `session:a1b2c3...` | 30 min | User session state |
| `student:{id}:progress` | `student:123:progress` | 5 min | Student progress |
| `assessment:{id}:meta` | `assessment:456:meta` | 15 min | Assessment metadata |
| `class:{id}:roster` | `class:789:roster` | 10 min | Student list |
| `course:{id}:questions` | `course:abc:questions` | 1 hour | Question bank |
| `lock:assessment:{attemptId}` | `lock:assessment:xyz` | 5 min | Concurrent prevention |

### 4.3 Cache Invalidation Strategies

**Write-Through Cache:**

```csharp
// Update both database and cache
await _repository.UpdateStudentAsync(student);
await _cache.SetAsync($"student:{student.Id}", student, TimeSpan.FromMinutes(5));
```

**Cache-Aside (Lazy Loading):**

```csharp
// Check cache first, load from DB if miss
var cached = await _cache.GetAsync<Student>($"student:{studentId}");
if (cached == null)
{
    cached = await _repository.GetStudentAsync(studentId);
    await _cache.SetAsync($"student:{studentId}", cached, TimeSpan.FromMinutes(5));
}
return cached;
```

**Explicit Invalidation:**

```csharp
// Clear cache on data change
await _repository.UpdateClassRosterAsync(classId, newStudents);
await _cache.RemoveAsync($"class:{classId}:roster");
```

### 4.4 Redis Configuration

**Development (Docker):**

```yaml
redis:
  image: redis:7-alpine
  ports:
    - "6379:6379"
  volumes:
    - redis-data:/data
```

**Production (Azure Cache for Redis):**

- **Tier:** Standard C1 (1 GB)
- **Persistence:** RDB snapshots every 15 minutes
- **High Availability:** Zone-redundant (replicas)
- **SSL:** Required (TLS 1.2+)
- **Access:** Private endpoint only

---

## 5. Data Migration Strategy

### 5.1 Entity Framework Core Migrations

**Migration Files:**

- `20251015005710_InitialCreate.cs` - Initial schema
- `20251015212949_AddContentMetadataFields.cs` - Added BoardName, ModuleName, Metadata

**Creating Migrations:**

```bash
dotnet ef migrations add MigrationName \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web \
  --output-dir Data/Migrations
```

**Applying Migrations:**

```bash
# Development
dotnet ef database update \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web

# Production (Azure)
dotnet ef database update \
  --connection "$AZURE_DB_CONNECTION_STRING"
```

### 5.2 Multi-Database Migration

For schools with dedicated databases:

```bash
# Iterate over all school databases
for school_id in $(az postgres flexible-server db list --resource-group rg-prod --server-name edumind-postgres --query "[?starts_with(name, 'edumind_school')].name" -o tsv); do
  echo "Migrating $school_id..."
  dotnet ef database update \
    --connection "Host=edumind-postgres.postgres.database.azure.com;Database=$school_id;..."
done
```

### 5.3 Data Seeding

**Seed Data Scripts:**

- `scripts/seed-demo-data-final.sql` - Demo courses, questions, assessments
- Applied via `AcademicContext.OnModelCreating()` or manual execution

---

## 6. Backup and Recovery

### 6.1 PostgreSQL Backup Strategy

**Automated Backups:**

- **Continuous Transaction Logs:** Every 5 minutes
- **Full Backup:** Daily at 2 AM UTC
- **Retention:** 7 days (dev), 35 days (production)
- **Storage:** Geo-redundant in production

**Point-in-Time Restore (PITR):**

```bash
az postgres flexible-server restore \
  --resource-group rg-prod \
  --name edumind-postgres-prod-restore \
  --source-server edumind-postgres-prod \
  --restore-time "2025-10-24T14:30:00Z"
```

**Manual Backup Before Migrations:**

```bash
# Create backup
pg_dump -h edumind-postgres.postgres.database.azure.com \
  -U edumindadmin \
  -d edumind_selfservice \
  --format=custom \
  --file="backup-$(date +%Y%m%d-%H%M).dump"

# Upload to Azure Blob
az storage blob upload \
  --account-name edumindbackups \
  --container-name database-exports \
  --file "backup-$(date +%Y%m%d-%H%M).dump"
```

**Restore from Backup:**

```bash
pg_restore -h edumind-postgres.postgres.database.azure.com \
  -U edumindadmin \
  -d edumind_restored \
  --clean --if-exists \
  backup.dump
```

### 6.2 Redis Persistence

**RDB Snapshots:**

- **Frequency:** Every 15 minutes (if data changed)
- **Location:** `/data/dump.rdb` (Docker volume)
- **Production:** Azure Cache RDB snapshots + geo-replication

**AOF (Append-Only File) - Disabled:**

- Not enabled for performance (ephemeral cache data)
- Session state can be rebuilt from database

---

## Related Documentation

- **03-domain-model.md** - Entity definitions and relationships
- **04-application-components.md** - Infrastructure component implementation
- **07-security-privacy.md** - Multi-tenant isolation rules and RBAC
- **12-performance.md** - Query optimization and caching strategies

---

**Schema Version:** EF Core 8.0.10  
**PostgreSQL Version:** 16  
**Redis Version:** 7-alpine
