# Environment Strategy for EduMind.AI

**Last Updated:** 2025-10-25  
**Status:** ✅ Implemented

## Overview

EduMind.AI uses a multi-environment strategy to ensure the same code works across development, testing, and production scenarios while maintaining appropriate isolation and resource usage.

---

## Environment Matrix

| Environment | Purpose | Aspire | Database | Cache | Authentication | AI Provider |
|-------------|---------|--------|----------|-------|----------------|-------------|
| **Development** | Local dev | ✅ Containers | PostgreSQL (container) | Redis (container) | JWT (dev keys) | Ollama (local) |
| **Testing** | Integration tests | ❌ Disabled | In-Memory | Memory Cache | JWT (test keys) | Ollama (local) |
| **Staging** | Pre-production | ✅ Azure services | Azure PostgreSQL | Azure Redis | Azure AD B2C | Azure OpenAI |
| **Production** | Live system | ✅ Azure services | Azure PostgreSQL | Azure Redis | Azure AD B2C | Azure OpenAI |

---

## Environment Detection

### ASP.NET Core Environment Variable

The primary environment discriminator is `ASPNETCORE_ENVIRONMENT`:

```csharp
// In Program.cs
if (builder.Environment.IsEnvironment("Testing"))
{
    // Test-specific configuration
}
else if (builder.Environment.IsDevelopment())
{
    // Development configuration
}
else
{
    // Production/Staging configuration
}
```

### Environment Values

- **`Development`**: Local development with Aspire orchestration
- **`Testing`**: Integration test execution (in-memory resources)
- **`Staging`**: Pre-production Azure environment
- **`Production`**: Live production Azure environment

---

## Testing Environment ("Testing")

### Purpose

The "Testing" environment is **specifically for running integration tests**, not for deploying to a cloud environment named "test". It provides:

1. **Fast test execution** - No network calls to real services
2. **Isolation** - Each test gets its own database instance
3. **Deterministic behavior** - No external dependencies
4. **CI/CD compatibility** - Runs without infrastructure

### Characteristics

```csharp
// Integration tests use Testing environment
builder.UseEnvironment("Testing");

// Testing environment skips:
// 1. Aspire service discovery (AddServiceDefaults)
// 2. Aspire database connection (AddNpgsqlDbContext)
// 3. Aspire cache connection (AddRedisClient)
// 4. Production authentication (Microsoft Identity Web)
// 5. Health check endpoints (MapDefaultEndpoints)
```

### Test Resource Configuration

**Database:**

```csharp
// In-memory database per test
services.AddDbContextPool<AcademicContext>(options =>
{
    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
});
```

**Cache:**

```csharp
// Memory cache instead of Redis
services.AddDistributedMemoryCache();
```

**Authentication:**

```csharp
// Test JWT authentication
["Authentication:Enabled"] = "false"
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* test configuration */);
```

### When Tests Run

Integration tests execute in the "Testing" environment:

- ✅ **Local development**: `dotnet test` on developer machine
- ✅ **CI/CD pipelines**: Automated test runs in GitHub Actions
- ✅ **Pull request validation**: PR checks before merge
- ❌ **NOT for cloud deployment**: This is not a cloud environment

---

## Development Environment ("Development")

### Purpose

Local development with full Aspire orchestration, matching production architecture but using containers.

### Configuration

**Aspire AppHost:**

```csharp
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("edumind");

var redis = builder.AddRedis("cache", port: 6379)
    .WithLifetime(ContainerLifetime.Persistent);
```

**Web API Service Discovery:**

```csharp
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddServiceDefaults();
    builder.AddNpgsqlDbContext<AcademicContext>("edumind");
    builder.AddRedisClient("cache");
}
```

### Running Locally

```bash
# Start Aspire orchestration
dotnet run --project src/EduMind.AppHost

# Access services:
# - Aspire Dashboard: https://localhost:17126
# - Web API: https://localhost:7026
# - Dashboard: https://localhost:7156
# - Student App: https://localhost:7073
```

---

## Staging/Production Environments

### Purpose

Azure-hosted environments with managed services.

### Configuration

**Aspire AppHost (Publish Mode):**

```csharp
if (builder.ExecutionContext.IsPublishMode)
{
    // Azure managed services
    var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
        .AddDatabase("edumind");
    
    var redis = builder.AddAzureRedis("cache");
}
else
{
    // Local containers (Development)
    var postgres = builder.AddPostgres("postgres", port: 5432)
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("edumind");
    
    var redis = builder.AddRedis("cache", port: 6379)
        .WithLifetime(ContainerLifetime.Persistent);
}
```

**Authentication:**

```csharp
// Production uses Azure AD B2C
if (authEnabled && !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));
}
```

**AI Provider:**

```csharp
// Environment detection in deployment
var aiProvider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "Ollama";

if (aiProvider == "AzureOpenAI")
{
    // Use Azure OpenAI in production
}
else
{
    // Use Ollama locally
}
```

