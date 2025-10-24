# EduMind.AI System Specification

**Version:** 1.0  
**Last Updated:** January 20, 2025  
**Status:** Complete ✅

## Overview

This directory contains the complete technical specification for EduMind.AI, an AI-powered academic assessment platform. The specification is organized into 21 comprehensive documents covering architecture, features, APIs, workflows, performance, and deployment.

**Total Documentation:** 19,372 lines across 21 documents

---

## Quick Start

**New to the project?** Read these documents first:

1. **[Executive Summary](./01-executive-summary.md)** - Vision, goals, and high-level overview
2. **[System Architecture](./02-system-architecture.md)** - Technical architecture and component diagram
3. **[Deployment Models](./13-deployment-models.md)** - Getting started with local development

**Building a feature?** Reference these:

4. **[Domain Model](./03-domain-model.md)** - Core entities and relationships
5. **[Application Components](./04-application-components.md)** - Services and business logic
6. **[API References](./10a-assessment-api.md)** - REST API documentation

---

## Document Index

### Part 1: System Architecture (Documents 01-08)

Core architectural documentation covering system design, domain model, and infrastructure.

#### [01. Executive Summary](./01-executive-summary.md) (520 lines)

**Purpose:** High-level project overview  
**Audience:** Executive stakeholders, project managers  
**Contents:**

- Vision and mission statement
- Key features and capabilities
- Technical stack overview (.NET 9, PostgreSQL, Redis, OLLAMA)
- Success metrics and KPIs
- Implementation roadmap

**Key Takeaway:** Multi-agent AI platform for personalized academic assessment supporting 1000+ concurrent students with <2s response times.

---

#### [02. System Architecture](./02-system-architecture.md) (1,023 lines)

**Purpose:** Complete architectural design  
**Audience:** System architects, senior developers  
**Contents:**

- Clean Architecture layers (Domain → Application → Infrastructure → Presentation)
- Component diagram with 8 major components
- Technology decisions and rationale
- Deployment architecture (Azure Container Apps)
- Data flow and communication patterns
- Service dependencies

**Key Takeaway:** Clean Architecture with DDD patterns, horizontally scalable microservices deployed on Azure Container Apps.

---

#### [03. Domain Model](./03-domain-model.md) (934 lines)

**Purpose:** Core business entities and relationships  
**Audience:** Developers, database designers  
**Contents:**

- 12 core domain entities with detailed properties
- Entity relationships and cardinality (1:1, 1:N, M:N)
- Aggregate roots and value objects (DDD patterns)
- Entity lifecycle and state transitions
- Business rules and invariants
- C# entity definitions with attributes

**Key Entities:** Student, Teacher, School, Assessment, Question, Response, Session, Class, LearningObjective, Agent, OrchestrationTask, AnalyticsSnapshot

**Key Takeaway:** Rich domain model enforcing business rules at the entity level using DDD patterns.

---

#### [04. Application Components](./04-application-components.md) (1,158 lines)

**Purpose:** Application layer services and logic  
**Audience:** Developers implementing business logic  
**Contents:**

- 7 major application services with interfaces
- Repository pattern implementations (17 repositories)
- Service responsibilities and dependencies
- Domain event handlers
- Background job processors
- Integration patterns with external services
- C# code examples and interfaces

**Key Services:** AssessmentService, StudentAnalyticsService, OrchestrationService, AgentService, GradingService, NotificationService, ReportingService

**Key Takeaway:** Application services orchestrate domain logic and coordinate across repositories and external services.

---

#### [05. Data Storage](./05-data-storage.md) (1,248 lines)

**Purpose:** Database schema and caching strategy  
**Audience:** Database administrators, backend developers  
**Contents:**

- PostgreSQL schema (17 tables)
- Complete table definitions with columns, types, constraints
- Indexing strategy for performance
- Entity Framework Core configuration
- Redis caching strategy (4-hour session TTL, 15-min analytics TTL)
- Data access patterns and query optimization
- Migration strategy

**Key Tables:** users, students, teachers, schools, assessments, questions, assessment_sessions, assessment_responses, classes, learning_objectives, agents

**Key Takeaway:** Normalized PostgreSQL schema with strategic Redis caching for performance (target: 80%+ hit rate).

---

