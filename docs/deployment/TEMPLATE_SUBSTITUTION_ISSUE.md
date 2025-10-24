# PostgreSQL Connection String Template Substitution Issue

**Date:** 2025-10-20  
**Environment:** rg-staging (Australia East)  
**Status:** ROOT CAUSE IDENTIFIED

## Problem

The webapi container is connecting to PostgreSQL using the short hostname `postgres` instead of the Azure Database FQDN `psql-c6fvx6uzvxmv6.postgres.database.azure.com`.

## Evidence

From webapi logs:

```
PostgreSQL Host: postgres
PostgreSQL Database: edumind
```

Expected:

```
PostgreSQL Host: psql-c6fvx6uzvxmv6.postgres.database.azure.com
PostgreSQL Database: edumind
```

## Root Cause

The `src/infra/webapi.tmpl.yaml` template contains:

```yaml
secrets:
  - name: connectionstrings--edumind
    value: Host={{ .Env.POSTGRES_HOST }};Port=5432;Username={{ .Env.POSTGRES_USERNAME }};Password={{ securedParameter "postgres_password" }};Database={{ .Env.POSTGRES_DATABASE }};SslMode=Require
```

However, the template variables `{{ .Env.POSTGRES_HOST }}`, `{{ .Env.POSTGRES_USERNAME }}`, and `{{ .Env.POSTGRES_DATABASE }}` **are NOT being substituted** during `azd deploy`.

## Verification

Bicep outputs ARE correct:

```bash
POSTGRES_HOST="psql-c6fvx6uzvxmv6.postgres.database.azure.com"
POSTGRES_DATABASE="edumind"
POSTGRES_USERNAME="edumind_admin"
```

But the webapi secret contains `Host=postgres` (unsubstituted).

## Hypothesis

1. **azd template processing timing**: The outputs from Bicep may not be available when azd processes the container app templates
2. **Template variable scope**: The `.Env.POSTGRES_HOST` variables might not be in scope during template processing
3. **azd deployment order**: Container apps might be deployed before the PostgreSQL Flexible Server outputs are available

## Similar Issue with Redis

Redis patching works because we do it at **runtime** in Program.cs, not via template substitution:

```
Redis Host: cache.internal.kindplant-6461f562.australiaeast.azurecontainerapps.io:6379
```

## Recommendation

**Stop fighting with azd and test locally first**:

1. Deploy infrastructure manually (Bicep only)
2. Test local connection to Azure Database for PostgreSQL
3. Run webapi locally pointing to Azure resources
4. Verify all components can actually communicate
5. THEN attempt automated deployment

This will confirm whether:

- The issue is azd template processing (deployment problem)
- OR the architecture/configuration itself (fundamental problem)

After 4+ hours of deployment troubleshooting, we need to validate the fundamentals work before debugging deployment tooling.
