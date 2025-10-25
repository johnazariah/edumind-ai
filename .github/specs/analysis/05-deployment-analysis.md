# Analysis Story: Deployment Guide

## Objective

Create a comprehensive `deploy.md` that clearly outlines how the CI/CD build should work and what it needs to deploy, including both local development and Azure production deployments.

## Context

The EduMind.AI project has complex deployment requirements spanning:

- Local development with Aspire
- Azure Container Apps production deployment
- Multiple interconnected services
- Database migrations
- Configuration management
- Infrastructure as Code with Bicep

Deployment knowledge is scattered across multiple docs and workflow files.

## Task Instructions

### 1. Review Existing Deployment Documentation

Analyze all deployment docs:

- `docs/deployment/AZURE_DEPLOYMENT_STRATEGY.md`
- `docs/deployment/AZURE_DEPLOYMENT_STEPS.md`
- `docs/deployment/LOCAL_DEPLOYMENT_GUIDE.md`
- `docs/deployment/DEPLOYMENT_STATUS_*.md`
- `docs/deployment/FRESH_AUSTRALIA_EAST_DEPLOYMENT.md`
- `docs/deployment/ASPIRE_ANALYSIS.md`
- `docs/deployment/ASPIRE_MIGRATION_LOG.md`

### 2. Analyze CI/CD Workflows

Review GitHub Actions:

```bash
ls -la .github/workflows/
cat .github/workflows/*.yml
```

Extract:

- Trigger conditions
- Build steps
- Test execution
- Deployment steps
- Environment variables needed
- Secrets required
- Azure CLI commands

### 3. Review Infrastructure as Code

Analyze Bicep templates:

- `infra/main.bicep` - Primary infrastructure
- `infra/resources.bicep` - Resource definitions
- `src/infra/**/*.bicep` - Aspire-generated infrastructure
- Parameter files and their usage

Document:

- Resources deployed
- Dependencies between resources
- Configuration parameters
- Output values used

### 4. Analyze Azure Developer CLI Configuration

Review:

- `azure.yaml` - azd configuration
- `.azure/` directory contents (if exists)
- Environment-specific configurations

### 5. Extract Container Registry Strategy

Analyze:

- Dockerfile locations
- Container build process
- Registry authentication
- Image tagging strategy
- Multi-architecture builds (if any)

### 6. Review Database Migration Strategy

Check:

- Entity Framework migrations
- Seed data scripts in `scripts/`
- Database initialization process
- Migration execution in deployment

### 7. Document Configuration Management

Analyze:

- Environment variables
- Azure Key Vault usage
- Connection string management
- Secrets handling
- Feature flags

### 8. Extract Service Dependencies

Map service dependencies:

```bash
# Check Aspire AppHost for service dependencies
cat src/EduMind.AppHost/Program.cs

# Check docker-compose if used
cat docker-compose.yml 2>/dev/null || echo "No docker-compose"
```

### 9. Review Health Checks and Readiness

Analyze:

- Health check endpoints
- Startup probes
- Liveness probes
- Readiness probes
- Health check configuration

### 10. Extract Troubleshooting Procedures

Review:

- `docs/deployment/TROUBLESHOOTING.md`
- Known deployment issues
- Resolution procedures
- Diagnostic commands

### 11. Review Rollback Procedures

Check if documented:

- How to roll back deployments
- Database rollback procedures
- Configuration rollback
- Emergency procedures

## Expected Output Structure

Create `.github/deploy.md` with these sections:

### 1. Overview

- Deployment architecture summary
- Supported deployment targets (local, Azure)
- Prerequisites overview
- Key concepts

### 2. Prerequisites

#### 2.1 Local Development Prerequisites

- .NET SDK version
- Docker Desktop
- Azure CLI (if needed)
- azd (Azure Developer CLI)
- Database tools
- Required VS Code extensions

#### 2.2 Azure Deployment Prerequisites

- Azure subscription requirements
- Required Azure CLI version
- azd installation
- Service principal or managed identity setup
- Required permissions/roles
- Network requirements

#### 2.3 CI/CD Prerequisites

- GitHub secrets needed
- Service connections
- Azure credentials configuration
- Container registry access

### 3. Local Development Deployment

#### 3.1 First-Time Setup

Step-by-step for new developers:

```bash
# Exact commands with explanations
```

#### 3.2 Running with Aspire

- Starting the AppHost
- Service discovery
- Configuration
- Accessing services
- Troubleshooting common issues

#### 3.3 Database Setup

- Running migrations
- Seeding data
- Connecting to local PostgreSQL
- Redis configuration

#### 3.4 Local Testing

- Health checks
- API testing
- UI testing
- Integration testing

### 4. Azure Production Deployment

#### 4.1 Infrastructure Deployment

```bash
# azd commands
azd auth login
azd env new [environment-name]
azd up
```

Resources deployed:

- Azure Container Apps Environment
- PostgreSQL Flexible Server
- Redis Cache
- Container Registry
- Application Insights
- Key Vault
- Storage (if used)

#### 4.2 Application Deployment

- Container build process
- Image push to registry
- Container Apps deployment
- Environment variables
- Secrets injection

#### 4.3 Database Migration in Azure

- How migrations are executed
- Seed data deployment
- Verification steps