#### [06. External Integrations](./06-external-integrations.md) (740 lines)

**Purpose:** Third-party service integrations  
**Audience:** Integration developers, DevOps  
**Contents:**

- OLLAMA LLM integration (llama3.2:3b, 20-25s per evaluation)
- Azure AD B2C authentication (JWT bearer tokens)
- Azure OpenAI (optional, <2s per evaluation)
- Email service (SendGrid/Azure Communication Services)
- SMS notifications (Twilio/Azure Communication Services)
- Future integrations (video proctoring, plagiarism detection)
- API client implementations
- Error handling and fallback strategies

**Key Integrations:** OLLAMA (local LLM), Azure AD B2C (auth), Azure OpenAI (production LLM)

**Key Takeaway:** OLLAMA for development (CPU-only, 20-25s), Azure OpenAI for production (<2s), with StubLLMService fallback.

---

#### [07. Security and Privacy](./07-security-privacy.md) (1,089 lines)

**Purpose:** Security architecture and compliance  
**Audience:** Security engineers, compliance officers  
**Contents:**

- Authentication (Azure AD B2C, JWT tokens)
- Authorization (RBAC with 6 roles: Student, Teacher, SchoolAdmin, CourseAdmin, BusinessAdmin, SystemAdmin)
- Data protection (encryption at rest and in transit, TLS 1.3)
- Privacy features (k-anonymity with k≥5, data anonymization)
- GDPR compliance (right to access, erasure, rectification)
- Security best practices (rate limiting, input validation, CORS)
- Threat model and mitigations

**Security Measures:** TLS 1.3, AES-256 encryption, Azure Key Vault, k-anonymity, RBAC, rate limiting

**Key Takeaway:** Enterprise-grade security with GDPR compliance, k-anonymity protection, and comprehensive RBAC.

---

#### [08. Observability and Monitoring](./08-observability.md) (783 lines)

**Purpose:** Logging, tracing, and monitoring strategy  
**Audience:** DevOps engineers, SREs  
**Contents:**

- OpenTelemetry integration (metrics, tracing, logging)
- Serilog structured logging
- Distributed tracing across services
- Health checks (self, PostgreSQL, Redis)
- Performance metrics (request duration, error rate, cache hit rate)
- Application Insights integration
- Grafana dashboards
- Alerting rules and runbooks

**Observability Stack:** OpenTelemetry + Serilog + Application Insights + Grafana

**Key Takeaway:** Complete observability with distributed tracing, structured logging, and real-time dashboards.

---

### Part 2: Features and Capabilities (Documents 09a-09e)

Detailed feature documentation organized by domain area.

#### [09a. Core Assessment Features](./09a-core-assessment-features.md) (1,100 lines)

**Purpose:** Assessment engine capabilities  
**Audience:** Product managers, developers  
**Contents:**

- 9 question types: Multiple Choice, True/False, Fill-in-Blank, Short Answer, Essay, Coding, Matching, Ordering, Diagram Labeling
- IRT (Item Response Theory) adaptive assessment algorithm
- Difficulty parameters (-3 to +3), discrimination (0.5-2.5), guessing probability
- Rich content rendering (LaTeX, code syntax highlighting, images)
- Auto-grading engines per question type
- LLM-assisted essay grading with rubrics
- Coding question execution sandbox
- Partial credit and negative marking
- Implementation status per feature

**Question Types:** 9 types with detailed specifications and grading algorithms

**Key Takeaway:** Sophisticated assessment engine with IRT-based adaptivity and 9 question types including LLM-graded essays.

---

#### [09b. Agent Orchestration Features](./09b-agent-orchestration-features.md) (1,000 lines)

**Purpose:** Multi-agent system architecture  
**Audience:** AI/ML engineers, system architects  
**Contents:**

- StudentProgressOrchestrator (central coordinator)
- 5 subject specialist agents (Mathematics, Physics, Chemistry, Biology, English)
- Agent-to-Agent (A2A) protocol for communication
- Task routing algorithm (capability matching, load balancing)
- Workflow execution engine
- Agent lifecycle management (discovery, registration, health monitoring)
- Performance tracking and statistics
- Implementation details with code examples

**Agents:** 1 orchestrator + 5 subject specialists (Mathematics, Physics, Chemistry, Biology, English)

