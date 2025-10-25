# ADR-005: PostgreSQL as Primary Database

**Status:** ✅ Accepted  
**Date:** October 2025  
**Context:** Week 1 - Data Storage Selection

## Context

The system required a relational database supporting:

- Complex queries for analytics (student progress, performance trends)
- ACID transactions for assessment submissions
- Multi-tenant data isolation (one database per school)
- JSON storage for flexible assessment metadata
- Full-text search for question banks
- Cost-effective at scale (50-50K concurrent users)

Options:

- SQL Server (Microsoft ecosystem, expensive)
- PostgreSQL (open-source, feature-rich)
- MySQL (popular, less advanced features)
- Azure Cosmos DB (NoSQL, eventual consistency)
- MongoDB (document DB, schema-less)

## Decision

Selected **PostgreSQL 16** as the primary relational database, deployed as **Azure Database for PostgreSQL Flexible Server** in production.

## Rationale

1. **Open Source**: No licensing costs, large community
2. **Advanced Features**: JSON/JSONB, full-text search, CTEs, window functions
3. **Performance**: Excellent query optimizer, parallel query execution
4. **Azure Native**: First-class Azure support with managed service
5. **Multi-Tenant**: Easy to implement database-per-school isolation
6. **Entity Framework**: Excellent Npgsql provider for EF Core
7. **Cost**: ~$12/month (B1ms) vs $50+/month for SQL Server

## Consequences

### Positive

- Rich querying capabilities for analytics (percentile calculations, window functions)
- JSONB for flexible question metadata without schema migrations
- Full-text search for question bank queries
- Excellent performance for complex joins (student→assessment→response)
- Strong consistency guarantees (ACID transactions)
- Easy backup/restore with Azure managed service

### Negative

- Developers need PostgreSQL expertise (vs SQL Server familiarity)
- Some differences in T-SQL vs PL/pgSQL for stored procedures
- Dev container needs PostgreSQL service
- Connection pooling required for scale (Npgsql handles this)

### Risks Mitigated

- Used Entity Framework Core for database abstraction
- Azure managed service handles backup, HA, security patches
- Connection string FQDN patching for Azure Container Apps (ADR-022)
- Implemented retry policies for transient connection failures

## Implementation Details

**Local Development** (Aspire):

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("edumind");
```

**Production** (Azure Database for PostgreSQL Flexible Server):

```bicep
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: 'psql-${resourceToken}'
  sku: {
    name: 'Standard_B1ms'  // Burstable, cost-effective
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    storage: { storageSizeGB: 32 }
  }
}
```

**Entity Framework Configuration**:

```csharp
services.AddDbContext<AcademicAssessmentDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsqlOptions => npgsqlOptions
            .EnableRetryOnFailure(maxRetryCount: 3)
            .CommandTimeout(30)));
```

## Database Schema Highlights

- **17 tables**: Students, Assessments, Questions, Responses, etc.
- **Foreign keys with cascades**: Maintain referential integrity
- **Indexes**: Composite indexes on (StudentId, Subject), (AssessmentId, QuestionId)
- **JSONB columns**: Question.Metadata, Assessment.AdaptiveSettings
- **Audit fields**: CreatedAt, UpdatedAt on all entities

## Alternative Considered: SQL Server

**Rejected because:**

- Higher licensing costs ($50-200/month for Azure SQL)
- Less advanced JSON support (JSON_VALUE vs JSONB operators)
- Overkill for application needs (no need for SQL Server-specific features)
- PostgreSQL more common in modern cloud-native stacks

## Related Decisions

- ADR-021: Azure Database for PostgreSQL Flexible Server (production deployment)
- ADR-022: Runtime FQDN Detection for Connection Strings
- ADR-070: Entity Framework Core for ORM
- ADR-071: Database Migration Strategy

## References

- `src/AcademicAssessment.Infrastructure/Data/AcademicAssessmentDbContext.cs`
- `infra/resources.bicep` - PostgreSQL Flexible Server definition
- Commit: `252231e` - "Migrate to Azure Database for PostgreSQL"
- Commit: `7015686` - "docs: Add comprehensive Azure deployment strategy"
