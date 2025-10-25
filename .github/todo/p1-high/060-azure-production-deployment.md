# TODO-060: Azure Production Deployment

**Priority:** P1 - High  
**Area:** Infrastructure / Deployment  
**Estimated Effort:** Large (10-15 hours)  
**Status:** Not Started

## Description

Complete end-to-end Azure production deployment of EduMind.AI including all services, database migration, monitoring, and production readiness checks.

## Context

The application has been deployed to Azure successfully during development, but needs a production-ready deployment with:

- Proper environment configuration
- Database migration and seeding
- Monitoring and alerting
- Security hardening
- Performance optimization
- Disaster recovery
- Cost optimization

This is planned for **Weeks 5-6** in the roadmap.

## Technical Requirements

### Infrastructure (Bicep)

Current infrastructure in `infra/main.bicep`:
- ✅ Azure Container Apps Environment
- ✅ 7 Container Apps (webapi, studentapp, 5 admin apps)
- ✅ Azure Database for PostgreSQL Flexible Server
- ✅ Azure Cache for Redis
- ✅ Application Insights
- ✅ Log Analytics Workspace
- ✅ Managed Identity for service authentication

**Additional Infrastructure Needed:**
- [ ] Azure Key Vault for secrets
- [ ] Azure CDN for static assets
- [ ] Azure Storage for file uploads
- [ ] Azure Service Bus for async messaging (optional)
- [ ] Azure Front Door for WAF and global distribution

### Database Migration

```bash
# Production database setup
az postgres flexible-server create \
  --resource-group rg-edumind-prod \
  --name psql-edumind-prod \
  --location australiaeast \
  --admin-user edumind_admin \
  --admin-password <from-keyvault> \
  --sku-name Standard_D2ds_v4 \
  --tier GeneralPurpose \
  --version 16 \
  --storage-size 128 \
  --backup-retention 30 \
  --high-availability Enabled

# Run migrations
dotnet ef database update --project src/AcademicAssessment.Infrastructure \
  --connection "Host=psql-edumind-prod.postgres.database.azure.com;..."

# Seed production data
psql -h psql-edumind-prod.postgres.database.azure.com \
  -U edumind_admin -d edumind_db \
  -f scripts/seed-production-data.sql
```

### Configuration