**Key Takeaway:** Intelligent multi-agent system with dynamic task routing and A2A protocol for scalable workflow execution.

---

#### [09c. User Interface Features](./09c-user-interface-features.md) (950 lines)

**Purpose:** Frontend application features  
**Audience:** Frontend developers, UX designers  
**Contents:**

- Blazor Server architecture (3 apps: Student, Dashboard, Admin)
- Session management (Redis-backed, 4-hour TTL)
- Real-time features (SignalR for progress updates)
- Assessment taking UI (question palette, timer, auto-save every 30s)
- Analytics dashboards (performance charts, progress tracking)
- Responsive design (desktop, tablet, mobile)
- Accessibility features (WCAG 2.1 AA compliance)
- Offline mode (service worker caching)

**UI Apps:** StudentApp (Blazor), Dashboard (Blazor), AdminApp (Blazor)

**Key Takeaway:** Interactive Blazor Server apps with real-time updates, auto-save, and responsive design.

---

#### [09d. Analytics and Reporting Features](./09d-analytics-reporting-features.md) (600 lines)

**Purpose:** Student analytics capabilities  
**Audience:** Data analysts, product managers  
**Contents:**

- 7 analytics endpoints with detailed specifications
- Student performance summary (overall score, mastery level, trend)
- Subject-level performance breakdown
- Learning objective mastery tracking
- IRT-based ability estimates
- Weak area identification (bottom 3 objectives)
- Progress timeline (daily/weekly/monthly aggregation)
- Peer comparison with k-anonymity protection (k≥5)
- Export formats (PDF, Excel, PowerPoint)

**Analytics Endpoints:** 7 REST APIs for comprehensive student insights

**Key Takeaway:** Rich analytics with k-anonymity protection enabling data-driven learning personalization.

---

#### [09e. Known Issues and Limitations](./09e-known-issues-limitations.md) (700 lines)

**Purpose:** Technical debt and limitations  
**Audience:** Development team, project managers  
**Contents:**

- .NET 9 WebApplicationFactory bug (integration test workaround)
- Azure deployment challenges (PostgreSQL password issue resolved)
- OLLAMA performance (20-25s CPU, needs GPU or Azure OpenAI)
- 19 TODO comments in codebase (categorized by priority)
- Missing features (coding sandbox, video proctoring, mobile apps)
- Technical debt items (test coverage gaps, performance optimization)
- Security considerations (rate limiting, input validation)
- Known bugs and workarounds

**Critical Issues:** OLLAMA performance (production blocker), Load testing (0% complete)

**Key Takeaway:** Honest assessment of current limitations with prioritized roadmap for resolution.

---

### Part 3: API Reference (Documents 10a-10c)

REST API documentation organized by controller.

#### [10a. Assessment API](./10a-assessment-api.md) (850 lines)

**Purpose:** Assessment management API documentation  
**Audience:** Frontend developers, API consumers  
**Contents:**

- 6 HTTP endpoints with full specifications
- Request/response examples (JSON)
- 11 DTO definitions with C# code
- Error handling and status codes
- Authentication and authorization requirements
- Rate limiting rules
- Implementation status (3 TODOs for persistence)

**Endpoints:**

- `GET /api/v1/assessments` - List available assessments
- `GET /api/v1/assessments/{id}` - Get assessment details
- `GET /api/v1/assessments/{id}/session` - Get or create session
- `POST /api/v1/assessments/{id}/responses` - Save responses (auto-save)
- `POST /api/v1/assessments/{id}/submit` - Submit assessment
- `GET /api/v1/assessments/{id}/results` - Get results

**Key Takeaway:** RESTful API for assessment lifecycle (discovery → session → responses → submission → results).

---

#### [10b. Student Analytics API](./10b-student-analytics-api.md) (1,015 lines)

**Purpose:** Student analytics API documentation  
**Audience:** Frontend developers, data analysts  
**Contents:**

- 7 HTTP endpoints for analytics data
- Request/response examples with realistic data
- 7 DTO definitions (StudentPerformanceSummary, SubjectPerformance, etc.)
- K-anonymity enforcement logic
- Role-based access control (students see own data, teachers see class data)
- Caching strategy (15-minute TTL)
- Query parameter options

**Endpoints:**

