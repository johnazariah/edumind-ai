# Story 007: Implement Multi-Tenant Physical Isolation

**Priority:** P1 - Production Quality  
**Status:** Ready for Implementation  
**Effort:** Large (2 weeks)  
**Dependencies:** None


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/11

---

## Problem Statement

EduMind.AI currently implements multi-tenancy using **logical isolation** (tenant query filters in application code). While this works for small-scale deployments, it presents significant risks for enterprise customers:

**Current Architecture (Logical Isolation):**

- Single shared PostgreSQL database
- All tenant data in same tables with `tenant_id` column
- Application-level query filters enforce isolation
- One compromised query = potential cross-tenant data leak

**Enterprise Requirements:**

- **Physical isolation:** Separate databases per tenant
- **Compliance:** FERPA/GDPR require strong data isolation
- **Performance:** Tenant workloads don't impact each other
- **Security:** Database-level isolation prevents cross-tenant access
- **Scalability:** Scale tenants independently

**Business Impact:**

- Cannot close enterprise deals (schools, districts) without physical isolation
- Compliance auditors flag shared database as high-risk
- Large tenants impact small tenants' performance
- Single point of failure affects all tenants

---

## Goals & Success Criteria

### Functional Goals

1. **Database-per-tenant architecture**
   - Each tenant gets dedicated PostgreSQL database
   - Connection string routing based on tenant ID
   - Automatic schema migration for new tenants

2. **Tenant lifecycle management**
   - Provision new tenant (create database + seed data)
   - Pause tenant (disable access, preserve data)
   - Delete tenant (cascade delete after grace period)
   - Migrate tenant between servers (future)

3. **Connection pooling optimization**
   - Separate connection pools per tenant
   - Pool size limits to prevent resource exhaustion
   - Connection monitoring and alerting

### Non-Functional Goals

- **Zero downtime:** Migrate existing tenants without service interruption
- **Performance:** <100ms overhead for tenant routing
- **Security:** No cross-tenant database access possible
- **Observability:** Track tenant-specific metrics (DB size, query latency)

### Success Criteria

- [ ] Each tenant has dedicated database
- [ ] Connection routing works correctly for all tenants
- [ ] New tenant provisioning takes <5 minutes
- [ ] Zero cross-tenant data leaks in security audit
- [ ] Existing tenants migrated successfully
- [ ] Tenant lifecycle operations (provision/pause/delete) functional
- [ ] All tests pass with multi-tenant configuration

---

## Technical Approach

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    API Request                           │
│                (JWT contains tenant_id)                  │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│          Tenant Resolution Middleware                    │
│  - Extract tenant_id from JWT                            │
│  - Set TenantContext for request scope                   │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│       Tenant Connection Resolver                         │
│  - Lookup connection string for tenant_id                │
│  - Get/create connection pool for tenant                 │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│          DbContext Factory                               │
│  - Create DbContext with tenant-specific connection      │
│  - Apply tenant-specific configuration                   │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│   PostgreSQL Databases (One per Tenant)                 │
│   - edumind_tenant_001                                   │
│   - edumind_tenant_002                                   │
│   - edumind_tenant_003                                   │
└─────────────────────────────────────────────────────────┘
```

### Database Naming Convention

```
edumind_tenant_{tenant_id}

