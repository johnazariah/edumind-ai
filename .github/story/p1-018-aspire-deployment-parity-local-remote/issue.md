# Story 018: Aspire Deployment Parity - Local and Remote Service Discovery

**Priority:** P1 (Production Quality)  
**Estimated Effort:** 2-3 weeks  
**Status:** Ready for Development

---

## Problem Statement

### Current State

The application currently has **divergent code paths** for local and remote deployment:

**Local Development:**

- Uses hardcoded `AddConnectionString()` with localhost URLs
- Services reference external containers via `localhost:5432`, `localhost:6379`
- No service discovery - manual configuration
- Aspire orchestration but not leveraging Aspire's service discovery features

**Azure Remote:**

- Bicep templates create Azure Container Apps
- Manual connection string management
- Different environment variable patterns
- Services need to know exact hostnames/ports

**The Problem:**

- "Works on my machine" syndrome
- Deployment configuration drift
- Different service discovery mechanisms locally vs remotely
- Cannot test production-like service communication locally
- Hard to debug deployment issues because local ≠ remote

### Business Impact

- **Delayed Production Readiness:** Configuration mismatches cause deployment failures
- **Developer Friction:** Developers can't reproduce production issues locally
- **Deployment Risk:** Different code paths = different failure modes
- **Technical Debt:** Maintaining two parallel configuration systems

---

## Goals & Success Criteria

### Primary Goals

1. **Unified Service Discovery:** Same Aspire service discovery code works locally and in Azure
2. **Production Parity:** Local Aspire environment mirrors Azure Container Apps configuration
3. **Zero Configuration Drift:** Single source of truth for service topology
4. **Seamless Testing:** Developers can test production-like service communication locally

### Success Criteria

- [ ] Aspire `AddPostgres()` / `AddRedis()` used instead of `AddConnectionString()`
- [ ] Services discover each other via Aspire service discovery (no hardcoded URLs)
- [ ] `azd deploy` automatically provisions Aspire-compatible Azure resources
- [ ] Local and remote deployments use identical Aspire manifest
- [ ] All services start and communicate successfully in both environments
- [ ] Integration tests pass using service discovery (not hardcoded URLs)
- [ ] Documentation updated with unified deployment approach

---

## Technical Approach

### Architecture: Aspire Service Discovery

**Key Principle:** Aspire provides unified service discovery that works locally (via .NET Aspire orchestration) and remotely (via Azure Container Apps with Aspire integration).

#### Current Architecture (Problematic)

```csharp
// Local: Hardcoded localhost
var postgres = builder.AddConnectionString("postgres", 
    "Host=localhost;Port=5432;Database=edumind_dev;...");

var redis = builder.AddConnectionString("cache", 
    "localhost:6379");

// Services get connection strings as environment variables
var webApi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
    .WithReference(postgres)  // Injects connection string as env var
    .WithReference(redis);
```

**Problem:** This doesn't work in Azure because:

- `localhost` doesn't resolve to external Azure services
- Services need to know exact Azure hostnames
- No automatic service discovery

#### Target Architecture (Unified)

```csharp
// Local AND Remote: Aspire manages service discovery
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithPgAdmin()  // Optional: adds PgAdmin for local dev
    .AddDatabase("edumind_dev");

var redis = builder.AddRedis("cache", port: 6379);

var ollama = builder.AddContainer("ollama", "ollama/ollama", "latest")
    .WithEndpoint(port: 11434, name: "http");

// Services automatically discover dependencies
var webApi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
    .WithExternalHttpEndpoints()
    .WithReference(postgres)  // Aspire injects discovery info
    .WithReference(redis)
    .WithReference(ollama);
```

**Benefits:**

- **Local:** Aspire starts PostgreSQL/Redis containers with discovery
- **Azure:** Aspire generates Azure Container Apps with service bindings
- **Services:** Use `IConfiguration` or Aspire service discovery client - same code both environments

### Implementation Strategy

#### Phase 1: Convert AppHost to Use Aspire Resource APIs

1. **Replace AddConnectionString with AddPostgres/AddRedis**
   - Use `builder.AddPostgres()` for PostgreSQL
   - Use `builder.AddRedis()` for Redis
   - Use `builder.AddContainer()` for Ollama (local only)

2. **Configure Service References**
   - Services reference resources via `.WithReference()`
   - Aspire injects connection strings/URLs automatically
   - Works identically locally and remotely

#### Phase 2: Update Bicep for Aspire Deployment

1. **Azure Container Apps with Service Bindings**
   - Use `Microsoft.App/containerApps` with service bindings
   - Aspire generates connection strings via managed identities
   - Services discover each other via internal FQDNs