- `GET /api/v1/students/{id}/analytics/performance-summary` - Overall metrics
- `GET /api/v1/students/{id}/analytics/subject-performance` - Subject breakdown
- `GET /api/v1/students/{id}/analytics/learning-objectives` - Mastery tracking
- `GET /api/v1/students/{id}/analytics/ability-estimates` - IRT ability estimates
- `GET /api/v1/students/{id}/analytics/improvement-areas` - Weak areas
- `GET /api/v1/students/{id}/analytics/progress-timeline` - Progress over time
- `GET /api/v1/students/{id}/analytics/peer-comparison` - Peer benchmarking

**Key Takeaway:** Comprehensive analytics API with k-anonymity protection and role-based data access.

---

#### [10c. System Health API](./10c-system-health-api.md) (697 lines)

**Purpose:** Health check and monitoring endpoints  
**Audience:** DevOps engineers, monitoring systems  
**Contents:**

- 2 HTTP endpoints for health monitoring
- 3 health check implementations (self, PostgreSQL, Redis)
- Response format (JSON health report)
- Kubernetes liveness and readiness probe configurations
- Health check tags and filtering
- Security considerations (development-only exposure)
- Integration with Application Insights

**Endpoints:**

- `GET /health` - Comprehensive health check (all dependencies)
- `GET /alive` - Simple liveness check (process running)

**Health Checks:** Self (liveness), PostgreSQL (unhealthy on failure), Redis (degraded on failure)

**Key Takeaway:** Standard health check endpoints for Kubernetes orchestration and monitoring systems.

---

### Part 4: User Workflows (Documents 11a-11c)

End-to-end user journey documentation organized by user role.

#### [11a. Student Workflows](./11a-student-workflows.md) (1,005 lines)

**Purpose:** Complete student user journey  
**Audience:** Product managers, UX designers, QA engineers  
**Contents:**

- 7 workflows from registration to adaptive learning
- 6 sequence diagrams showing API interactions
- UI details (question palette, timer, auto-save)
- Common scenarios (time expires, connection lost, browser closed)
- Troubleshooting guide (session expired, auto-save failures)
- Database operations for each workflow
- API calls with request/response examples

**Workflows:**

1. Registration and onboarding (Azure AD B2C authentication)
2. Assessment discovery and selection
3. Taking assessments (auto-save every 30s, Redis cache)
4. Reviewing results and feedback
5. Progress tracking dashboard
6. Identifying weak areas
7. Adaptive learning path recommendations

**Key Takeaway:** Student-centric workflows with auto-save, offline support, and personalized learning recommendations.

---

#### [11b. Teacher Workflows](./11b-teacher-workflows.md) (1,329 lines)

**Purpose:** Complete teacher user journey  
**Audience:** Product managers, UX designers, QA engineers  
**Contents:**

- 7 workflows from class setup to analytics
- 5 sequence diagrams for key teacher actions
- 9 question types with creation examples
- Real-time monitoring with SignalR updates
- LLM-assisted grading (OLLAMA 20-25s per essay)
- Batch grading optimization strategies
- Common scenarios (time extensions, assessment mistakes, bulk import failures)

**Workflows:**

1. Registration and class setup
2. Creating assessments (9 question types, IRT parameters)
3. Assigning assessments to classes
4. Real-time progress monitoring (SignalR dashboard)
5. Grading and feedback (LLM-assisted, batch processing)
6. Generating progress reports (PDF, Excel, PowerPoint)
7. Class analytics (performance trends, heat maps, at-risk identification)

**Key Takeaway:** Powerful teacher tools including LLM-assisted grading, real-time monitoring, and predictive analytics.

---

#### [11c. Administrator Workflows](./11c-admin-workflows.md) (1,449 lines)

**Purpose:** Complete admin user journey  
**Audience:** System administrators, school admins  
**Contents:**

- 7 major admin workflow areas
- 4 sequence diagrams for key admin operations
- School and user management (RBAC)
- System configuration (feature flags, integrations)
- School-wide analytics (KPIs, cross-school comparison)
- Content management (learning objectives, question bank)
- Infrastructure monitoring (Container Apps, Database, Redis)
- GDPR compliance tools (DSAR, right to erasure)

**Workflows:**

