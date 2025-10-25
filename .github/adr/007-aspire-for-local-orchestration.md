# ADR-007: .NET Aspire for Local Orchestration

**Status:** ✅ Accepted  
**Date:** October 2025  
**Context:** Local Development Experience

## Context

Local development required orchestrating multiple services:

- Web API (ASP.NET Core)
- Student App (Blazor Server)
- Dashboard (Blazor Server)
- PostgreSQL database
- Redis cache
- Ollama LLM server

Challenges:

- Manual docker-compose setup error-prone
- Connection string management across services
- Difficult to observe service health
- No unified logging/tracing
- Hard to debug inter-service communication

Options:

- Docker Compose (manual setup, limited observability)
- Kubernetes (overkill for local dev)
- .NET Aspire (cloud-native orchestration)
- Manual processes (too complex)
- Tye (Microsoft, but deprecated)

## Decision

Selected **.NET Aspire 9.5.1** (via `EduMind.AppHost`) for local development orchestration.

## Rationale

1. **Unified Experience**: Single `dotnet run` starts all services
2. **Service Discovery**: Automatic connection string injection
3. **Dashboard**: Built-in Aspire dashboard (<http://localhost:15888>)
4. **OpenTelemetry**: Distributed tracing out-of-the-box
5. **Health Checks**: Visual health monitoring for all services
6. **Live Reload**: Watch mode for code changes
7. **Azure Parity**: Similar patterns to Azure Container Apps

## Consequences

### Positive

- One command (`dotnet run --project src/EduMind.AppHost`) starts entire stack
- Automatic service discovery (no hard-coded connection strings)
- Real-time logs from all services in Aspire dashboard
- Distributed tracing shows request flow across services
- Easy to add new services (Ollama, PostgreSQL, Redis)
- Health checks visible at a glance

### Negative

- Requires .NET Aspire workload (`dotnet workload install aspire`)
- Additional learning curve for Aspire concepts
- Dashboard runs on separate port (15888)
- Some CI/CD complexity (azd integration)
- Breaking changes between Aspire versions

### Risks Mitigated

- Pinned Aspire version (9.5.1) in all projects
- Documented Aspire setup in README.md
- CI/CD workflows handle Aspire workload installation
- Dev container pre-configured with Aspire

## Implementation Details

**AppHost Configuration** (src/EduMind.AppHost/Program.cs):

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure services
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("edumind");

var cache = builder.AddRedis("cache")
    .WithRedisCommander();

var ollama = builder.AddContainer("ollama", "ollama/ollama")
    .WithBindMount("ollama-data", "/root/.ollama")
    .WithHttpEndpoint(port: 11434, targetPort: 11434);

// Application services with service discovery
var webapi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
    .WithReference(postgres)
    .WithReference(cache)
    .WithReference(ollama);

var studentApp = builder.AddProject<Projects.AcademicAssessment_StudentApp>("studentapp")
    .WithReference(webapi);

builder.Build().Run();
```

**ServiceDefaults Integration**:
All projects reference `EduMind.ServiceDefaults` for:

- OpenTelemetry tracing
- Health checks
- Service discovery
- Resilience policies

```csharp
// In each project's Program.cs
builder.AddServiceDefaults();
```

**Connection String Injection**:
Aspire automatically injects connection strings:

- `ConnectionStrings__edumind` → PostgreSQL connection string
- `ConnectionStrings__cache` → Redis connection string
- `services__ollama__http__0` → Ollama endpoint

## Local Development Workflow

1. Clone repository
2. Install Aspire workload: `dotnet workload install aspire`
3. Run: `dotnet run --project src/EduMind.AppHost`
4. Open Aspire dashboard: <http://localhost:15888>
5. Access applications:
   - Web API: <http://localhost:5000>
   - Student App: <http://localhost:5001>
   - Dashboard: <http://localhost:5002>

## Observability Features

**Aspire Dashboard provides:**

- **Structured Logs**: Filterable by service, level, timestamp
- **Traces**: Distributed tracing with OpenTelemetry
- **Metrics**: CPU, memory, request rates per service
- **Console Output**: Real-time stdout/stderr from services
- **Environment Variables**: View/edit service configuration

## Production Deployment

While Aspire orchestrates local development, production uses:

- **Azure Container Apps** (not Aspire in production)
- **azd** (Azure Developer CLI) for deployment
- Aspire manifest → Bicep conversion for infrastructure

## Alternative Considered: Docker Compose

**Rejected because:**

- No built-in observability dashboard
- Manual connection string management
- No automatic service discovery
- Limited health check visualization
- Harder to debug distributed issues

## Related Decisions

- ADR-001: .NET 9.0 Framework Selection (Aspire requires .NET 8+)
- ADR-004: Ollama for Local LLM (orchestrated by Aspire)
- ADR-020: Azure Container Apps (production deployment)
- ADR-080: Application Insights Integration (production observability)

## References

- `src/EduMind.AppHost/` - Aspire orchestration project
- `src/EduMind.ServiceDefaults/` - Shared service configuration
- Commit: `28044ab` - "feat: Upgrade to .NET 9 and Aspire 9.5.1"
- Commit: `702fb08` - "feat: Complete ServiceDefaults integration"
- docs/deployment/LOCAL_DEPLOYMENT_GUIDE.md