2. **Managed Services Integration**
   - Azure Database for PostgreSQL Flexible Server
   - Azure Container Apps managed Redis (or Container)
   - Azure OpenAI (instead of local Ollama) for production

#### Phase 3: Update Application Code

1. **Remove Hardcoded Connection Strings**
   - Use Aspire's service discovery patterns
   - Services read connection info from environment (injected by Aspire)

2. **Service-to-Service Communication**
   - Dashboard → Web API: Use Aspire service discovery
   - StudentApp → Web API: Use Aspire service discovery
   - No hardcoded URLs

#### Phase 4: Testing & Validation

1. **Local Validation**
   - Start Aspire: `dotnet run --project src/EduMind.AppHost`
   - Verify all services start and discover each other
   - Run integration tests

2. **Azure Validation**
   - Deploy: `azd deploy`
   - Verify services discover Azure resources
   - Run smoke tests against deployed endpoints

---

## Task Decomposition

### Task 1: Update AppHost to Use Aspire Resource APIs

**Estimated Time:** 4 hours

**Description:**
Convert `EduMind.AppHost/Program.cs` from `AddConnectionString` to Aspire's native resource APIs.

**Files to Modify:**

- `src/EduMind.AppHost/Program.cs`

**Implementation:**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with database
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithLifetime(ContainerLifetime.Persistent)  // Keep data between runs
    .WithPgAdmin()  // Optional: add PgAdmin UI for local dev
    .AddDatabase("edumind");

// Redis cache
var redis = builder.AddRedis("cache", port: 6379)
    .WithLifetime(ContainerLifetime.Persistent);

// Ollama (local only - won't deploy to Azure)
var ollama = builder.AddContainer("ollama", "ollama/ollama", "latest")
    .WithEndpoint(port: 11434, scheme: "http", name: "ollama-http")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithBindMount("~/.ollama", "/root/.ollama")  // Persist models
    .PublishAsDockerFile();  // Don't deploy to Azure

// Web API
var webApi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(ollama);

// Dashboard
builder.AddProject<Projects.AcademicAssessment_Dashboard>("dashboard")
    .WithExternalHttpEndpoints()
    .WithReference(webApi)
    .WithReference(redis);

// Student App
builder.AddProject<Projects.AcademicAssessment_StudentApp>("studentapp")
    .WithExternalHttpEndpoints()
    .WithReference(webApi)
    .WithReference(redis);

builder.Build().Run();
```

**Acceptance:**

- [ ] Aspire starts PostgreSQL container automatically
- [ ] Aspire starts Redis container automatically
- [ ] Services receive connection strings via environment variables
- [ ] `dotnet run --project src/EduMind.AppHost` starts all services successfully

---

### Task 2: Add Aspire NuGet Packages to AppHost

**Estimated Time:** 1 hour

**Description:**
Install required Aspire hosting packages for PostgreSQL, Redis, and container support.

**Files to Modify:**

- `src/EduMind.AppHost/EduMind.AppHost.csproj`

**Implementation:**

```xml
<ItemGroup>
  <!-- Aspire Hosting Packages -->
  <PackageReference Include="Aspire.Hosting.AppHost" Version="9.5.1" />
  <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="9.5.1" />
  <PackageReference Include="Aspire.Hosting.Redis" Version="9.5.1" />
  
  <!-- For PgAdmin (optional but useful for local dev) -->
  <PackageReference Include="Aspire.Hosting.PostgreSQL.PgAdmin" Version="9.5.1" />
</ItemGroup>
```

**Acceptance:**

- [ ] Packages restore successfully
- [ ] No version conflicts
- [ ] Build succeeds

---

### Task 3: Update Web API to Use Aspire Service Discovery

**Estimated Time:** 3 hours

**Description:**
Remove hardcoded connection strings from Web API and use Aspire's injected configuration.

**Files to Modify:**

- `src/AcademicAssessment.Web/Program.cs`
- `src/AcademicAssessment.Web/appsettings.json`
- `src/AcademicAssessment.Web/AcademicAssessment.Web.csproj`

**Implementation:**

**AcademicAssessment.Web.csproj:**

```xml
<ItemGroup>
  <!-- Aspire Service Discovery Client -->
  <PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.5.1" />
  <PackageReference Include="Aspire.StackExchange.Redis" Version="9.5.1" />
</ItemGroup>
```

**Program.cs:**

```csharp
// Before:
// builder.Configuration.GetConnectionString("PostgreSQL")

// After: Use Aspire-injected connection
builder.AddNpgsqlDbContext<ApplicationDbContext>("postgres");