1. School and organization management
2. User management and access control (bulk import, RBAC)
3. System configuration (app settings, feature flags, integrations)
4. School-wide analytics dashboard (KPIs, visualizations)
5. Content and curriculum management (learning objectives, templates)
6. System monitoring and health (logs, traces, performance)
7. Data export and compliance (GDPR, backups)

**Admin Roles:** SchoolAdmin, SystemAdmin, BusinessAdmin, CourseAdmin

**Key Takeaway:** Comprehensive admin capabilities for school management, user administration, and system operations.

---

### Part 5: Performance and Deployment (Documents 12-13)

Non-functional requirements and operational documentation.

#### [12. Performance Requirements](./12-performance.md) (1,126 lines)

**Purpose:** Performance targets and optimization  
**Audience:** Performance engineers, DevOps, architects  
**Contents:**

- Performance targets (API <500ms P95, LLM <30s, database <100ms)
- Scalability requirements (50 to 50,000+ concurrent users)
- Resource requirements per deployment tier (CPU, memory, database, cache)
- Current performance characteristics (with stub data)
- Optimization strategies (caching, indexing, batch processing, compression)
- Load testing plans (0% complete - high priority)
- Performance monitoring (OpenTelemetry, Application Insights)
- Bottleneck analysis (LLM latency main blocker)

**Targets:**

- **API Response**: <500ms P95 for most endpoints, <2s for complex operations
- **LLM Evaluation**: <30s CPU (dev), <3s GPU, <2s Azure OpenAI (prod)
- **Database Queries**: <100ms simple, <500ms complex
- **Cache Hit Rate**: >80%
- **Throughput**: 1000 requests/sec sustained, 2000 req/sec peak

**Scalability Tiers:**

- **Small (100 users)**: $575/month
- **Medium (1K users)**: $1,450/month
- **Large (10K users)**: $4,400/month
- **Enterprise (100K users)**: $16,700/month

**Key Takeaway:** Clear performance targets with optimization strategies; LLM latency is main production blocker.

---

#### [13. Deployment Models](./13-deployment-models.md) (1,056 lines)

**Purpose:** Deployment architecture and procedures  
**Audience:** DevOps engineers, system administrators, developers  
**Contents:**

- Local development with .NET Aspire 9.5.1 (one-command startup)
- Azure Container Apps deployment (azd automation)
- CI/CD pipeline with GitHub Actions
- Environment configuration (dev, staging, production)
- Migration strategy (4 phases, Phase 1 complete)
- Monitoring and observability (Application Insights, Aspire Dashboard)
- Disaster recovery (backups, HA, multi-region)
- Troubleshooting guide

**Deployment Models:**

1. **Local Development**: .NET Aspire + Docker (free, low complexity)
2. **Azure Container Apps**: Production for <10K users (medium complexity)
3. **Azure Kubernetes Service**: Production for 10K+ users (high complexity)

**CI/CD Pipeline:**

- **CI**: Build → Test → Coverage → Security Scan
- **CD**: Dev (auto) → Staging (auto) → Production (manual approval)
- **Rollback**: Automatic on health check failure

**Migration Phases:**

- **Phase 1** (✅ Complete): Aspire migration (.NET 9, Aspire 9.5.1)
- **Phase 2** (🎯 In Progress): Azure deployment with azd (2-3 weeks)
- **Phase 3** (⏳ Planned): Production hardening (1-2 months)
- **Phase 4** (⏳ Future): Scale to AKS if needed (3-6 months)

**Key Takeaway:** Modern deployment with .NET Aspire and Azure Container Apps, automated CI/CD, comprehensive DR strategy.

---

## Document Categories

### By Audience

**Executive/Management:**

- [01. Executive Summary](./01-executive-summary.md)
- [09e. Known Issues and Limitations](./09e-known-issues-limitations.md)

**Architects/Senior Engineers:**

- [02. System Architecture](./02-system-architecture.md)
- [03. Domain Model](./03-domain-model.md)
- [07. Security and Privacy](./07-security-privacy.md)
- [12. Performance Requirements](./12-performance.md)

**Backend Developers:**

- [04. Application Components](./04-application-components.md)
- [05. Data Storage](./05-data-storage.md)
- [06. External Integrations](./06-external-integrations.md)
- [10a. Assessment API](./10a-assessment-api.md)
- [10b. Student Analytics API](./10b-student-analytics-api.md)
- [10c. System Health API](./10c-system-health-api.md)

