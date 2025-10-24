# System Architecture

**Document Version:** 1.0  
**Last Updated:** October 24, 2025  
**System Version:** 0.2.0

---

## Table of Contents

1. [High-Level Architecture](#high-level-architecture)
2. [Component Overview](#component-overview)
3. [Technology Stack](#technology-stack)
4. [Deployment Architecture](#deployment-architecture)
5. [Data Flow Patterns](#data-flow-patterns)
6. [Multi-Tenancy Model](#multi-tenancy-model)
7. [Integration Patterns](#integration-patterns)

---

## 1. High-Level Architecture

### System Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            USER INTERFACES (Blazor Apps)                         │
├───────────────┬───────────────┬───────────────┬───────────────┬─────────────────┤
│   Student     │   Teacher     │ School Admin  │ Course Admin  │ Business/Sys    │
│     App       │   ClassApp    │     App       │     App       │    Admin Apps   │
│               │               │               │               │                 │
│ • Assessments │ • Class View  │ • School View │ • Curriculum  │ • Onboarding    │
│ • Progress    │ • Grading     │ • Analytics   │ • Questions   │ • Billing       │
│ • Feedback    │ • Feedback    │ • Reports     │ • Standards   │ • System Ops    │
└───────┬───────┴───────┬───────┴───────┬───────┴───────┬───────┴─────────┬───────┘
        │               │               │               │                 │
        └───────────────┴───────────────┴───────────────┴─────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        WEB API LAYER (ASP.NET Core)                             │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │   Student    │  │   Teacher    │  │ School Admin │  │ Course Admin │       │
│  │ Controller   │  │ Controller   │  │  Controller  │  │  Controller  │       │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘       │
│                                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────────────┐       │
│  │  Business    │  │   System     │  │     SignalR Hubs               │       │
│  │   Admin      │  │   Admin      │  │  • Student Progress Hub        │       │
│  │ Controller   │  │ Controller   │  │  • Class Monitoring Hub        │       │
│  └──────────────┘  └──────────────┘  │  • Admin Dashboard Hub         │       │
│                                       └────────────────────────────────┘       │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐ │
│  │              AUTHENTICATION & AUTHORIZATION MIDDLEWARE                    │ │
│  │  • Azure AD B2C  • JWT Tokens  • Role-Based Policies                     │ │
│  │  • Tenant Context Extraction  • Claims Transformation                    │ │
│  └───────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          BUSINESS LOGIC LAYER                                    │
├─────────────────┬─────────────────┬─────────────────┬─────────────────────────┤
│  Orchestration  │     Agents      │   Analytics     │   Shared Services       │
├─────────────────┼─────────────────┼─────────────────┼─────────────────────────┤
│                 │                 │                 │                         │
│ • Progress      │ • Math Agent    │ • Performance   │ • LLM Service          │
│   Orchestrator  │ • Physics Agent │   Analytics     │   (Ollama/Azure)       │
│                 │ • Chemistry     │ • Statistical   │ • Adaptive Testing     │
│ • Assessment    │   Agent         │   Analysis      │   Engine (ML.NET)      │
│   Scheduler     │ • Biology Agent │ • Predictive    │ • Caching (Redis)      │
│                 │ • English Agent │   Models        │ • Notification         │
│ • Task Router   │                 │ • Reporting     │   Service              │
│                 │ • Shared Base   │   Engine        │                         │
└─────────────────┴─────────────────┴─────────────────┴─────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          DATA ACCESS LAYER                                       │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐ │
│  │                      TENANT CONTEXT MIDDLEWARE                            │ │
│  │  Automatic filtering by SchoolId, ClassId based on user claims           │ │
│  └───────────────────────────────────────────────────────────────────────────┘ │
│                                                                                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐            │
│  │     Student      │  │   Assessment     │  │    Question      │            │
│  │   Repository     │  │   Repository     │  │   Repository     │            │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘            │
│                                                                                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐            │
│  │     School       │  │     Class        │  │     Course       │            │
│  │   Repository     │  │   Repository     │  │   Repository     │            │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘            │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐ │
│  │                   ENTITY FRAMEWORK CORE CONTEXT                           │ │
│  │  • Row-Level Security (Query Filters)                                    │ │
│  │  • Multi-Tenant Data Isolation                                           │ │
│  │  • Optimistic Concurrency                                                │ │
│  └───────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              DATA STORAGE                                        │
├───────────────────┬────────────────────┬────────────────────┬──────────────────┤
│   PostgreSQL      │      Redis         │  Azure Blob        │   Ollama LLM     │
│   (Primary DB)    │    (Caching)       │   (Documents)      │   (Local AI)     │
│                   │                    │                    │                  │
│ • Students        │ • Session Data     │ • Assessment PDFs  │ • llama3.2:3b    │
│ • Assessments     │ • Question Cache   │ • Student Reports  │ • Question Gen   │
│ • Questions       │ • Real-time State  │ • Backups          │ • Evaluation     │
│ • Progress Data   │ • Rate Limiting    │                    │ • Feedback       │
└───────────────────┴────────────────────┴────────────────────┴──────────────────┘
```

*Reference: `docs/architecture/SYSTEM_DIAGRAM.md` for detailed version*

### Architecture Layers

#### Presentation Layer
- **Blazor Server Apps:** Interactive web UIs with SignalR
- **Client-side state management:** Component-level state
- **Real-time updates:** SignalR hubs for live data

#### API Layer
- **ASP.NET Core Web API:** RESTful endpoints
- **API versioning:** URL-based (v1.0)
- **Middleware pipeline:** Auth, CORS, tenant context, error handling
- **SignalR hubs:** Real-time bidirectional communication

#### Business Logic Layer
- **Domain services:** Orchestration, agents, analytics
- **Multi-agent system:** Subject-specific AI agents
- **Functional patterns:** Railway-oriented programming with `Result<T>`

#### Data Access Layer
- **Repository pattern:** Abstraction over data access
- **Entity Framework Core:** ORM with query filters
- **Tenant isolation:** Automatic filtering by SchoolId/ClassId

#### Data Storage Layer
- **PostgreSQL:** Primary relational database
- **Redis:** Session cache and real-time state
- **Azure Blob:** Document storage (planned)
- **Ollama:** Local LLM inference

---

## 2. Component Overview

### Solution Structure

```
EduMind.AI.sln (11 projects)
├── src/ (8 projects)
│   ├── AcademicAssessment.Core          # Domain models and interfaces
│   ├── AcademicAssessment.Infrastructure # Data access and EF Core
│   ├── AcademicAssessment.Agents        # AI agents (5 subjects)
│   ├── AcademicAssessment.Orchestration # Multi-agent coordination
│   ├── AcademicAssessment.Analytics     # Analytics and reporting
│   ├── AcademicAssessment.Web           # RESTful Web API
│   ├── AcademicAssessment.StudentApp    # Student Blazor UI
│   ├── AcademicAssessment.Dashboard     # Admin Blazor UI
│   ├── EduMind.AppHost                  # .NET Aspire orchestration
│   └── EduMind.ServiceDefaults          # Shared Aspire config
└── tests/ (3 projects)
    ├── AcademicAssessment.Tests.Unit         # Unit tests
    ├── AcademicAssessment.Tests.Integration  # Integration tests
    ├── AcademicAssessment.Tests.UI           # UI/E2E tests
    └── AcademicAssessment.Tests.Performance  # Performance tests
```

### Component Responsibilities

#### 1. **Core Library** (`AcademicAssessment.Core`)
- Domain entity definitions (records)
- Business rule interfaces
- Shared enumerations
- Common utilities (`Result<T>` error handling)
- **No dependencies** - foundation layer

**Key Types:**
- `Student`, `Assessment`, `Question`, `StudentAssessment`
- `IAssessmentRepository`, `IStudentRepository`
- `Subject`, `GradeLevel`, `DifficultyLevel`, `AssessmentType`

#### 2. **Infrastructure** (`AcademicAssessment.Infrastructure`)
- Entity Framework Core `DbContext`
- Repository implementations
- Database migrations
- External service clients
- Caching implementations

**Key Types:**
- `AcademicDbContext` - EF Core context
- `StudentRepository`, `AssessmentRepository`
- `PostgreSqlHealthCheck`, `RedisHealthCheck`

#### 3. **Agents** (`AcademicAssessment.Agents`)
- Subject-specific AI agents
- Semantic Kernel integration
- LLM prompt engineering
- A2A (Agent-to-Agent) protocol

**Key Agents:**
- `MathematicsAssessmentAgent`
- `PhysicsAssessmentAgent`
- `ChemistryAssessmentAgent`
- `BiologyAssessmentAgent`
- `EnglishAssessmentAgent`
- `A2ABaseAgent` - Shared functionality

#### 4. **Orchestration** (`AcademicAssessment.Orchestration`)
- Multi-agent workflow coordination
- Assessment scheduling
- Progress tracking
- Adaptive learning engine

**Key Types:**
- `StudentProgressOrchestrator`
- `AssessmentScheduler`
- `AdaptiveLearningEngine`

#### 5. **Analytics** (`AcademicAssessment.Analytics`)
- Performance metrics calculation
- Statistical analysis
- Predictive modeling
- Reporting generation

**Key Types:**
- `PerformanceAnalyticsAgent`
- `StatisticalAnalysisService`
- `LearningAnalyticsEngine`

#### 6. **Web API** (`AcademicAssessment.Web`)
- RESTful endpoints
- SignalR hubs for real-time
- Authentication/authorization
- API versioning
- Health checks

**Controllers:**
- `AssessmentController` - Assessment CRUD
- `OrchestrationController` - Agent coordination
- `StudentAnalyticsController` - Student metrics

**SignalR Hubs:**
- `StudentProgressHub`
- `ClassMonitoringHub`

#### 7. **Student App** (`AcademicAssessment.StudentApp`)
- Blazor Server interactive UI
- Assessment browsing and taking
- Progress visualization
- Real-time feedback

**Pages:**
- `Home.razor` - Landing page
- `AssessmentDashboard.razor` - Assessment list
- `AssessmentDetail.razor` - Assessment info
- `AssessmentSession.razor` - Taking assessment
- `AssessmentResults.razor` - Results view

#### 8. **Dashboard** (`AcademicAssessment.Dashboard`)
- Blazor Server admin interface
- Class management
- Analytics dashboards
- Student oversight

**Pages:**
- Admin views (to be expanded)
- Analytics dashboards
- Student management

#### 9. **AppHost** (`EduMind.AppHost`)
- .NET Aspire orchestration
- Service discovery
- Local development coordination
- Resource configuration

**Managed Services:**
- PostgreSQL connection
- Redis connection
- Ollama connection
- Web API
- Student App
- Dashboard

#### 10. **ServiceDefaults** (`EduMind.ServiceDefaults`)
- Shared Aspire configuration
- OpenTelemetry setup
- Health check defaults
- Logging configuration
- Resilience policies

---

## 3. Technology Stack

### Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 9.0 | Runtime and SDK |
| C# | 13 (latest) | Programming language |
| ASP.NET Core | 9.0 | Web framework |
| Entity Framework Core | 9.0 | ORM |
| Semantic Kernel | Latest | AI orchestration |
| ML.NET | Latest | Adaptive testing |
| Serilog | Latest | Structured logging |
| OpenTelemetry | Latest | Observability |

### Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| Blazor Server | .NET 9.0 | Interactive web UI |
| Bootstrap | 5.3 | CSS framework |
| SignalR | .NET 9.0 | Real-time communication |
| Font Awesome | Latest | Icons |

### Data Storage

| Technology | Version | Purpose |
|------------|---------|---------|
| PostgreSQL | 17 | Primary database |
| Redis | 7 | Cache and sessions |
| Azure Blob Storage | Latest | Document storage (planned) |

### AI/ML

| Technology | Version | Purpose |
|------------|---------|---------|
| Ollama | Latest | Local LLM hosting |
| llama3.2:3b | 3B params | Question generation, evaluation |
| Azure OpenAI | GPT-4o | Fallback for complex reasoning |
| ML.NET | Latest | IRT, adaptive algorithms |

### DevOps

| Technology | Version | Purpose |
|------------|---------|---------|
| Azure Container Apps | Latest | Hosting platform |
| Azure Container Registry | Latest | Container images |
| GitHub Actions | Latest | CI/CD |
| Azure Developer CLI (azd) | Latest | Deployment automation |
| Bicep | Latest | Infrastructure as Code |
| Docker | Latest | Containerization |

### Development Tools

| Tool | Purpose |
|------|---------|
| Visual Studio 2022 / VS Code | IDE |
| .NET Aspire | Local orchestration |
| Swagger/OpenAPI | API documentation |
| xUnit | Testing framework |
| FluentAssertions | Test assertions |
| Playwright | E2E testing (planned) |

---

## 4. Deployment Architecture

### Local Development

```
┌─────────────────────────────────────────────────────────┐
│              .NET Aspire AppHost                        │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │   Web API    │  │ Student App  │  │  Dashboard   │ │
│  │  :5103       │  │  :5049       │  │   :5091      │ │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘ │
│         │                 │                  │         │
│         └─────────────────┴──────────────────┘         │
│                           │                            │
│  ┌────────────────────────┴─────────────────────────┐ │
│  │         Service Discovery & Health Checks        │ │
│  └──────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                          │
              ┌───────────┴────────────┐
              │                        │
    ┌─────────▼─────────┐   ┌─────────▼────────┐
    │   PostgreSQL      │   │      Redis       │
    │   localhost:5432  │   │ localhost:6379   │
    └───────────────────┘   └──────────────────┘
                   │
            ┌──────▼──────┐
            │   Ollama    │
            │ :11434      │
            └─────────────┘
```

**Access Points:**
- Web API: http://localhost:5103
- Student App: http://localhost:5049
- Dashboard: http://localhost:5091
- Aspire Dashboard: http://localhost:15888

### Azure Production

```
┌─────────────────────────────────────────────────────────────────┐
│                    Azure Container Apps Environment             │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │
│  │   webapi     │  │  studentapp  │  │  dashboard   │        │
│  │  (external)  │  │  (external)  │  │  (external)  │        │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘        │
│         │                 │                  │                │
│         └─────────────────┴──────────────────┘                │
│                           │                                   │
│  ┌────────────────────────┴─────────────────────────┐        │
│  │         Internal Service Discovery               │        │
│  │  • edumind.internal.{env}.azurecontainerapps.io │        │
│  └──────────────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────────┘
                          │
              ┌───────────┴────────────┐
              │                        │
    ┌─────────▼─────────┐   ┌─────────▼────────┐
    │  Azure Database   │   │   Azure Cache    │
    │  for PostgreSQL   │   │   for Redis      │
    │  Flexible Server  │   │                  │
    └───────────────────┘   └──────────────────┘
                   │
            ┌──────▼──────┐
            │  Ollama     │
            │ Container   │
            │ (internal)  │
            └─────────────┘
```

**Azure Resources:**
- Resource Group: `rg-{environmentName}`
- Container Apps Environment: `cae-{environmentName}`
- PostgreSQL: `psql-{environmentName}-{suffix}`
- Redis: `redis-{environmentName}`
- Log Analytics: `log-{environmentName}`

### Deployment Models

#### Development
- Local Docker containers
- .NET Aspire orchestration
- Hot reload enabled
- Debug logging

#### Staging
- Azure Container Apps
- Scaled-down replicas (1-2)
- Test data
- Full monitoring

#### Production
- Azure Container Apps
- Auto-scaling (1-10 replicas)
- Production data
- High availability
- Backup and DR

---

## 5. Data Flow Patterns

### Student Assessment Flow

```
1. Student Authentication
   Browser → Azure AD B2C → JWT Token → Claims Extraction

2. Assessment Selection
   Student App → HTTP GET /api/v1/assessment
             → Web API Controller
             → Tenant Context Applied (SchoolId filter)
             → Repository Query (EF Core with filters)
             → PostgreSQL
             → JSON Response

3. Start Assessment
   Student App → HTTP GET /api/v1/assessment/{id}/session
             → Session Creation
             → Redis Cache (session state)
             → Questions Retrieved
             → Response with session data

4. Submit Answer
   Student App → HTTP POST /api/v1/assessment/session/{id}/answer
             → Validation
             → Agent Evaluation (Ollama LLM call)
             → Adaptive Algorithm (ML.NET)
             → Next Question Selection
             → PostgreSQL (save response)
             → Redis (update session)
             → SignalR (notify teacher)
             → Response with feedback

5. Complete Assessment
   Student App → HTTP POST /api/v1/assessment/session/{id}/complete
             → Calculate Score
             → Generate Report
             → Update Progress
             → PostgreSQL (finalize)
             → SignalR (notify teacher)
             → Redirect to Results
```

### Real-Time Progress Updates

```
Teacher Dashboard:
1. Connect to SignalR Hub
   Dashboard → SignalR Connection → ClassMonitoringHub
            → Subscribe to ClassId groups
            → Tenant validation

2. Student Submits Answer (concurrent):
   Student App → Answer Submission
              → Save to DB
              → SignalR Hub sends to ClassId group
              → All connected teachers receive update

3. Dashboard Updates:
   ClassMonitoringHub → Message → Dashboard
                     → Update UI (live progress bar)
                     → No page refresh needed
```

### Agent Coordination Flow

```
Multi-Agent Assessment:
1. Question Generation Request
   Orchestrator → Determine Subject
              → Select Agent (e.g., MathematicsAgent)
              → Agent Prompt Engineering
              → Ollama LLM API call
              → Parse Response
              → Validate Question
              → Return structured question

2. Answer Evaluation
   Orchestrator → Parse Student Answer
              → Select Evaluating Agent
              → Agent evaluates correctness
              → Generate feedback
              → Update difficulty estimate (IRT)
              → Return evaluation + feedback

3. Adaptive Selection
   Orchestrator → Get Student Ability (theta)
              → Query Question Bank
              → Calculate IRT probabilities
              → Select optimal next question
              → Return question
```

---

## 6. Multi-Tenancy Model

### Tenant Hierarchy

```
System (Root)
└── Schools (Tenant Boundary = SchoolId)
    ├── Classes (ClassId)
    │   └── Students (StudentId)
    └── Teachers (TeacherId with ClassIds)
```

### Data Isolation Strategy

#### Physical Isolation
- **One database per school** (for B2B deployments)
- Absolute data separation
- Independent backups
- Regulatory compliance (FERPA, GDPR)

#### Logical Isolation
- **Row-Level Security** via EF Core query filters
- Every entity tagged with `SchoolId` and/or `ClassId`
- Automatic filtering based on user claims
- Multi-tenant queries optimized with indexes

### Tenant Context Flow

```
1. User Authenticates
   Azure AD B2C → JWT Token
               → Claims: { userId, role, schoolId, classIds }

2. Request Arrives
   HTTP Request → Authentication Middleware
               → Extract Claims
               → Create ITenantContext
                  {
                    UserId: guid,
                    Role: "Teacher",
                    SchoolId: guid,
                    ClassIds: [guid, guid]
                  }

3. Database Query
   Repository → EF Core Query
            → Query Filter Applied Automatically
               WHERE SchoolId = @currentSchoolId
               AND (ClassId IN @currentClassIds OR ClassId IS NULL)
            → Execute Query
            → Return Filtered Results

4. Authorization Check
   Controller → [Authorize(Policy = "TeacherOnly")]
            → Validate tenant context
            → Ensure data belongs to user's school/class
            → Process request or return 403 Forbidden
```

### Access Control Matrix

| Role               | Own Data | Class Data | School Data | Cross-School | System |
|--------------------|----------|------------|-------------|--------------|--------|
| Student            | ✅ Full  | ❌         | ❌          | ❌           | ❌     |
| Teacher            | ✅ Full  | ✅ Full    | ❌          | ❌           | ❌     |
| School Admin       | ✅ Full  | ✅ Full    | ✅ Full     | ❌           | ❌     |
| Course Admin       | ✅ Full  | 📊 Anon.   | 📊 Anon.    | 📊 Anon.     | ❌     |
| Business Admin     | ✅ Full  | 🔒 Meta    | ✅ Full     | ✅ Full      | ❌     |
| System Admin       | ✅ Full  | ✅ Full    | ✅ Full     | ✅ Full      | ✅ Full|

**Legend:**
- ✅ Full Access
- ❌ No Access  
- 📊 Anonymized/Aggregated Only
- 🔒 Metadata Only

---

## 7. Integration Patterns

### External Service Integration

#### Ollama LLM
- **Connection:** HTTP REST API
- **Model:** llama3.2:3b
- **Timeout:** 120 seconds
- **Retry:** 3 attempts with exponential backoff
- **Fallback:** Azure OpenAI (if configured)

#### Azure Services
- **Application Insights:** Telemetry and monitoring
- **Key Vault:** Secrets management (planned)
- **Blob Storage:** Document storage (planned)
- **Container Registry:** Docker images

### Inter-Service Communication

#### HTTP REST
- Service-to-service calls via HTTP
- JSON payloads
- Versioned APIs
- Resilience policies (Polly)

#### SignalR
- Real-time bidirectional communication
- WebSocket with fallbacks
- Hub-based messaging
- Group subscriptions for tenants

### Caching Strategy

#### Redis Usage
- **Session state:** 30-minute sliding expiration
- **Question cache:** 1-hour absolute expiration
- **Rate limiting:** Token bucket algorithm
- **Real-time state:** Assessment session data

#### Cache Invalidation
- Time-based expiration
- Event-driven invalidation on updates
- Cache-aside pattern

---

## Architecture Principles

### Design Principles

1. **Separation of Concerns:** Clear layer boundaries
2. **Dependency Inversion:** Depend on abstractions, not concretions
3. **Single Responsibility:** Each component has one reason to change
4. **Open/Closed:** Open for extension, closed for modification
5. **Liskov Substitution:** Implementations are substitutable
6. **Interface Segregation:** Client-specific interfaces
7. **Don't Repeat Yourself:** Shared logic in libraries

### Functional Programming

- **Immutable domain models:** C# records
- **Railway-oriented programming:** `Result<T>` for error handling
- **Pure functions:** Where possible
- **Composition:** Function composition for workflows

### Cloud-Native Patterns

- **12-Factor App:** Configuration, stateless processes, logs as streams
- **Containerization:** Docker containers for all services
- **Orchestration:** .NET Aspire / Azure Container Apps
- **Health checks:** Liveness and readiness probes
- **Observability:** Metrics, logs, traces (OpenTelemetry)

---

## Related Documentation

- **03-domain-model.md** - Complete entity and relationship documentation
- **04-application-components.md** - Detailed component specifications
- **05-data-storage.md** - Database schema and caching details
- **07-security-privacy.md** - Security architecture and compliance

---

**For System Diagram:** See `docs/architecture/SYSTEM_DIAGRAM.md`  
**For Solution Structure:** See `docs/architecture/SOLUTION_STRUCTURE.md`  
**For Development Standards:** See `.github/instructions.md`