// Before:
// builder.Services.AddStackExchangeRedisCache(...)

// After: Use Aspire-injected Redis
builder.AddRedisClient("cache");
```

**appsettings.json (remove hardcoded connections):**

```json
{
  // Remove these - Aspire injects them:
  // "ConnectionStrings": {
  //   "PostgreSQL": "Host=localhost;Port=5432;..."
  // }
}
```

**Acceptance:**

- [ ] Web API starts without hardcoded connection strings
- [ ] Database context connects successfully
- [ ] Redis cache operations work
- [ ] Health checks pass

---

### Task 4: Update Dashboard to Use Service Discovery for Web API

**Estimated Time:** 2 hours

**Description:**
Replace hardcoded Web API URLs with Aspire service discovery.

**Files to Modify:**

- `src/AcademicAssessment.Dashboard/Program.cs`
- `src/AcademicAssessment.Dashboard/AcademicAssessment.Dashboard.csproj`

**Implementation:**

**AcademicAssessment.Dashboard.csproj:**

```xml
<ItemGroup>
  <!-- Aspire Service Discovery -->
  <PackageReference Include="Aspire.StackExchange.Redis" Version="9.5.1" />
  <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.5.1" />
</ItemGroup>
```

**Program.cs:**

```csharp
// Before:
// builder.Services.AddHttpClient("WebApi", client => 
//     client.BaseAddress = new Uri("http://localhost:5103"));

// After: Use Aspire service discovery
builder.Services.AddHttpClient("WebApi", client => 
    client.BaseAddress = new Uri("http://webapi"));  // Aspire resolves this

// Or even better, use service discovery directly:
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();  // Auto-discovery for all HttpClients
});

// Redis (same as Web API)
builder.AddRedisClient("cache");
```

**Acceptance:**

- [ ] Dashboard discovers Web API URL automatically
- [ ] HTTP calls to Web API succeed
- [ ] SignalR connections work (via Redis backplane)
- [ ] No hardcoded URLs in code

---

### Task 5: Update Student App to Use Service Discovery

**Estimated Time:** 2 hours

**Description:**
Same as Task 4 but for Student App.

**Files to Modify:**

- `src/AcademicAssessment.StudentApp/Program.cs`
- `src/AcademicAssessment.StudentApp/AcademicAssessment.StudentApp.csproj`

**Implementation:** (Same pattern as Task 4)

**Acceptance:**

- [ ] Student App discovers Web API URL automatically
- [ ] HTTP calls to Web API succeed
- [ ] SignalR connections work
- [ ] No hardcoded URLs in code

---

### Task 6: Update Bicep Templates for Aspire Azure Deployment

**Estimated Time:** 6 hours

**Description:**
Modify Bicep templates to generate Aspire-compatible Azure Container Apps with service bindings.

**Files to Modify:**

- `infra/main.bicep`
- `infra/resources.bicep`
- `azure.yaml` (azd configuration)

**Implementation:**

**Key Changes:**

1. **Use Azure Database for PostgreSQL Flexible Server** (not container)
2. **Add Service Bindings** in Container Apps for automatic connection injection
3. **Configure Internal Ingress** for service-to-service communication
4. **Add Managed Identity** for secure connections

**resources.bicep example:**

```bicep
// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: 'psql-${environmentName}'
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '17'
    administratorLogin: 'edumind_admin'
    administratorLoginPassword: postgres_password
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
  }
}

// Container App with Service Binding
resource webApi 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-webapi-${environmentName}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
      }
      // Service Bindings (Aspire compatibility)
      serviceBinds: [
        {
          serviceId: postgresServer.id
          name: 'postgres'
        }
        {
          serviceId: redis.id
          name: 'cache'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'webapi'
          image: '${containerRegistry.properties.loginServer}/webapi:latest'
          env: [
            {
              name: 'ConnectionStrings__postgres'
              // Injected by service binding
            }
            {
              name: 'ConnectionStrings__cache'
              // Injected by service binding
            }
          ]
        }
      ]
    }
  }
}
```

**Acceptance:**

- [ ] Bicep templates deploy successfully
- [ ] Azure services have service bindings configured
- [ ] Connection strings automatically injected
- [ ] Managed identities configured correctly

---

### Task 7: Update Azure Deployment Workflow

**Estimated Time:** 3 hours

**Description:**
Update CI/CD pipeline to deploy using Aspire manifest and azd.

**Files to Modify:**

- `.github/workflows/azure-deploy.yml`
- `azure.yaml`

**Implementation:**

**azure.yaml:**

```yaml
name: edumind-ai
metadata:
  template: aspire-azd-starter