**Frontend Developers:**

- [09c. User Interface Features](./09c-user-interface-features.md)
- [11a. Student Workflows](./11a-student-workflows.md)
- [11b. Teacher Workflows](./11b-teacher-workflows.md)
- [11c. Administrator Workflows](./11c-admin-workflows.md)

**DevOps/SRE:**

- [08. Observability and Monitoring](./08-observability.md)
- [12. Performance Requirements](./12-performance.md)
- [13. Deployment Models](./13-deployment-models.md)

**Data Scientists/AI Engineers:**

- [09b. Agent Orchestration Features](./09b-agent-orchestration-features.md)
- [09a. Core Assessment Features](./09a-core-assessment-features.md) (IRT algorithms)

**QA/Testing:**

- [11a. Student Workflows](./11a-student-workflows.md)
- [11b. Teacher Workflows](./11b-teacher-workflows.md)
- [11c. Administrator Workflows](./11c-admin-workflows.md)
- [09e. Known Issues and Limitations](./09e-known-issues-limitations.md)

**Product Managers:**

- [01. Executive Summary](./01-executive-summary.md)
- [09a-09d. Feature Documentation](./09a-core-assessment-features.md)
- [09e. Known Issues and Limitations](./09e-known-issues-limitations.md)

### By Development Phase

**Phase 0: Planning (Before coding)**

- [01. Executive Summary](./01-executive-summary.md)
- [02. System Architecture](./02-system-architecture.md)
- [03. Domain Model](./03-domain-model.md)

**Phase 1: Foundation (Core infrastructure)**

- [04. Application Components](./04-application-components.md)
- [05. Data Storage](./05-data-storage.md)
- [07. Security and Privacy](./07-security-privacy.md)
- [08. Observability and Monitoring](./08-observability.md)

**Phase 2: Core Features (MVP)**

- [09a. Core Assessment Features](./09a-core-assessment-features.md)
- [09c. User Interface Features](./09c-user-interface-features.md)
- [10a. Assessment API](./10a-assessment-api.md)
- [11a. Student Workflows](./11a-student-workflows.md)
- [11b. Teacher Workflows](./11b-teacher-workflows.md)

**Phase 3: Advanced Features**

- [09b. Agent Orchestration Features](./09b-agent-orchestration-features.md)
- [09d. Analytics and Reporting Features](./09d-analytics-reporting-features.md)
- [06. External Integrations](./06-external-integrations.md)
- [10b. Student Analytics API](./10b-student-analytics-api.md)
- [11c. Administrator Workflows](./11c-admin-workflows.md)

**Phase 4: Production Readiness**

- [12. Performance Requirements](./12-performance.md)
- [13. Deployment Models](./13-deployment-models.md)
- [10c. System Health API](./10c-system-health-api.md)

**Phase 5: Continuous Improvement**

- [09e. Known Issues and Limitations](./09e-known-issues-limitations.md)

---

## Key Technologies

### Backend

- **.NET 9.0** - Latest LTS framework
- **C# 13** - Modern language features
- **ASP.NET Core 9.0** - Web API and Blazor Server
- **Entity Framework Core 9.0** - ORM with PostgreSQL provider

### Database & Cache

- **PostgreSQL 16** - Primary relational database (17 tables)
- **Redis 7** - Distributed cache (4-hour session TTL)
- **Azure Database for PostgreSQL** - Managed database service
- **Azure Cache for Redis** - Managed cache service

### AI/ML

- **OLLAMA** - Local LLM (llama3.2:3b, 20-25s per evaluation)
- **Azure OpenAI GPT-4o** - Production LLM (<2s per evaluation)
- **IRT (Item Response Theory)** - Adaptive assessment algorithm

### Frontend

- **Blazor Server** - Interactive web UI (3 apps)
- **SignalR** - Real-time WebSocket communication
- **Bootstrap 5** - Responsive CSS framework

### Cloud Platform

- **Azure Container Apps** - Serverless container hosting
- **Azure Front Door** - Global load balancer
- **Azure Key Vault** - Secrets management
- **Application Insights** - APM and monitoring

### Observability