Examples:
- edumind_tenant_b2c_default  (B2C shared database)
- edumind_tenant_district_001 (School District #1)
- edumind_tenant_school_045   (Individual School #45)
```

### Tenant Configuration Store

**New Table in Admin Database:**

```sql
CREATE TABLE tenant_configuration (
    tenant_id UUID PRIMARY KEY,
    tenant_name VARCHAR(255) NOT NULL,
    database_name VARCHAR(100) NOT NULL UNIQUE,
    connection_string TEXT NOT NULL, -- Encrypted
    isolation_level VARCHAR(20) NOT NULL DEFAULT 'physical', -- 'physical' or 'logical'
    status VARCHAR(20) NOT NULL DEFAULT 'active', -- 'active', 'paused', 'deleted'
    max_users INT,
    max_storage_gb INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_tenant_config_status ON tenant_configuration(status);
CREATE INDEX idx_tenant_config_database ON tenant_configuration(database_name);
```

---

## Task Decomposition

### Task 1: Create Tenant Context Service

- **Description:** Implement scoped service to hold current tenant information
- **Files to Create:**
  - `src/AcademicAssessment.Infrastructure/MultiTenancy/TenantContext.cs`
  - `src/AcademicAssessment.Infrastructure/MultiTenancy/ITenantContextAccessor.cs`
- **Implementation:**

  ```csharp
  public interface ITenantContextAccessor
  {
      Guid? TenantId { get; }
      string? TenantName { get; }
      void SetTenant(Guid tenantId, string tenantName);
  }
  
  public sealed class TenantContext : ITenantContextAccessor
  {
      private Guid? _tenantId;
      private string? _tenantName;
      
      public Guid? TenantId => _tenantId;
      public string? TenantName => _tenantName;
      
      public void SetTenant(Guid tenantId, string tenantName)
      {
          _tenantId = tenantId;
          _tenantName = tenantName;
      }
  }
  ```

- **Acceptance:** TenantContext service registered as scoped
- **Dependencies:** None

### Task 2: Create Tenant Resolution Middleware

- **Description:** Extract tenant ID from JWT and set TenantContext
- **Files to Create:**
  - `src/AcademicAssessment.Web/Middleware/TenantResolutionMiddleware.cs`
- **Implementation:**

  ```csharp
  public class TenantResolutionMiddleware
  {
      private readonly RequestDelegate _next;
      
      public async Task InvokeAsync(
          HttpContext context,
          ITenantContextAccessor tenantContext)
      {
          // Extract tenant_id claim from JWT
          var tenantIdClaim = context.User.FindFirst("tenant_id");
          if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
          {
              var tenantName = context.User.FindFirst("tenant_name")?.Value ?? "Unknown";
              tenantContext.SetTenant(tenantId, tenantName);
          }
          
          await _next(context);
      }
  }
  ```

- **Acceptance:** Middleware extracts tenant ID from JWT
- **Dependencies:** Task 1

### Task 3: Create Tenant Configuration Repository

- **Description:** Repository to manage tenant configuration (connection strings, etc.)
- **Files to Create:**
  - `src/AcademicAssessment.Core/Entities/TenantConfiguration.cs`
  - `src/AcademicAssessment.Infrastructure/Repositories/TenantConfigurationRepository.cs`
- **Entity:**

  ```csharp
  public sealed record TenantConfiguration
  {
      public required Guid TenantId { get; init; }
      public required string TenantName { get; init; }
      public required string DatabaseName { get; init; }
      public required string ConnectionString { get; init; }
      public required TenantIsolationLevel IsolationLevel { get; init; }
      public required TenantStatus Status { get; init; }
      public int? MaxUsers { get; init; }
      public int? MaxStorageGb { get; init; }
      public DateTime CreatedAt { get; init; }
      public DateTime UpdatedAt { get; init; }
  }
  
  public enum TenantIsolationLevel { Physical, Logical }
  public enum TenantStatus { Active, Paused, Deleted }
  ```

- **Acceptance:** Repository can CRUD tenant configurations
- **Dependencies:** None

### Task 4: Create Tenant Connection Resolver

- **Description:** Resolve database connection string for current tenant
- **Files to Create:**
  - `src/AcademicAssessment.Infrastructure/MultiTenancy/ITenantConnectionResolver.cs`
  - `src/AcademicAssessment.Infrastructure/MultiTenancy/TenantConnectionResolver.cs`
- **Implementation:**

  ```csharp
  public interface ITenantConnectionResolver
  {
      Task<Result<string>> GetConnectionStringAsync(
          Guid tenantId,
          CancellationToken cancellationToken = default);
  }
  
  public sealed class TenantConnectionResolver : ITenantConnectionResolver
  {
      private readonly IMemoryCache _cache;
      private readonly TenantConfigurationRepository _repository;
      
      public async Task<Result<string>> GetConnectionStringAsync(
          Guid tenantId,
          CancellationToken cancellationToken = default)
      {
          // Check cache first
          if (_cache.TryGetValue($"tenant_conn_{tenantId}", out string? cached))
              return Result<string>.Success(cached!);
          
          // Lookup from database
          var config = await _repository.GetByIdAsync(tenantId, cancellationToken);
          if (config is null)
              return Result<string>.Failure($"Tenant {tenantId} not found");
          
          if (config.Status != TenantStatus.Active)
              return Result<string>.Failure($"Tenant {tenantId} is {config.Status}");
          
          // Cache for 5 minutes
          _cache.Set($"tenant_conn_{tenantId}", config.ConnectionString, TimeSpan.FromMinutes(5));
          
          return Result<string>.Success(config.ConnectionString);
      }
  }
  ```

- **Acceptance:** Resolves connection strings with caching
- **Dependencies:** Task 3

### Task 5: Create Multi-Tenant DbContext Factory

- **Description:** Factory to create DbContext with tenant-specific connection
- **Files to Modify:**
  - `src/AcademicAssessment.Infrastructure/Data/ApplicationDbContext.cs`
- **Files to Create:**
  - `src/AcademicAssessment.Infrastructure/Data/MultiTenantDbContextFactory.cs`
- **Implementation:**

  ```csharp
  public interface IDbContextFactory
  {
      Task<Result<ApplicationDbContext>> CreateDbContextAsync(
          CancellationToken cancellationToken = default);
  }
  
  public sealed class MultiTenantDbContextFactory : IDbContextFactory
  {
      private readonly ITenantContextAccessor _tenantContext;
      private readonly ITenantConnectionResolver _connectionResolver;
      
      public async Task<Result<ApplicationDbContext>> CreateDbContextAsync(
          CancellationToken cancellationToken = default)
      {
          if (_tenantContext.TenantId is null)
              return Result<ApplicationDbContext>.Failure("No tenant context");
          
          var connResult = await _connectionResolver.GetConnectionStringAsync(
              _tenantContext.TenantId.Value,
              cancellationToken);
          
          if (connResult.IsFailure)
              return Result<ApplicationDbContext>.Failure(connResult.Error);
          
          var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
          optionsBuilder.UseNpgsql(connResult.Value);
          
          return Result<ApplicationDbContext>.Success(new ApplicationDbContext(optionsBuilder.Options));
      }
  }
  ```

- **Acceptance:** Creates DbContext with correct tenant connection
- **Dependencies:** Tasks 1, 4

### Task 6: Create Tenant Provisioning Service

- **Description:** Service to provision new tenant (create database, run migrations)
- **Files to Create:**
  - `src/AcademicAssessment.Infrastructure/MultiTenancy/ITenantProvisioningService.cs`
  - `src/AcademicAssessment.Infrastructure/MultiTenancy/TenantProvisioningService.cs`
- **Operations:**

  ```csharp
  public interface ITenantProvisioningService
  {
      Task<Result<Guid>> ProvisionTenantAsync(
          string tenantName,
          TenantIsolationLevel isolationLevel,
          CancellationToken cancellationToken = default);
      
      Task<Result> PauseTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
      Task<Result> DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
  }
  ```

- **Provisioning Steps:**
  1. Generate tenant ID and database name
  2. Create PostgreSQL database
  3. Run Entity Framework migrations
  4. Seed default data (admin user, sample assessments)
  5. Save tenant configuration
  6. Return tenant ID
- **Acceptance:** Can provision new tenant end-to-end
- **Dependencies:** Task 5

### Task 7: Update Repository Base Class

- **Description:** Modify repository base to use multi-tenant DbContext factory
- **Files to Modify:**
  - `src/AcademicAssessment.Infrastructure/Repositories/RepositoryBase.cs`
- **Changes:**

  ```csharp
  public abstract class RepositoryBase<TEntity> where TEntity : class
  {
      private readonly IDbContextFactory _dbContextFactory;
      
      protected async Task<Result<ApplicationDbContext>> GetDbContextAsync(
          CancellationToken cancellationToken)
      {
          return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
      }
      
      // All methods now use GetDbContextAsync() instead of constructor-injected DbContext
  }
  ```

- **Acceptance:** All repositories use tenant-aware DbContext
- **Dependencies:** Task 5

### Task 8: Create Tenant Migration Script

- **Description:** Script to migrate existing tenants to physical isolation
- **Files to Create:**
  - `scripts/migrate-to-multi-tenant.sh`
- **Migration Steps:**
  1. Take backup of current database
  2. For each existing tenant (identified by `tenant_id` in data):
     - Create new database
     - Copy tenant data to new database
     - Create tenant configuration entry
  3. Update application configuration
  4. Run smoke tests
  5. Switch over
- **Acceptance:** Migration script documented and tested
- **Dependencies:** Task 6

### Task 9: Update Dependency Injection Configuration

- **Description:** Register all multi-tenant services in DI container
- **Files to Modify:**
  - `src/AcademicAssessment.Infrastructure/DependencyInjection.cs`
  - `src/AcademicAssessment.Web/Program.cs`
- **Registrations:**

  ```csharp
  services.AddScoped<ITenantContextAccessor, TenantContext>();
  services.AddSingleton<ITenantConnectionResolver, TenantConnectionResolver>();
  services.AddScoped<IDbContextFactory, MultiTenantDbContextFactory>();
  services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
  services.AddMemoryCache(); // For connection string caching
  
  app.UseMiddleware<TenantResolutionMiddleware>(); // After authentication
  ```

- **Acceptance:** All services registered and middleware configured
- **Dependencies:** Tasks 1-7

### Task 10: Create Tenant Management API Endpoints

- **Description:** Admin API endpoints for tenant lifecycle operations
- **Files to Create:**
  - `src/AcademicAssessment.Web/Controllers/TenantManagementController.cs`
- **Endpoints:**

  ```csharp
  [ApiController]
  [Route("api/admin/tenants")]
  [Authorize(Roles = "SystemAdmin")]
  public class TenantManagementController : ControllerBase
  {
      [HttpPost]
      public async Task<IActionResult> ProvisionTenant([FromBody] ProvisionTenantRequest request);
      
      [HttpGet("{tenantId}")]
      public async Task<IActionResult> GetTenant(Guid tenantId);
      
      [HttpPost("{tenantId}/pause")]
      public async Task<IActionResult> PauseTenant(Guid tenantId);
      
      [HttpDelete("{tenantId}")]
      public async Task<IActionResult> DeleteTenant(Guid tenantId);
      
      [HttpGet("{tenantId}/metrics")]
      public async Task<IActionResult> GetTenantMetrics(Guid tenantId);
  }
  ```

- **Acceptance:** Endpoints functional with proper authorization
- **Dependencies:** Task 6

### Task 11: Write Integration Tests

- **Description:** Test multi-tenant isolation and connection routing
- **Files to Create:**
  - `tests/AcademicAssessment.Tests.Integration/MultiTenancy/TenantIsolationTests.cs`
  - `tests/AcademicAssessment.Tests.Integration/MultiTenancy/TenantProvisioningTests.cs`
- **Test Cases:**

  ```csharp
  [Fact]
  public async Task TenantA_CannotAccessTenantB_Data()
  {
      // Arrange: Create two tenants with separate data
      // Act: Query as TenantA
      // Assert: TenantB data not returned
  }
  
  [Fact]
  public async Task ProvisionTenant_CreatesDatabase_AndRunsMigrations()
  {
      // Arrange: New tenant request
      // Act: Provision tenant
      // Assert: Database exists, schema correct
  }
  ```

- **Acceptance:** All tests pass
- **Dependencies:** Tasks 1-10

### Task 12: Update Documentation

- **Description:** Document multi-tenant architecture and operations
- **Files to Create:**
  - `docs/architecture/MULTI_TENANT_ARCHITECTURE.md`
  - `docs/operations/TENANT_PROVISIONING_GUIDE.md`
- **Content:**
  - Architecture diagrams
  - Provisioning procedures
  - Migration guide
  - Troubleshooting
- **Acceptance:** Documentation complete and reviewed
- **Dependencies:** Task 11

---

## Acceptance Criteria

### Functional Requirements

- [ ] Each tenant has dedicated PostgreSQL database
- [ ] Tenant ID extracted from JWT and stored in TenantContext
- [ ] Connection string resolved correctly for all tenants
- [ ] New tenant provisioning takes <5 minutes
- [ ] Tenant pause/delete operations functional
- [ ] Admin API endpoints for tenant management

### Security Requirements

- [ ] No cross-tenant data access possible (tested)
- [ ] Connection strings encrypted in configuration store
- [ ] Tenant management APIs require SystemAdmin role
- [ ] Tenant isolation validated by security audit

### Performance Requirements

- [ ] Tenant routing overhead <100ms
- [ ] Connection pooling optimized for multi-tenant workload
- [ ] Cache hit rate >90% for tenant connection lookups

### Testing Requirements

- [ ] Integration tests validate tenant isolation
- [ ] Migration script tested on staging environment
- [ ] Smoke tests pass after migration

---

## Context & References

### Documentation

- [System Specification - Multi-Tenancy](.github/specification/02-system-architecture.md#multi-tenancy)
- [RBAC Architecture](docs/architecture/RBAC_ARCHITECTURE.md)

### External References

- [Multi-Tenant Data Architecture (Microsoft)](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/approaches/data-partitioning)
- [Entity Framework Multi-Tenancy](https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy)

---

## Notes

- **B2C Tenants:** Can share database (logical isolation) for cost efficiency
- **B2B Tenants:** Require physical isolation for compliance
- **Hybrid Approach:** Support both isolation levels in same system
- **Migration Risk:** High - requires careful planning and rollback strategy
- **Cost Impact:** More databases = higher PostgreSQL hosting costs

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