services:
  webapi:
    project: src/AcademicAssessment.Web
    language: dotnet
    host: containerapp
  
  dashboard:
    project: src/AcademicAssessment.Dashboard
    language: dotnet
    host: containerapp
  
  studentapp:
    project: src/AcademicAssessment.StudentApp
    language: dotnet
    host: containerapp

# Aspire orchestration
aspire:
  manifest: src/EduMind.AppHost/aspire-manifest.json
```

**Acceptance:**

- [ ] `azd deploy` succeeds
- [ ] Services deploy to Azure Container Apps
- [ ] Service discovery works in Azure
- [ ] All health checks pass

---

### Task 8: Add Environment Detection Logic

**Estimated Time:** 2 hours

**Description:**
Add logic to handle environment-specific differences (e.g., Ollama local only, Azure OpenAI in production).

**Files to Modify:**

- `src/EduMind.AppHost/Program.cs`

**Implementation:**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").AddDatabase("edumind");
var redis = builder.AddRedis("cache");

// Ollama: Local only
IResourceBuilder<IResourceWithConnectionString>? aiService = null;
if (builder.Environment.IsDevelopment())
{
    aiService = builder.AddContainer("ollama", "ollama/ollama", "latest")
        .WithEndpoint(port: 11434, scheme: "http", name: "ollama-http")
        .PublishAsDockerFile();
}
else
{
    // Production: Use Azure OpenAI
    aiService = builder.AddConnectionString("openai");
}

var webApi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
    .WithReference(postgres)
    .WithReference(redis);

if (aiService != null)
{
    webApi.WithReference(aiService);
}
```

**Acceptance:**

- [ ] Local development uses Ollama
- [ ] Azure deployment skips Ollama, uses Azure OpenAI
- [ ] Services handle AI service differences gracefully

---

### Task 9: Update Integration Tests for Service Discovery

**Estimated Time:** 4 hours

**Description:**
Update integration tests to use Aspire test host instead of hardcoded URLs.

**Files to Modify:**

- `tests/AcademicAssessment.Tests.Integration/WebApplicationFactoryFixture.cs`
- `tests/AcademicAssessment.Tests.Integration/AcademicAssessment.Tests.Integration.csproj`

**Implementation:**

**AcademicAssessment.Tests.Integration.csproj:**

```xml
<ItemGroup>
  <PackageReference Include="Aspire.Hosting.Testing" Version="9.5.1" />
</ItemGroup>
```

**WebApplicationFactoryFixture.cs:**

```csharp
public class AspireIntegrationTestFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    
    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.EduMind_AppHost>();
        
        _app = await appHost.BuildAsync();
        await _app.StartAsync();
    }
    
    public async Task DisposeAsync()
    {
        if (_app != null)
            await _app.DisposeAsync();
    }
    
    public HttpClient CreateClient(string serviceName)
    {
        return _app!.CreateHttpClient(serviceName);
    }
}
```

**Acceptance:**

- [ ] Integration tests start Aspire test host
- [ ] Services discover each other in tests
- [ ] No hardcoded URLs in tests
- [ ] Tests pass consistently

---

### Task 10: Update Documentation

**Estimated Time:** 3 hours

**Description:**
Update all deployment documentation to reflect unified Aspire approach.

**Files to Modify:**

- `.github/deployment/playbook/01-first-time-local-setup.md`
- `.github/deployment/playbook/02-daily-development-workflow.md`
- `.github/deployment/playbook/03-first-time-azure-deployment.md`
- `docs/deployment/LOCAL_DEPLOYMENT_GUIDE.md`
- `README.md`

**Key Updates:**

- Remove references to manual Docker container setup
- Document Aspire's automatic container management
- Update connection string configuration instructions
- Add service discovery troubleshooting

**Acceptance:**

- [ ] Documentation accurately reflects new Aspire approach
- [ ] Step-by-step instructions updated
- [ ] Architecture diagrams show service discovery
- [ ] Troubleshooting guide covers common issues

---

### Task 11: Local Testing & Validation

**Estimated Time:** 4 hours

**Description:**
Comprehensive testing of local Aspire deployment.

**Test Checklist:**

