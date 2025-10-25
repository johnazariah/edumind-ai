# Analysis Story: Architectural Decision Records

## Objective

Create a comprehensive `adr.md` that summarizes all significant architectural decisions made during the EduMind.AI project development, with each decision captured as a clear paragraph.

## Context

Throughout the project, numerous architectural decisions have been made regarding technology choices, patterns, deployment strategies, and system design. These decisions are currently scattered across:

- Git commit messages (especially "Fix:" and "feat:" commits)
- Architecture documents
- Deployment documentation
- Code comments
- PR descriptions

## Task Instructions

### 1. Review Existing Architecture Documentation

Analyze:

- `docs/architecture/**/*.md` - All architecture documents
- Look for explicit decisions about:
  - Technology selections
  - Pattern choices
  - Security approaches
  - Scalability strategies

### 2. Extract Decisions from Deployment Journey

Review deployment documentation:

- `docs/deployment/AZURE_DEPLOYMENT_STRATEGY.md`
- `docs/deployment/PRAGMATIC_POSTGRES_SOLUTION.md`
- `docs/deployment/TEMPLATE_SUBSTITUTION_ISSUE.md`
- `docs/deployment/POSTGRESQL_AZURE_FILES_FIX.md`
- `docs/deployment/RUNTIME_DOMAIN_DETECTION.md`
- `docs/deployment/FQDN_PATCHING_FIX.md`

These documents reveal decisions about:

- Azure Database for PostgreSQL vs containerized
- Template variable substitution approaches
- FQDN detection strategies
- Connection string management

### 3. Analyze Git History for Architectural Changes

Review commits focusing on:

```bash
git log --all --grep="feat:" --grep="Fix:" --grep="Migrate" --grep="Switch" --oneline
```

Key decision indicators:

- "Migrate to X" - Technology change decisions
- "Switch from X to Y" - Pattern or approach changes
- "Use X instead of Y" - Alternative selection
- "Revert" commits - Decisions that were reversed

### 4. Extract Technology Choices from Codebase

**Framework Decisions:**

- .NET 9.0 selection
- Blazor Server vs WebAssembly choice
- ASP.NET Core patterns

**AI/LLM Decisions:**

- Semantic Kernel adoption
- Ollama for local LLM vs cloud APIs
- Multi-agent architecture

**Infrastructure Decisions:**

- .NET Aspire for orchestration
- Azure Container Apps vs other Azure compute
- PostgreSQL vs other databases
- Redis for caching

**Frontend Decisions:**

- Blazor component patterns
- State management approach
- Navigation strategies (note: recent HTML link workaround)

### 5. Review Authentication & Security Decisions

Analyze:

- `docs/architecture/RBAC_ARCHITECTURE.md`
- `docs/architecture/PRIVACY_AND_SECURITY.md`
- `docs/deployment/AUTHENTICATION_SETUP.md`
- Code in authentication middleware

Decisions about:

- OIDC vs client-secret authentication
- Role-based access control design
- Data encryption strategies
- Privacy compliance approaches

### 6. Extract Data Architecture Decisions

Review:

- Database migration history
- `scripts/seed-demo-data*.sql` evolution
- Entity Framework patterns in Infrastructure project
- Caching strategies in code

### 7. Identify Integration Decisions

Analyze:

- API versioning strategy
- Service discovery patterns
- Container orchestration approach
- Health check implementations

### 8. Document Deployment & Operations Decisions

Review:

- CI/CD workflow choices (`.github/workflows/`)
- Container image sources (MCR vs Docker Hub)
- Environment configuration strategies
- Monitoring and observability approaches

## Expected Output Structure

Create a top-level document `adr\introduction.md` with:

### Introduction

- Purpose of this ADR document
- How decisions are organized
- Context about the project

### Architectural Decision Records

For each decision, create a document named `adr\{ADR_NUMBER}-{ADR_TITLE}.md` with:

1. **Decision number and title**
2. **Context** - Why this decision was needed
3. **Decision** - What was decided
4. **Rationale** - Why this choice was made
5. **Consequences** - Impact and trade-offs
6. **Status** - Current, superseded, or deprecated
7. **Date/commit reference**

### Categories of Decisions

#### 1. Technology Stack Decisions

- ADR-001: .NET 9.0 Framework Selection
- ADR-002: Blazor Server for Student App
- ADR-003: Semantic Kernel for AI Integration
- ADR-004: Ollama for Local LLM Execution
- ADR-005: PostgreSQL as Primary Database
- ADR-006: Redis for Caching Layer
- ADR-007: .NET Aspire for Local Orchestration
- [Extract more from codebase]

