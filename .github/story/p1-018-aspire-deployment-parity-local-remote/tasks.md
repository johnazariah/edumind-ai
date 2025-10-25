# Task Tracker: Story 018 - Aspire Deployment Parity

**Story:** p1-018-aspire-deployment-parity-local-remote  
**Started:** 2025-10-25  
**Status:** In Progress

---

## Task List

### Phase 1: Core Aspire Integration (7 hours)

- [ ] **Task 1:** Update AppHost to Use Aspire Resource APIs (4 hours)
  - Convert AddConnectionString to AddPostgres/AddRedis
  - Configure service references
  - Test local startup
  
- [ ] **Task 2:** Add Aspire NuGet Packages to AppHost (1 hour)
  - Install Aspire.Hosting.PostgreSQL
  - Install Aspire.Hosting.Redis
  - Verify builds

- [ ] **Task 3:** Update Web API to Use Aspire Service Discovery (3 hours)
  - Add Aspire client packages
  - Remove hardcoded connection strings
  - Test database and Redis connections

### Phase 2: Service Communication (6 hours)

- [ ] **Task 4:** Update Dashboard to Use Service Discovery for Web API (2 hours)
  - Add service discovery packages
  - Replace hardcoded API URLs
  - Test HTTP communication

- [ ] **Task 5:** Update Student App to Use Service Discovery (2 hours)
  - Same as Task 4 for Student App
  - Test API calls and SignalR

- [ ] **Task 8:** Add Environment Detection Logic (2 hours)
  - Ollama for local, Azure OpenAI for production
  - Environment-specific configuration

### Phase 3: Azure Infrastructure (9 hours)

- [ ] **Task 6:** Update Bicep Templates for Aspire Azure Deployment (6 hours)
  - Azure PostgreSQL Flexible Server
  - Service bindings for Container Apps
  - Managed identity configuration

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
**Completed:** 0  
**In Progress:** 0  
**Blocked:** 0  

**Estimated Total Effort:** 39 hours (~2 weeks)  
**Actual Time Spent:** 0 hours

---

## Notes

### 2025-10-25: Story Created

- Comprehensive specification written
- 12 tasks defined with clear acceptance criteria
- Ready to begin implementation

---

## Current Focus

**Next Task:** Task 1 - Update AppHost to Use Aspire Resource APIs

**Blockers:** None

**Questions:**

- Should we keep Ollama for local dev or switch to Azure OpenAI everywhere?
- PgAdmin for local development - include or skip?