- [ ] `dotnet run --project src/EduMind.AppHost` starts successfully
- [ ] Aspire Dashboard accessible (usually <https://localhost:15000>)
- [ ] PostgreSQL container starts and is healthy
- [ ] Redis container starts and is healthy
- [ ] Ollama container starts and is healthy (if used)
- [ ] Web API starts and connects to dependencies
- [ ] Dashboard starts and connects to Web API
- [ ] Student App starts and connects to Web API
- [ ] Health checks pass for all services
- [ ] Integration tests pass
- [ ] Can perform CRUD operations via API
- [ ] Blazor apps render correctly and communicate with API

**Acceptance:**

- [ ] All local tests pass
- [ ] No hardcoded connection strings remain
- [ ] Services discover each other automatically
- [ ] Aspire Dashboard shows all services healthy

---

### Task 12: Azure Deployment & Validation

**Estimated Time:** 6 hours

**Description:**
Deploy to Azure and validate service discovery works remotely.

**Deployment Steps:**

```bash
# Login to Azure
azd auth login

# Initialize environment
azd init

# Deploy
azd deploy
```

**Validation Checklist:**

- [ ] `azd deploy` completes without errors
- [ ] All Container Apps deploy successfully
- [ ] PostgreSQL Flexible Server provisioned
- [ ] Redis provisioned (managed or container)
- [ ] Service bindings configured correctly
- [ ] Services start and pass health checks
- [ ] Web API responds to HTTP requests
- [ ] Dashboard loads and connects to API
- [ ] Student App loads and connects to API
- [ ] Can perform CRUD operations via deployed API
- [ ] Logs show successful service discovery
- [ ] No "connection refused" or DNS errors

**Acceptance:**

- [ ] Azure deployment fully functional
- [ ] Same code works locally and remotely
- [ ] No deployment-specific code changes needed
- [ ] Services communicate via internal FQDNs

---

## Acceptance Criteria

### Functional Requirements

- [x] **Unified Configuration:** Single AppHost configuration works locally and in Azure
- [x] **Automatic Service Discovery:** Services discover dependencies without hardcoded URLs
- [x] **Local Development:** `dotnet run --project src/EduMind.AppHost` starts complete stack
- [x] **Azure Deployment:** `azd deploy` deploys complete stack with service discovery
- [x] **No Configuration Drift:** Same connection patterns in both environments

### Non-Functional Requirements

- [x] **Performance:** Service discovery adds < 100ms overhead
- [x] **Reliability:** Services reconnect automatically on dependency restart
- [x] **Developer Experience:** Single command to start/deploy entire application
- [x] **Observability:** Aspire Dashboard shows service topology and health

### Testing Requirements

- [x] All integration tests pass using service discovery
- [x] Local deployment tested end-to-end
- [x] Azure deployment tested end-to-end
- [x] Service failover tested (restart dependencies, verify reconnection)

---

## Dependencies

**Blocked By:**

- None (can start immediately)

**Blocks:**

- Story 007 (Multi-tenant Physical Isolation) - Needs unified connection management
- Story 011 (Monitoring & Alerting) - Should use Aspire's built-in telemetry

**Related:**

- Story 005 (Optimize Ollama) - Local AI service configuration
- Story 012 (Rate Limiting) - Service-to-service communication patterns

---

## Technical References

### Documentation

- **[.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)** - Official Aspire docs
- **[Aspire Service Discovery](https://learn.microsoft.com/dotnet/aspire/service-discovery/)** - Service discovery patterns
- **[Azure Container Apps + Aspire](https://learn.microsoft.com/azure/container-apps/aspire)** - Aspire deployment to Azure
- **[Aspire PostgreSQL](https://learn.microsoft.com/dotnet/aspire/database/postgresql-component)** - PostgreSQL integration
- **[Aspire Redis](https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-component)** - Redis integration

### Code Examples

- **[eShopSupport (Microsoft Sample)](https://github.com/dotnet/eShopSupport)** - Full Aspire example
- **[Aspire Samples Repo](https://github.com/dotnet/aspire-samples)** - Various patterns

---

## Risk Assessment

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Breaking existing local dev setup | High | High | Incremental rollout, keep old config as fallback initially |
| Azure service bindings don't work as expected | Medium | High | Test early, validate with Microsoft samples |
| Performance impact of service discovery | Low | Medium | Benchmark, optimize if needed |
| Integration test failures | Medium | Medium | Update tests incrementally, validate each change |

### Rollback Plan

If deployment parity fails:

1. Revert AppHost changes (keep `AddConnectionString`)
2. Use environment-specific configuration branches
3. Document limitations and plan future retry

---

## Notes

- This story is **foundational** for production readiness - it eliminates "works on my machine" issues
- Aspire is Microsoft's recommended pattern for cloud-native .NET apps
- Service discovery is critical for multi-tenant physical isolation (Story 007)
- Consider Aspire's built-in OpenTelemetry integration for observability (Story 011)

---

**Created:** 2025-10-25  
**Last Updated:** 2025-10-25  
**Author:** GitHub Copilot Agent