- **OpenTelemetry** - Distributed tracing and metrics
- **Serilog** - Structured logging
- **Grafana** - Dashboard visualization
- **Prometheus** - Metrics collection

### DevOps

- **.NET Aspire 9.5.1** - Cloud-native orchestration
- **Docker** - Containerization
- **GitHub Actions** - CI/CD pipelines
- **Azure Developer CLI (azd)** - Deployment automation

### Authentication

- **Azure AD B2C** - Identity provider
- **JWT Bearer Tokens** - API authentication
- **OAuth 2.0 / OpenID Connect** - Auth protocols

---

## Implementation Status

### ✅ Complete (Production-Ready)

- System architecture and design
- Domain model and database schema
- Core assessment engine (9 question types)
- REST API (6 assessment endpoints, 7 analytics endpoints, 2 health endpoints)
- Blazor UI applications (Student, Dashboard, Admin)
- Azure AD B2C authentication
- Role-based access control (RBAC)
- OpenTelemetry observability
- Integration tests (57 tests passing)
- .NET Aspire local development
- Documentation (19,372 lines)

### 🎯 In Progress

- Azure Container Apps deployment (Phase 2)
- Load testing (0% complete - high priority)
- Database persistence (3 TODOs in AssessmentController)
- Performance optimization (LLM GPU/Azure OpenAI migration)

### ⏳ Planned

- Coding question sandbox
- Video proctoring integration
- Native mobile apps (iOS, Android)
- Multi-region deployment
- Kubernetes migration (if scale requires)

### ❌ Known Issues

- .NET 9 WebApplicationFactory bug (workaround in place)
- OLLAMA performance (20-25s CPU, needs GPU or Azure OpenAI)
- Load testing not performed (required before production)
- Redis cache evictions (125/hour, needs tuning)

**See [09e. Known Issues and Limitations](./09e-known-issues-limitations.md) for complete list.**

---

## Getting Started

### For Developers

**1. Read Core Documentation:**

- [Executive Summary](./01-executive-summary.md) - Understand the vision
- [System Architecture](./02-system-architecture.md) - Understand the design
- [Domain Model](./03-domain-model.md) - Understand the entities

**2. Set Up Local Environment:**

- Follow [Deployment Models](./13-deployment-models.md) - Local Development section
- Prerequisites: .NET 9 SDK, Docker Desktop
- Start with: `dotnet run --project src/EduMind.AppHost`
- Access dashboard: `https://localhost:15888`

**3. Explore the Codebase:**

- **Domain Layer**: `src/AcademicAssessment.Core/`
- **Application Layer**: `src/AcademicAssessment.Infrastructure/`, `src/AcademicAssessment.Analytics/`
- **API Layer**: `src/AcademicAssessment.Web/`
- **UI Layer**: `src/AcademicAssessment.StudentApp/`, `src/AcademicAssessment.Dashboard/`

**4. Run Tests:**

```bash
# Unit tests
dotnet test tests/AcademicAssessment.Tests.Unit

# Integration tests
dotnet test tests/AcademicAssessment.Tests.Integration
```

**5. Build a Feature:**

- Reference [Application Components](./04-application-components.md) for service patterns
- Reference [API Documentation](./10a-assessment-api.md) for endpoint specs
- Reference [User Workflows](./11a-student-workflows.md) for user journeys

### For DevOps Engineers

**1. Read Infrastructure Documentation:**

- [Deployment Models](./13-deployment-models.md) - Complete deployment guide
- [Performance Requirements](./12-performance.md) - Scaling and performance targets
- [Observability](./08-observability.md) - Monitoring and alerting

**2. Deploy to Azure:**

```bash
# Install Azure Developer CLI
curl -fsSL https://aka.ms/install-azd.sh | bash

# Login and deploy
azd auth login
azd init
azd up
```

**3. Set Up Monitoring:**

- Application Insights for APM
- Azure Monitor for logs and metrics
- Grafana dashboards for visualization
- Configure alerts (error rate, latency, health checks)

**4. Configure CI/CD:**

- GitHub Actions workflows in `.github/workflows/`
- Environments: dev, staging, production
- Manual approval required for production

### For Product Managers

**1. Understand Features:**

