# Application Components

**Document Version:** 1.0  
**Last Updated:** October 24, 2025  
**System Version:** 0.2.0

---

## Table of Contents

1. [Overview](#overview)
2. [Component Catalog](#component-catalog)
3. [Component Dependencies](#component-dependencies)
4. [Inter-Component Communication](#inter-component-communication)
5. [Technology Stack by Component](#technology-stack-by-component)

---

## 1. Overview

EduMind.AI is organized into **10 application components** following Clean Architecture and Domain-Driven Design principles:

### Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │   Web API    │  │   Dashboard  │  │  Student App │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ Orchestration│  │   Agents     │  │  Analytics   │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                       Domain Layer                           │
│                     ┌──────────────┐                        │
│                     │     Core     │                        │
│                     └──────────────┘                        │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                      │
│                  ┌──────────────────┐                       │
│                  │  Infrastructure  │                       │
│                  └──────────────────┘                       │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     Orchestration Layer                      │
│         ┌──────────────┐    ┌──────────────┐               │
│         │   AppHost    │    │ ServiceDefaults │             │
│         └──────────────┘    └──────────────┘               │
└─────────────────────────────────────────────────────────────┘
```

### Design Principles

- **Dependency Inversion:** All components depend on Core abstractions
- **Separation of Concerns:** Clear boundaries between layers
- **Single Responsibility:** Each component has focused purpose
- **Open/Closed:** Extensible via interfaces, closed for modification

---

## 2. Component Catalog

### 2.1 AcademicAssessment.Core

**Type:** Class Library (Foundation)  
**Layer:** Domain  
**Location:** `src/AcademicAssessment.Core/`

**Purpose:** Contains all domain models, interfaces, and enumerations. This is the foundation layer with zero external dependencies.

**Key Responsibilities:**
- Define domain entities (Student, Assessment, Question, etc.)
- Define service interfaces (IStudentRepository, IAssessmentService, etc.)
- Define enumerations (Subject, GradeLevel, AssessmentType, etc.)
- Provide domain business logic (immutable records with `With()` methods)
- Define `Result<T>` pattern for error handling

**Internal Structure:**
```
Core/
├── Models/              # Domain entities
│   ├── Student.cs
│   ├── Assessment.cs
│   ├── Question.cs
│   ├── StudentAssessment.cs
│   ├── StudentResponse.cs
│   ├── School.cs
│   ├── Class.cs
│   ├── Course.cs
│   └── User.cs
├── Enums/              # Enumerations
│   ├── Subject.cs
│   ├── GradeLevel.cs
│   ├── AssessmentType.cs
│   ├── DifficultyLevel.cs
│   ├── QuestionType.cs
│   ├── AssessmentStatus.cs
│   ├── UserRole.cs
│   ├── SubscriptionTier.cs
│   └── MasteryLevel.cs
├── Interfaces/         # Service contracts
│   ├── IStudentRepository.cs
│   ├── IAssessmentRepository.cs
│   ├── IQuestionRepository.cs
│   ├── IStudentAssessmentRepository.cs
│   ├── IStudentResponseRepository.cs
│   └── ...
└── Common/            # Shared types
    └── Result.cs      # Functional error handling
```

**Dependencies:** None (foundation layer)

**Key Technologies:**
- .NET 9.0 Records (immutability)
- C# 13 features (`required` keyword)
- Functional patterns (`Result<T>`)

---

### 2.2 AcademicAssessment.Infrastructure

**Type:** Class Library  
**Layer:** Infrastructure  
**Location:** `src/AcademicAssessment.Infrastructure/`

**Purpose:** Implements all external concerns including database access, external service integrations, and repository implementations.

**Key Responsibilities:**
- Entity Framework Core database context
- Repository implementations (Student, Assessment, Question, etc.)
- External service clients (LLM providers)
- Database migrations and seeding
- Multi-tenant query filters
- Caching strategies

**Internal Structure:**
```
Infrastructure/
├── Context/
│   ├── AssessmentDbContext.cs          # Main EF Core context
│   └── DbContextFactory.cs             # Factory for migrations
├── Repositories/
│   ├── StudentRepository.cs
│   ├── AssessmentRepository.cs
│   ├── QuestionRepository.cs
│   ├── StudentAssessmentRepository.cs
│   └── ...
├── ExternalServices/
│   ├── LLM/
│   │   ├── OllamaLlmClient.cs         # Local LLM
│   │   ├── AzureOpenAILlmClient.cs    # Azure OpenAI
│   │   └── StubLlmClient.cs           # Testing stub
│   └── ...
├── Services/
│   ├── TenantService.cs               # Multi-tenancy
│   └── CachingService.cs
├── Middleware/
│   └── TenantResolutionMiddleware.cs
└── Data/
    └── Migrations/                     # EF Core migrations
```

**Dependencies:** 
- Core (interfaces and models)
- Entity Framework Core 9.0
- Npgsql (PostgreSQL driver)
- Redis client libraries

**Key Technologies:**
- Entity Framework Core 9.0
- PostgreSQL 16+
- Redis 7+
- Dependency Injection

---

### 2.3 AcademicAssessment.Agents

**Type:** Class Library  
**Layer:** Application  
**Location:** `src/AcademicAssessment.Agents/`

**Purpose:** Subject-specific assessment agents implementing the Agent-to-Agent (A2A) protocol for distributed assessment generation and evaluation.

**Key Responsibilities:**
- Generate subject-specific assessment questions
- Evaluate student responses using LLMs
- Provide detailed feedback and explanations
- Implement A2A protocol for agent communication
- Support both Ollama (local) and Azure OpenAI

**Internal Structure:**
```
Agents/
├── Shared/
│   ├── A2ABaseAgent.cs                # Base agent with A2A protocol
│   ├── AgentCard.cs                   # Agent capability declaration
│   ├── AgentTask.cs                   # Task model
│   ├── ITaskService.cs                # Task routing interface
│   └── Models/                        # Shared models
├── Mathematics/
│   └── MathematicsAssessmentAgent.cs
├── Physics/
│   └── PhysicsAssessmentAgent.cs
├── Chemistry/
│   └── ChemistryAssessmentAgent.cs
├── Biology/
│   └── BiologyAssessmentAgent.cs
└── English/
    └── EnglishAssessmentAgent.cs
```

**Agent Capabilities:**
- **generate_assessment:** Create questions based on student level
- **evaluate_response:** Grade student answers with LLM
- **provide_feedback:** Generate detailed explanations
- **suggest_topics:** Recommend study areas

**Dependencies:**
- Core (models and interfaces)
- Microsoft.AspNetCore.SignalR.Client (for progress broadcasting)

**Key Technologies:**
- Agent-to-Agent (A2A) protocol
- SignalR client for real-time updates
- LLM integration (abstracted via Core interfaces)

---

### 2.4 AcademicAssessment.Orchestration

**Type:** Class Library  
**Layer:** Application  
**Location:** `src/AcademicAssessment.Orchestration/`

**Purpose:** Central coordinator that manages student assessment workflows and routes tasks to appropriate subject agents.

**Key Responsibilities:**
- Coordinate multi-agent workflows
- Track student progress across assessments
- Implement intelligent agent routing
- Maintain assessment state machines
- Broadcast progress updates via SignalR
- Schedule assessments and recommend study paths

**Internal Structure:**
```
Orchestration/
├── StudentProgressOrchestrator.cs     # Main orchestrator
├── Models/
│   ├── OrchestrationState.cs
│   ├── AgentMetrics.cs
│   └── RoutingDecision.cs
├── Entities/
│   └── OrchestrationSession.cs        # Session state
└── Interfaces/
    └── IOrchestrationMetricsService.cs
```

**Orchestration Logic:**
1. Receive student assessment request
2. Determine which subject needs assessment
3. Route task to best available agent
4. Monitor agent execution
5. Aggregate results
6. Update student progress
7. Broadcast updates to UI

**Dependencies:**
- Core (models and interfaces)
- Agents (A2A protocol and agent cards)
- Microsoft.AspNetCore.SignalR (for broadcasting)

**Key Technologies:**
- Agent routing algorithm (skill + subject + grade + load)
- SignalR for real-time progress
- State machine for assessment workflows

---

### 2.5 AcademicAssessment.Analytics

**Type:** Class Library  
**Layer:** Application  
**Location:** `src/AcademicAssessment.Analytics/`

**Purpose:** Performance analytics, statistical analysis, and predictive modeling for student outcomes.

**Key Responsibilities:**
- Aggregate student performance data
- Calculate class-level statistics
- Identify struggling students (early intervention)
- Generate reports for teachers and administrators
- Apply k-anonymity for privacy-preserving analytics
- Track mastery levels by topic

**Internal Structure:**
```
Analytics/
└── Services/
    ├── PerformanceAnalyticsService.cs
    ├── StatisticalAnalysisService.cs
    ├── PredictiveModelService.cs
    └── PrivacyPreservingAnalytics.cs
```

**Key Metrics:**
- Average score by class, subject, topic
- Question difficulty calibration (IRT)
- Student mastery levels
- Time-to-completion trends
- Intervention alerts

**Dependencies:**
- Core (models and interfaces)

**Key Technologies:**
- Statistical analysis algorithms
- K-anonymity (minimum 5 students for aggregation)
- IRT (Item Response Theory) calculations

---

### 2.6 AcademicAssessment.Web

**Type:** ASP.NET Core Web API  
**Layer:** Presentation  
**Location:** `src/AcademicAssessment.Web/`

**Purpose:** Primary backend REST API serving all clients with versioned endpoints, SignalR hubs, and health checks.

**Key Responsibilities:**
- Expose REST API endpoints
- Host SignalR hubs for real-time communication
- Implement authentication and authorization
- Validate requests and responses
- Handle multi-tenancy via middleware
- Provide OpenAPI/Swagger documentation
- Implement health checks

**Internal Structure:**
```
Web/
├── Controllers/
│   ├── AssessmentController.cs        # Assessment management
│   ├── OrchestrationController.cs     # Start assessments
│   ├── StudentAnalyticsController.cs  # Analytics endpoints
│   └── ...
├── Hubs/
│   ├── AgentProgressHub.cs            # Agent progress updates
│   └── OrchestrationHub.cs            # Orchestration events
├── Services/
│   ├── OrchestrationMetricsService.cs
│   ├── TenantContextDevelopment.cs
│   └── Stub*Repository.cs             # Dev/test stubs
├── Program.cs                          # App configuration
├── appsettings.json
└── appsettings.Development.json
```

**API Versioning:**
- `/api/v1/assessments`
- `/api/v1/orchestration`
- `/api/v1/analytics`

**SignalR Hubs:**
- `/hubs/agent-progress` - Agent progress updates
- `/hubs/orchestration` - Orchestration events

**Dependencies:**
- Core (models and interfaces)
- Infrastructure (repositories)
- Orchestration (orchestrator service)
- Analytics (analytics services)
- Agents (agent registration)
- EduMind.ServiceDefaults (Aspire integration)

**Key Technologies:**
- ASP.NET Core 9.0
- SignalR for WebSocket communication
- JWT authentication via Azure AD B2C
- Swagger/OpenAPI
- API versioning (Asp.Versioning)
- Health checks (PostgreSQL, Redis)
- Serilog structured logging

---

### 2.7 AcademicAssessment.Dashboard

**Type:** Blazor Web App (Server)  
**Layer:** Presentation  
**Location:** `src/AcademicAssessment.Dashboard/`

**Purpose:** Teacher and administrator interface for managing students, viewing analytics, and reviewing assessments.

**Key Responsibilities:**
- Teacher dashboard with class overview
- Student progress visualization
- Assessment review and grading
- Class-level analytics and reports
- School administration features
- Real-time updates via SignalR

**Internal Structure:**
```
Dashboard/
├── Pages/
│   ├── Index.razor                    # Dashboard home
│   ├── Classes/
│   ├── Students/
│   ├── Assessments/
│   └── Analytics/
├── Components/
│   ├── ClassPerformanceChart.razor
│   ├── StudentProgressCard.razor
│   └── AssessmentReviewTable.razor
├── Services/
│   └── DashboardApiClient.cs          # Web API client
└── Program.cs
```

**User Personas Supported:**
- Teachers (view assigned classes)
- School Administrators (school-wide analytics)
- Course Administrators (curriculum management)
- Business/System Administrators (multi-school overview)

**Dependencies:**
- Core (models for display)
- Analytics (analytics client)
- EduMind.ServiceDefaults (Aspire integration)

**Key Technologies:**
- Blazor Server (interactive UI)
- SignalR (automatic real-time updates)
- Chart.js via Blazor wrappers
- Bootstrap 5 UI framework

---

### 2.8 AcademicAssessment.StudentApp

**Type:** Blazor Web App (Server)  
**Layer:** Presentation  
**Location:** `src/AcademicAssessment.StudentApp/`

**Purpose:** Student-facing application for taking assessments, viewing progress, and receiving personalized recommendations.

**Key Responsibilities:**
- Assessment-taking interface
- Real-time question display
- Submit answers and receive immediate feedback
- Progress tracking and streaks (gamification)
- Study recommendations
- View past assessments and scores

**Internal Structure:**
```
StudentApp/
├── Pages/
│   ├── Index.razor                    # Student home
│   ├── AssessmentList.razor
│   ├── TakeAssessment.razor           # Main assessment UI
│   ├── Progress.razor
│   └── Recommendations.razor
├── Components/
│   ├── QuestionDisplay.razor          # Question rendering
│   ├── AnswerInput.razor              # Answer input
│   ├── ProgressBar.razor
│   └── StreakIndicator.razor
├── Services/
│   └── StudentApiClient.cs            # Web API client
└── Program.cs
```

**User Flow:**
1. Student logs in (Azure AD B2C or self-service OAuth)
2. View available assessments
3. Start assessment (connects to orchestrator)
4. Answer questions (real-time via SignalR)
5. Receive instant feedback
6. View final score and recommendations

**Dependencies:**
- Core (models for display)
- EduMind.ServiceDefaults (Aspire integration)
- Markdig (Markdown rendering for questions)

**Key Technologies:**
- Blazor Server (interactive UI)
- SignalR (real-time question delivery)
- Markdown rendering for rich content
- Bootstrap 5 UI framework

---

### 2.9 EduMind.AppHost

**Type:** .NET Aspire App Host  
**Layer:** Orchestration  
**Location:** `src/EduMind.AppHost/`

**Purpose:** Orchestrates all services for local development and deployment using .NET Aspire.

**Key Responsibilities:**
- Service discovery and registration
- Dependency injection configuration
- Environment variable management
- Health check aggregation
- Distributed tracing configuration
- Local development orchestration

**Configuration:**
```csharp
// PostgreSQL reference
var postgres = builder.AddConnectionString("postgres", 
    "Host=localhost;Port=5432;Database=edumind_dev;...");

// Redis reference
var redis = builder.AddConnectionString("cache", "localhost:6379");

// OLLAMA reference
var ollama = builder.AddConnectionString("ollama", "http://localhost:11434");

// Web API
var webApi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
    .WithReference(postgres)
    .WithReference(redis);

// Dashboard
builder.AddProject<Projects.AcademicAssessment_Dashboard>("dashboard")
    .WithReference(webApi);

// Student App
builder.AddProject<Projects.AcademicAssessment_StudentApp>("studentapp")
    .WithReference(webApi);
```

**Dependencies:**
- All source projects
- Aspire.Hosting package

**Key Technologies:**
- .NET Aspire 9.5.1
- Service discovery
- Distributed tracing (OpenTelemetry)

---

### 2.10 EduMind.ServiceDefaults

**Type:** Class Library  
**Layer:** Orchestration  
**Location:** `src/EduMind.ServiceDefaults/`

**Purpose:** Shared configuration and cross-cutting concerns for all Aspire-integrated services.

**Key Responsibilities:**
- OpenTelemetry configuration (tracing, metrics, logging)
- Health check defaults
- Service discovery client configuration
- Resilience patterns (retry, circuit breaker)
- Common middleware registration

**Shared Configurations:**
- Distributed tracing with Jaeger/Zipkin
- Prometheus metrics export
- Health check endpoints
- Serilog structured logging
- HTTP client resilience policies

**Dependencies:**
- Microsoft.Extensions.* packages
- OpenTelemetry packages

**Key Technologies:**
- .NET Aspire ServiceDefaults pattern
- OpenTelemetry (OTEL)
- Polly (resilience)

---

## 3. Component Dependencies

### Dependency Graph

```
┌─────────────────────────────────────────────────────────────┐
│                      Core (Foundation)                       │
└─────────────────────────────────────────────────────────────┘
         ↑         ↑         ↑         ↑         ↑
         │         │         │         │         │
    ┌────┴────┐   │    ┌────┴────┐    │    ┌────┴────┐
    │Infra    │   │    │ Agents  │    │    │Analytics│
    │structure│   │    └─────────┘    │    └─────────┘
    └─────────┘   │         ↑         │         ↑
                  │         │         │         │
                  │    ┌────┴─────────┴────┐    │
                  │    │   Orchestration   │    │
                  │    └───────────────────┘    │
                  │         ↑                   │
         ┌────────┴─────────┼───────────────────┴────────┐
         │                  │                            │
    ┌────┴────┐      ┌──────┴──────┐          ┌─────────┴────┐
    │Dashboard│      │   Web API   │          │  Student App │
    └─────────┘      └─────────────┘          └──────────────┘
         ↑                  ↑                         ↑
         │                  │                         │
         └──────────────────┴─────────────────────────┘
                            ↑
                ┌───────────┴───────────┐
                │   ServiceDefaults     │
                └───────────────────────┘
                            ↑
                ┌───────────┴───────────┐
                │      AppHost          │
                └───────────────────────┘
```

### Dependency Table

| Component | Dependencies |
|-----------|--------------|
| **Core** | None (foundation) |
| **Infrastructure** | Core |
| **Agents** | Core, SignalR.Client |
| **Orchestration** | Core, Agents, SignalR |
| **Analytics** | Core |
| **Web API** | Core, Infrastructure, Orchestration, Analytics, Agents, ServiceDefaults |
| **Dashboard** | Core, Analytics, ServiceDefaults |
| **StudentApp** | Core, ServiceDefaults, Markdig |
| **ServiceDefaults** | Microsoft.Extensions.*, OpenTelemetry |
| **AppHost** | Aspire.Hosting, all projects |

### Circular Dependency Prevention

- **Core** has zero dependencies (foundation)
- All components depend on Core abstractions
- Infrastructure never references application layer
- Agents and Orchestration communicate via interfaces

---

## 4. Inter-Component Communication

### Communication Patterns

#### 4.1 HTTP REST API
**Used By:** Dashboard → Web API, StudentApp → Web API

```
Dashboard/StudentApp  →  [HTTP/HTTPS]  →  Web API
                                           ↓
                                      Orchestration
                                           ↓
                                        Agents
```

**Example Flow:**
1. Student clicks "Start Assessment" in StudentApp
2. StudentApp calls `POST /api/v1/orchestration/start`
3. Web API invokes Orchestrator
4. Orchestrator routes to subject agent
5. Agent generates questions
6. Results returned via HTTP response

#### 4.2 SignalR WebSockets
**Used By:** Web API → Dashboard, Web API → StudentApp

```
Orchestrator  →  [SignalR Hub]  →  Connected Clients
                                    (Dashboard/StudentApp)
```

**Example Flow:**
1. Orchestrator broadcasts progress: "Generating question 3 of 10..."
2. SignalR hub receives message
3. Hub pushes to all connected clients (filtered by StudentId)
4. StudentApp UI updates in real-time

**Hubs:**
- `AgentProgressHub` - Agent execution updates
- `OrchestrationHub` - Orchestration lifecycle events

#### 4.3 Agent-to-Agent (A2A) Protocol
**Used By:** Orchestrator ↔ Subject Agents

```
Orchestrator  →  [Task Queue/Direct Call]  →  Agent
              ←  [AgentCard Registration]  ←
```

**A2A Protocol:**
1. **Registration:** Agents publish `AgentCard` with capabilities
2. **Task Creation:** Orchestrator creates `AgentTask` with requirements
3. **Routing:** Orchestrator selects agent via skill + subject + grade + load
4. **Execution:** Agent processes task, updates status
5. **Completion:** Agent returns result, broadcasts progress

#### 4.4 Repository Pattern
**Used By:** All application components → Infrastructure

```
Orchestration/Agents  →  [IRepository]  →  Infrastructure
                                              ↓
                                          EF Core
                                              ↓
                                          PostgreSQL
```

**Example:**
```csharp
var studentResult = await _studentRepository.GetByIdAsync(studentId);
```

---

## 5. Technology Stack by Component

### Summary Table

| Component | Primary Technologies | Key Libraries |
|-----------|----------------------|---------------|
| **Core** | C# 13, .NET 9 Records | None (pure domain) |
| **Infrastructure** | EF Core 9, PostgreSQL | Npgsql 8.0, StackExchange.Redis |
| **Agents** | C# 13, SignalR Client | Microsoft.AspNetCore.SignalR.Client |
| **Orchestration** | C# 13, SignalR | Microsoft.AspNetCore.SignalR |
| **Analytics** | C# 13 | LINQ, statistical libraries |
| **Web API** | ASP.NET Core 9 | Swashbuckle, Serilog, HealthChecks |
| **Dashboard** | Blazor Server 9 | SignalR (built-in), Chart.js |
| **StudentApp** | Blazor Server 9 | Markdig, SignalR |
| **ServiceDefaults** | Aspire 9.5.1 | OpenTelemetry, Polly |
| **AppHost** | Aspire 9.5.1 | Aspire.Hosting |

### Shared Technologies (All Components)

- **.NET 9.0** - Runtime and SDK
- **C# 13** - Language features (records, required, pattern matching)
- **Dependency Injection** - Built-in Microsoft.Extensions.DependencyInjection
- **Logging** - Serilog with structured logging
- **Configuration** - IConfiguration with appsettings.json
- **OpenTelemetry** - Distributed tracing via ServiceDefaults

---

## Related Documentation

- **03-domain-model.md** - Entity definitions used across components
- **05-data-storage.md** - Database schema and Infrastructure implementation
- **06-external-integrations.md** - LLM providers and external services
- **07-security-privacy.md** - Authentication and multi-tenancy in Web API
- **10-api-reference.md** - Complete Web API endpoint documentation

---

**Build Command:** `dotnet build EduMind.AI.sln`  
**Run Command:** `dotnet run --project src/EduMind.AppHost`  
**Test Command:** `dotnet test`