---

## Cloud Test Environment (Future)

If you need a **cloud-based test environment** (not the "Testing" environment for integration tests), you would:

1. **Create a new environment**: `"QA"` or `"PreProd"`
2. **Deploy to Azure**: `azd env new qa && azd deploy`
3. **Use real Azure services**: Azure PostgreSQL, Redis, OpenAI
4. **Different configuration**: Separate from local/production

This is **separate from** the "Testing" environment which is for running integration tests.

---

## Common Scenarios

### Scenario 1: Run Integration Tests

```bash
# Tests use "Testing" environment automatically
dotnet test tests/AcademicAssessment.Tests.Integration

# Environment: Testing
# Database: In-Memory
# Cache: Memory
# Authentication: Test JWT
```

### Scenario 2: Local Development

```bash
# Start Aspire orchestration
dotnet run --project src/EduMind.AppHost

# Environment: Development
# Database: PostgreSQL container
# Cache: Redis container
# Authentication: JWT (dev keys)
# AI: Ollama (local)
```

### Scenario 3: Deploy to Azure Staging

```bash
# Create staging environment
azd env new staging
azd deploy

# Environment: Staging (ASPNETCORE_ENVIRONMENT=Staging)
# Database: Azure PostgreSQL
# Cache: Azure Redis
# Authentication: Azure AD B2C
# AI: Azure OpenAI
```

### Scenario 4: Deploy to Production

```bash
# Use production environment
azd env select production
azd deploy

# Environment: Production
# Database: Azure PostgreSQL
# Cache: Azure Redis
# Authentication: Azure AD B2C
# AI: Azure OpenAI
```

---

## Key Differences

### Testing vs Development

| Aspect | Testing | Development |
|--------|---------|-------------|
| **Use Case** | Run integration tests | Local development |
| **Aspire** | ❌ Disabled | ✅ Enabled |
| **Database** | In-Memory (EF) | PostgreSQL container |
| **Cache** | Memory | Redis container |
| **Speed** | Very fast | Fast |
| **Isolation** | Per test | Shared |
| **Network** | None | Local network |

### Development vs Production

| Aspect | Development | Production |
|--------|-------------|------------|
| **Use Case** | Local development | Live system |
| **Infrastructure** | Containers | Azure services |
| **Data Persistence** | Local volumes | Azure managed |
| **Authentication** | Test JWT | Azure AD B2C |
| **AI Provider** | Ollama (local) | Azure OpenAI |
| **Monitoring** | Local logs | App Insights |
| **Scaling** | Single instance | Auto-scaling |

---

## Best Practices

### 1. Always Use Correct Environment

```csharp
// ✅ Good: Environment-aware
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddServiceDefaults();
}

// ❌ Bad: Hardcoded
builder.AddServiceDefaults(); // Breaks tests
```

### 2. Make Aspire Features Conditional

```csharp
// ✅ Good: Skip in Testing
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddNpgsqlDbContext<AcademicContext>("edumind");
    builder.AddRedisClient("cache");
} else {
    builder.Services.AddDistributedMemoryCache();
}

// ❌ Bad: Always use Aspire
builder.AddNpgsqlDbContext<AcademicContext>("edumind"); // Breaks tests
```

### 3. Provide Test Alternatives

```csharp
// ✅ Good: Fallback for tests
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddRedisClient("cache");
} else {
    builder.Services.AddDistributedMemoryCache();
}

// ❌ Bad: No test alternative
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddRedisClient("cache");
}
// Tests will fail without cache
```

### 4. Document Environment Requirements

```csharp
/// <summary>
/// Configures database access.
/// - Development: PostgreSQL container via Aspire
/// - Testing: In-memory database (EF Core)
/// - Production: Azure PostgreSQL via Aspire
/// </summary>
```

---

## Troubleshooting

### Problem: Tests fail with "entry point exited without building IHost"

**Cause:** Aspire service discovery running in test environment

**Solution:** Ensure `UseEnvironment("Testing")` in test factory:

```csharp
builder.UseEnvironment("Testing");
```

### Problem: Tests fail with "ConnectionString is missing"

**Cause:** Aspire database registration running in tests

**Solution:** Make `AddNpgsqlDbContext` conditional:

```csharp
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddNpgsqlDbContext<AcademicContext>("edumind");
}
```

### Problem: Tests fail with authentication errors

**Cause:** Production Microsoft Identity authentication running

**Solution:** Check authentication condition:

```csharp
if (authEnabled && !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    // Production auth only
}
```

---

## Summary

- **"Testing"** = Integration test execution environment (in-memory, fast, isolated)
- **"Development"** = Local development with Aspire containers
- **"Staging"** = Pre-production Azure environment
- **"Production"** = Live Azure environment
- **Tests run in "Testing"**, not in a cloud test environment
- All environments use the **same code**, different configuration
- Aspire features are **conditionally disabled** in Testing environment