#### 4.4 Post-Deployment Verification

- Health check endpoints
- Smoke tests
- Log verification
- Metrics validation

### 5. CI/CD Pipeline

#### 5.1 Pipeline Architecture

- Trigger conditions
- Branch strategy
- Environment promotion
- Approval gates (if any)

#### 5.2 Build Stage

- Source checkout
- Dependency restoration
- Compilation
- Unit test execution
- Code coverage
- Static analysis

#### 5.3 Test Stage

- Integration tests
- E2E tests
- Performance tests
- Test result publishing

#### 5.4 Package Stage

- Container image builds
- Image scanning
- Artifact generation
- Version tagging

#### 5.5 Deploy Stage

- Infrastructure deployment (if changed)
- Application deployment
- Database migrations
- Configuration updates
- Health checks
- Smoke tests

#### 5.6 Required Secrets and Variables

Complete list with descriptions:

```yaml
# GitHub Secrets
AZURE_CLIENT_ID: "Description"
AZURE_TENANT_ID: "Description"
AZURE_SUBSCRIPTION_ID: "Description"
AZURE_CREDENTIALS: "Description"
# etc.
```

### 6. Configuration Management

#### 6.1 Environment Variables

Complete reference of all environment variables:

- Variable name
- Purpose
- Example value
- Required/optional
- Environment-specific differences

#### 6.2 Secrets Management

- Which secrets exist
- How they're stored (Key Vault, GitHub Secrets)
- Rotation procedures
- Access policies

#### 6.3 Connection Strings

- Format and structure
- How they're generated
- FQDN detection patterns
- Runtime patching (if applicable)

### 7. Service Dependencies

#### 7.1 Dependency Graph

Visual or textual representation of service dependencies

#### 7.2 Startup Order

- Required startup sequence
- Health check dependencies
- Retry policies

#### 7.3 External Dependencies

- Ollama integration
- Any third-party APIs
- Azure services

### 8. Monitoring and Observability

#### 8.1 Application Insights

- What's logged
- Metrics collected
- Custom events
- Querying logs

#### 8.2 Health Endpoints

- List of all health endpoints
- What each checks
- Expected responses

#### 8.3 Alerts and Notifications

- Configured alerts
- Notification channels
- Response procedures

### 9. Troubleshooting

#### 9.1 Common Issues

For each common issue:

- Symptoms
- Diagnosis steps
- Resolution
- Prevention

Key issues to document:

- Template substitution problems
- FQDN detection failures
- Health check failures
- Database connection issues
- Container startup failures
- Blazor SignalR issues

#### 9.2 Diagnostic Commands

```bash
# Azure CLI commands for troubleshooting
az containerapp logs show ...
az postgres flexible-server show ...
```

#### 9.3 Log Analysis

- Where to find logs
- How to query Application Insights
- Log levels and filtering
- Common error patterns

### 10. Rollback Procedures

#### 10.1 Application Rollback

- How to roll back container apps
- Image version management
- Traffic splitting (if supported)

#### 10.2 Database Rollback

- Migration rollback procedures
- Data backup and restore
- Point-in-time recovery

#### 10.3 Configuration Rollback

- Reverting configuration changes
- Key Vault secret versions
- Environment variable changes

### 11. Security Considerations

#### 11.1 Network Security

- VNet integration
- Private endpoints
- Firewall rules
- NSG configurations

#### 11.2 Identity and Access

- Managed identities
- Service principals
- RBAC assignments
- Key Vault access policies

#### 11.3 Data Security

- Encryption at rest
- Encryption in transit
- Secret management
- Certificate management

### 12. Performance and Scaling

#### 12.1 Scaling Configuration

- Container Apps scaling rules
- Database scaling
- Redis cache sizing

#### 12.2 Performance Monitoring

- Key metrics to watch
- Performance baselines
- Bottleneck identification

### 13. Cost Management

#### 13.1 Resource Costs

- Estimated monthly costs per environment
- Cost optimization opportunities
- Budget alerts

#### 13.2 Cost Monitoring

- Azure Cost Management
- Resource tagging strategy
- Cost allocation

### 14. Disaster Recovery

#### 14.1 Backup Strategy

- What's backed up
- Backup frequency
- Retention policies

#### 14.2 Recovery Procedures

- RTO (Recovery Time Objective)
- RPO (Recovery Point Objective)
- Step-by-step recovery process

### 15. Compliance and Governance

#### 15.1 Compliance Requirements

- Data residency
- Audit logging
- Compliance certifications

#### 15.2 Change Management

- Change approval process
- Deployment windows
- Communication procedures

## Success Criteria

- Complete deployment procedures for all environments
- CI/CD pipeline is fully documented
- All required secrets and variables are listed
- Troubleshooting procedures are clear and actionable
- Rollback procedures are documented
- Security considerations are addressed
- Practical for both developers and ops teams
- Can be followed by someone new to the project
- 75-100 pages equivalent of comprehensive deployment guide

## Notes

- Focus on CURRENT working procedures, not aspirational
- Include actual commands that work, not examples
- Note any manual steps that should be automated
- Document workarounds clearly
- Include timings/durations where helpful
- Reference specific commits where deployment changes were made
- Be explicit about Azure resource names and regions
- Include cost considerations
- Document both happy path and error scenarios