#### 2. Architecture Pattern Decisions

- ADR-010: Multi-Agent Architecture for Assessment Generation
- ADR-011: Repository Pattern for Data Access
- ADR-012: CQRS for Assessment Analytics
- ADR-013: API Versioning Strategy
- [Extract from code patterns]

#### 3. Azure Deployment Decisions

- ADR-020: Azure Container Apps for Hosting
- ADR-021: Azure Database for PostgreSQL Flexible Server
- ADR-022: Runtime FQDN Detection vs Template Variables
- ADR-023: Health Check Strategy (degraded vs unhealthy)
- ADR-024: MCR Image Sources over Docker Hub
- ADR-025: Client-Secret Authentication over OIDC
- [Extract from deployment docs]

#### 4. Security & Privacy Decisions

- ADR-030: Role-Based Access Control Model
- ADR-031: Student Data Privacy Controls
- ADR-032: API Authentication Strategy
- ADR-033: Connection String Security
- [Extract from security docs]

#### 5. Development Workflow Decisions

- ADR-040: Git Branch Strategy
- ADR-041: Semantic Commit Messages
- ADR-042: Dev Container for Development Environment
- ADR-043: Copilot Integration Guidelines
- [Extract from git patterns]

#### 6. Testing Strategy Decisions

- ADR-050: Unit Test Framework (xUnit)
- ADR-051: Integration Test Approach
- ADR-052: E2E Testing with Playwright
- ADR-053: Test Coverage Requirements
- [Extract from test projects]

#### 7. Frontend Decisions

- ADR-060: Blazor Interactive Server Components
- ADR-061: Bootstrap for UI Framework
- ADR-062: KaTeX for Math Rendering
- ADR-063: HTML Links vs Blazor Navigation (recent)
- [Extract from frontend code]

#### 8. Data Architecture Decisions

- ADR-070: Entity Framework Core for ORM
- ADR-071: Database Migration Strategy
- ADR-072: Caching Strategy
- ADR-073: Session Management Approach
- [Extract from data layer]

#### 9. Observability Decisions

- ADR-080: Application Insights Integration
- ADR-081: Structured Logging with Serilog
- ADR-082: Health Check Endpoints
- ADR-083: Distributed Tracing Strategy
- [Extract from observability code]

#### 10. AI/ML Decisions

- ADR-090: Question Generation Strategy
- ADR-091: Adaptive Difficulty Algorithm
- ADR-092: Feedback Generation Approach
- ADR-093: Context Window Management
- [Extract from agents code]

### Superseded Decisions

Document decisions that were made but later changed:

- What was the original decision
- Why it was changed
- What replaced it
- Reference to commits

### Deferred Decisions

Areas where decisions are pending:

- What needs to be decided
- Why it's deferred
- When it should be revisited

## Success Criteria

- All major architectural decisions are documented
- Each decision has clear rationale
- Consequences and trade-offs are explicit
- Superseded decisions are noted
- Commit/file references provide traceability
- Organized logically by category
- 30-50 distinct decisions documented
- Each decision is 1-2 paragraphs maximum

## Notes

- Focus on SIGNIFICANT decisions that shaped the system
- Avoid documenting trivial choices
- Include "why not X" explanations where relevant
- Note decisions made by constraint vs preference
- Highlight controversial or difficult decisions
- Reference specific commits where decisions were implemented
- Note decisions that may need revisiting

## Example ADR Format

**ADR-021: Azure Database for PostgreSQL Flexible Server**

*Context:* Initial deployment used containerized PostgreSQL in Azure Container Apps, but this presented challenges with data persistence, permissions (Azure Files), and production readiness concerns.

*Decision:* Migrate to Azure Database for PostgreSQL Flexible Server as the managed database solution, replacing the containerized PostgreSQL approach.

*Rationale:* Managed database provides automatic backups, high availability, security compliance, and eliminates container permission issues. The FQDN connection string requires runtime detection since azd template variable substitution proved unreliable.

*Consequences:* Improved reliability and production-readiness, but added complexity to connection string management. Requires runtime FQDN patching in application code. Increases Azure costs compared to containerized approach. Better aligns with enterprise requirements.

*Status:* Current (implemented in commit 252231e, refined in subsequent commits)

*References:* docs/deployment/PRAGMATIC_POSTGRES_SOLUTION.md, docs/deployment/TEMPLATE_SUBSTITUTION_ISSUE.md