- [Executive Summary](./01-executive-summary.md) - High-level overview
- [Core Assessment Features](./09a-core-assessment-features.md) - What the system does
- [User Workflows](./11a-student-workflows.md) - How users interact

**2. Track Progress:**

- [Known Issues and Limitations](./09e-known-issues-limitations.md) - Current gaps
- Implementation Status section above - What's complete vs. planned

**3. Plan Roadmap:**

- Review "Planned" features in Implementation Status
- Prioritize based on user feedback and business value
- Consider performance implications (see [Performance Requirements](./12-performance.md))

---

## Maintenance

### Keeping Documentation Updated

**When to update:**

- ✅ New feature added → Update relevant feature document (09a-09e)
- ✅ API changed → Update API reference (10a-10c)
- ✅ Architecture changed → Update system architecture (02)
- ✅ Deployment process changed → Update deployment models (13)
- ✅ Performance targets changed → Update performance requirements (12)

**Review schedule:**

- **Weekly**: Review implementation status and known issues
- **Monthly**: Review and update API documentation
- **Quarterly**: Full specification review and update
- **Major releases**: Complete documentation audit

**Document owners:**

- **Architecture docs (01-08)**: System Architect
- **Feature docs (09a-09e)**: Product Manager + Tech Lead
- **API docs (10a-10c)**: Backend Team Lead
- **Workflow docs (11a-11c)**: Product Manager + UX Designer
- **Operations docs (12-13)**: DevOps Lead

---

## Contributing

### Documentation Standards

**Format:**

- Markdown with GitHub Flavored Markdown extensions
- Mermaid diagrams for architecture and workflows
- Code blocks with language syntax highlighting
- Tables for structured data

**Structure:**

- Clear table of contents
- Progressive disclosure (overview → details)
- Code examples where applicable
- Cross-references to related documents

**Style:**

- Present tense ("The system does..." not "The system will do...")
- Active voice ("The service processes..." not "The data is processed...")
- Technical but accessible (avoid jargon, explain acronyms)
- Consistent terminology (refer to [03. Domain Model](./03-domain-model.md) for entity names)

### Version Control

- **Branch strategy**: Create feature branch for doc updates
- **Commit messages**: Use conventional commits (e.g., `docs: update API reference with new endpoint`)
- **Pull requests**: Required for all documentation changes
- **Reviews**: At least one approval required

---

## Related Documentation

### In Repository

- **Source Code**: `src/` directory
- **Tests**: `tests/` directory
- **Infrastructure**: `infra/` directory (Bicep templates)
- **Deployment Docs**: `docs/deployment/`
- **Development Guides**: `docs/development/`
- **Architecture Decisions**: `docs/architecture/`

### External Resources

- **.NET 9 Documentation**: <https://learn.microsoft.com/en-us/dotnet/>
- **Azure Container Apps**: <https://learn.microsoft.com/en-us/azure/container-apps/>
- **.NET Aspire**: <https://learn.microsoft.com/en-us/dotnet/aspire/>
- **PostgreSQL Documentation**: <https://www.postgresql.org/docs/>
- **Redis Documentation**: <https://redis.io/documentation>
- **OpenTelemetry**: <https://opentelemetry.io/docs/>

---

## Document Statistics

| Metric | Value |
|--------|-------|
| **Total Documents** | 21 |
| **Total Lines** | 19,372 |
| **Largest Document** | 11c-admin-workflows.md (1,449 lines) |
| **Smallest Document** | 01-executive-summary.md (520 lines) |
| **Average Length** | 923 lines |
| **Code Examples** | 150+ |
| **Diagrams** | 25+ (Mermaid) |
| **API Endpoints Documented** | 15 |
| **Workflows Documented** | 21 |
| **Last Updated** | January 20, 2025 |

---

## Feedback and Questions

For questions or feedback about this documentation:

1. **Create an issue**: [GitHub Issues](https://github.com/johnazariah/edumind-ai/issues)
2. **Discussion**: [GitHub Discussions](https://github.com/johnazariah/edumind-ai/discussions)
3. **Pull request**: Suggest improvements via PR

---

**Document Status:** Complete ✅  
**Version:** 1.0  
**Last Updated:** January 20, 2025  
**Maintainer:** GitHub Copilot  
**Total Specification Size:** 19,372 lines across 21 documents