**Production appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "AcademicDatabase": "@Microsoft.KeyVault(SecretUri=https://kv-edumind-prod.vault.azure.net/secrets/DatabaseConnectionString/)",
    "Redis": "@Microsoft.KeyVault(SecretUri=https://kv-edumind-prod.vault.azure.net/secrets/RedisConnectionString/)"
  },
  "AzureAdB2C": {
    "Instance": "https://edumindai.b2clogin.com/",
    "Domain": "edumindai.onmicrosoft.com",
    "TenantId": "<tenant-id>",
    "ClientId": "@Microsoft.KeyVault(SecretUri=https://kv-edumind-prod.vault.azure.net/secrets/B2CClientId/)",
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://kv-edumind-prod.vault.azure.net/secrets/B2CClientSecret/)",
    "SignUpSignInPolicyId": "B2C_1_susi_google"
  },
  "LLM": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://edumind-openai-prod.openai.azure.com/",
    "ApiKey": "@Microsoft.KeyVault(SecretUri=https://kv-edumind-prod.vault.azure.net/secrets/OpenAIApiKey/)",
    "ModelName": "gpt-4o",
    "Timeout": 60
  },
  "ApplicationInsights": {
    "ConnectionString": "@Microsoft.KeyVault(SecretUri=https://kv-edumind-prod.vault.azure.net/secrets/AppInsightsConnectionString/)"
  }
}
```

### Scaling Configuration

**Container Apps Scaling Rules:**
```bicep
properties: {
  template: {
    scale: {
      minReplicas: 2  // High availability
      maxReplicas: 50
      rules: [
        {
          name: 'http-requests'
          http: {
            metadata: {
              concurrentRequests: '100'
            }
          }
        }
        {
          name: 'cpu-utilization'
          custom: {
            type: 'cpu'
            metadata: {
              type: 'Utilization'
              value: '70'
            }
          }
        }
      ]
    }
  }
}
```

### Monitoring and Alerting

**Azure Monitor Alerts:**
```bicep
resource highErrorRateAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'high-error-rate'
  location: 'global'
  properties: {
    description: 'Alert when error rate exceeds 5%'
    severity: 1
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'ErrorRate'
          metricName: 'requests/failed'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Average'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}
```

**Dashboards:**
- Application performance dashboard
- User activity dashboard
- Infrastructure health dashboard
- Cost management dashboard

## Acceptance Criteria

### Infrastructure
- [ ] Production resource group created
- [ ] All Azure resources deployed via Bicep
- [ ] Key Vault provisioned with all secrets
- [ ] Managed Identity configured for all services
- [ ] Network security rules applied
- [ ] HTTPS enforced on all endpoints
- [ ] Custom domain configured (edumind.ai)
- [ ] SSL certificate provisioned

### Database
- [ ] Production database created with HA
- [ ] Database migrations applied
- [ ] Production data seeded
- [ ] Backup policy configured (30-day retention)
- [ ] Point-in-time restore tested
- [ ] Connection encryption verified
- [ ] Firewall rules configured

### Application
- [ ] All container apps deployed
- [ ] Environment variables configured
- [ ] Secrets loaded from Key Vault
- [ ] Health checks passing
- [ ] Log streaming configured
- [ ] Application Insights integrated
- [ ] Distributed tracing working

### Security
- [ ] All secrets in Key Vault (no hardcoded secrets)
- [ ] Managed Identity used for service auth
- [ ] Network isolation configured
- [ ] DDoS protection enabled
- [ ] WAF rules configured (if using Front Door)
- [ ] Security headers configured (HSTS, CSP, etc.)
- [ ] Vulnerability scanning passed

### Performance
- [ ] Load testing completed (500 concurrent users)
- [ ] Response times <2s for 95th percentile
- [ ] Caching configured and working
- [ ] CDN configured for static assets
- [ ] Database indexes optimized
- [ ] Connection pooling configured

### Monitoring
- [ ] Application Insights dashboards created
- [ ] Alerts configured for critical metrics
- [ ] Log Analytics queries documented
- [ ] Action group for alert notifications
- [ ] On-call rotation configured
- [ ] Runbooks for common issues

### Disaster Recovery
- [ ] Backup and restore tested
- [ ] Failover procedures documented
- [ ] RTO/RPO defined and achievable
- [ ] DR drill completed successfully

### Cost Optimization
- [ ] Autoscaling configured
- [ ] Reserved instances evaluated
- [ ] Cost alerts configured
- [ ] Unused resources identified and removed
- [ ] Cost tags applied to all resources

## Dependencies

- Completed application development (Weeks 1-4)
- Azure subscription with appropriate quota
- Production domain name (edumind.ai)
- Azure DevOps or GitHub Actions for CI/CD
- SSL certificate

## References

- **Files:**
  - `infra/main.bicep`
  - `infra/resources.bicep`
  - `.github/workflows/azure-deploy.yml`
  
- **Documentation:**
  - `docs/deployment/AZURE_DEPLOYMENT_STRATEGY.md`
  - `docs/deployment/AZURE_DEPLOYMENT_STEPS.md`
  - `.github/adr/020-azure-container-apps-for-hosting.md`
  - `.github/adr/021-azure-database-postgresql-flexible-server.md`
  - `docs/planning/ROADMAP.md` (Weeks 5-6)

- **Related TODOs:**
  - TODO-061: Set Up CI/CD Pipeline
  - TODO-062: Configure Azure AD B2C for Production
  - TODO-063: Performance Testing and Optimization

## Implementation Notes

1. **Staged Rollout:** Deploy to staging environment first
2. **Blue-Green Deployment:** Use deployment slots for zero-downtime updates
3. **Feature Flags:** Use feature flags to control rollout
4. **Canary Releases:** Gradually roll out to percentage of users
5. **Monitoring:** Watch metrics closely during and after deployment
6. **Rollback Plan:** Have rollback procedure ready
7. **Communication:** Notify stakeholders of deployment schedule

## Testing Strategy

**Pre-Production:**
- [ ] Deploy to staging environment
- [ ] Run full regression test suite
- [ ] Load testing (simulate production load)
- [ ] Security penetration testing
- [ ] Disaster recovery drill
- [ ] User acceptance testing

**Production Validation:**
- [ ] Smoke tests after deployment
- [ ] Monitor Application Insights for errors
- [ ] Verify health checks
- [ ] Test user workflows end-to-end
- [ ] Check performance metrics
- [ ] Verify monitoring and alerts working

**Post-Deployment:**
- [ ] Monitor for 24 hours
- [ ] Review logs for errors
- [ ] Check cost dashboard
- [ ] Gather user feedback
- [ ] Document lessons learned

## Deployment Checklist

**Pre-Deployment:**
- [ ] Code freeze (no new features)
- [ ] All tests passing
- [ ] Documentation updated
- [ ] Stakeholders notified
- [ ] Deployment window scheduled
- [ ] Rollback plan reviewed

**During Deployment:**
- [ ] Put maintenance page up (optional)
- [ ] Deploy infrastructure (Bicep)
- [ ] Deploy applications (Container Apps)
- [ ] Run database migrations
- [ ] Seed production data
- [ ] Verify health checks
- [ ] Smoke tests passing
- [ ] Remove maintenance page

**Post-Deployment:**
- [ ] Monitor metrics for 1 hour
- [ ] Verify user can complete workflows
- [ ] Check error logs
- [ ] Confirm alerts working
- [ ] Update status page
- [ ] Send deployment summary

## Rollback Procedure

If critical issues occur:

1. **Immediate:** Roll back to previous container image
2. **Database:** Restore from backup if needed (last resort)
3. **Monitoring:** Watch for error rate to return to normal
4. **Communication:** Notify stakeholders
5. **Post-Mortem:** Document what went wrong
6. **Fix:** Address issues before next deployment attempt
