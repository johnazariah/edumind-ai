# Task Tracker: Story 018 - Aspire Deployment Parity

**Story:** p1-018-aspire-deployment-parity-local-remote  
**Started:** 2025-10-25  
**Status:** In Progress

---

## Task List

### Phase 1: Core Aspire Integration (7 hours)

- [x] **Task 1:** Update AppHost to Use Aspire Resource APIs (4 hours)
  - Convert AddConnectionString to AddPostgres/AddRedis
  - Configure service references
  - Test local startup
  - **Completed:** 2025-10-25
  
- [x] **Task 2:** Add Aspire NuGet Packages to AppHost (1 hour)
  - Install Aspire.Hosting.PostgreSQL
  - Install Aspire.Hosting.Redis
  - Verify builds
  - **Completed:** 2025-10-25 (packages already present)

- [x] **Task 3:** Update Web API to Use Aspire Service Discovery (3 hours)
  - Add Aspire client packages
  - Remove hardcoded connection strings
  - Test database and Redis connections
  - **Completed:** 2025-10-25

### Phase 2: Service Communication (6 hours)

- [x] **Task 4:** Update Dashboard to Use Service Discovery for Web API (2 hours)
  - Add service discovery packages
  - Replace hardcoded API URLs
  - Test HTTP communication
  - **Completed:** 2025-10-25

- [x] **Task 5:** Update Student App to Use Service Discovery (2 hours)
  - Same as Task 4 for Student App
  - Test API calls and SignalR
  - **Completed:** 2025-10-25

- [ ] **Task 8:** Add Environment Detection Logic (2 hours)
  - Ollama for local, Azure OpenAI for production
  - Environment-specific configuration

### Phase 3: Azure Infrastructure (9 hours)

- [x] **Task 6:** Update Bicep Templates for Aspire Azure Deployment (6 hours)
  - Azure PostgreSQL Flexible Server
  - Service bindings for Container Apps
  - Managed identity configuration
  - **Completed:** 2025-10-25 (used azd infra generate from updated AppHost)

- [ ] **Task 7:** Update Azure Deployment Workflow (3 hours)
  - Update azure.yaml for Aspire
  - Test azd deploy

### Phase 4: Testing & Documentation (11 hours)

- [ ] **Task 9:** Update Integration Tests for Service Discovery (4 hours)
  - Use Aspire.Hosting.Testing
  - Remove hardcoded URLs from tests
  - Verify all tests pass

- [ ] **Task 10:** Update Documentation (3 hours)
  - Update deployment playbooks
  - Revise architecture docs
  - Add troubleshooting guide

- [ ] **Task 11:** Local Testing & Validation (4 hours)
  - Complete local test checklist
  - Verify all services healthy
  - Run integration test suite

### Phase 5: Production Deployment (6 hours)

- [ ] **Task 12:** Azure Deployment & Validation (6 hours)
  - Deploy to Azure
  - Validate service discovery
  - Run smoke tests

---

## Progress Tracking

**Total Tasks:** 12  
**Completed:** 6  
**In Progress:** 1 (Task 11 - Local Testing)  
**Blocked:** 0  

**Estimated Total Effort:** 39 hours (~2 weeks)  
**Actual Time Spent:** ~15 hours

---

## Notes

### 2025-10-25: Task 6 Complete - Azure Bicep Generation

**Approach:**
Instead of manually updating Bicep templates, used `azd infra generate` to auto-generate Aspire-compatible infrastructure from AppHost configuration.

**Key Changes:**

1. Added Azure hosting packages to AppHost:
   - `Aspire.Hosting.Azure.PostgreSQL` 9.5.1
   - `Aspire.Hosting.Azure.Redis` 9.5.1

2. Updated AppHost with publish mode detection:

   ```csharp
   if (builder.ExecutionContext.IsPublishMode) {
       // Azure: AddAzurePostgresFlexibleServer, AddAzureRedis
   } else {
       // Local: AddPostgres, AddRedis (containers)
   }
   ```

3. Generated modular Bicep files:
   - `infra/postgres/postgres.module.bicep` - Azure PostgreSQL Flexible Server with Entra ID auth
   - `infra/cache/cache.module.bicep` - Azure Cache for Redis with Entra ID auth
   - `infra/postgres-roles/` - RBAC assignments
   - `infra/cache-roles/` - RBAC assignments

**Benefits:**

- No manual Bicep authoring - generated from code
- Service bindings automatically configured
- Entra ID authentication (passwordless)
- Modern API versions (2024-11-01 Redis, 2024-08-01 Postgres)
- `aspire-resource-name` tags for service discovery

**Next:** Test local deployment (Task 11), then Azure deployment (Task 12)

### 2025-10-25: Tasks 1-5 Complete

**Completed:**

- Task 1: AppHost converted to Aspire resource APIs (AddPostgres, AddRedis, AddContainer)
- Task 2: Aspire packages verified (already present in AppHost)
- Task 3: Web API updated for Aspire service discovery (AddNpgsqlDbContext, AddRedisClient)
- Task 4: Dashboard updated with Aspire service discovery
- Task 5: Student App updated with Aspire service discovery

**Key Learnings:**

- Aspire 9.5.1 requires Microsoft.Extensions packages v9.x (upgraded from 8.x)
- Package version conflicts resolved by explicitly specifying Configuration.Abstractions 9.0.9
- All builds succeeding with new service discovery configuration

**Pattern Established:**

```csharp
// AppHost defines resources:
var postgres = builder.AddPostgres("postgres").AddDatabase("edumind");
var redis = builder.AddRedis("cache");

// Apps consume via service discovery:
builder.AddNpgsqlDbContext<AcademicContext>("edumind");
builder.AddRedisClient("cache");
builder.Services.AddHttpClient("ApiClient", c => c.BaseAddress = new Uri("http://webapi"))
    .AddServiceDiscovery();
```

### 2025-10-25: Story Created

- Comprehensive specification written
- 12 tasks defined with clear acceptance criteria
- Ready to begin implementation

---

## Current Focus

**Next Task:** Task 6 - Update Bicep Templates for Aspire Azure Deployment (6 hours)

**Blockers:** None

**Questions:**

- Should we keep Ollama for local dev or switch to Azure OpenAI everywhere? → Keeping Ollama local per spec
- PgAdmin for local development - include or skip? → Skipping for now, can add later
