# Deployment Documentation

This folder contains all documentation related to EduMind.AI deployment architecture, strategies, and processes.

## 📚 Documentation Index

### Architecture Decision Records (ADRs)

- **[ADR: Reusable Workflows](./ADR-REUSABLE-WORKFLOWS.md)** ⭐ **NEW**
  - Decision to adopt reusable GitHub Actions workflows
  - 4 reusable workflows for standardized CI/CD
  - Benefits: DRY, consistency, security, maintainability
  - Migration plan and usage patterns

### Deployment Strategies

- **[Azure Deployment Strategy](./AZURE_DEPLOYMENT_STRATEGY.md)**
  - Azure infrastructure architecture
  - Service topology and networking
  - Cost optimization strategies
  - Multi-environment setup (dev/staging/prod)

- **[Aspire Analysis](./ASPIRE_ANALYSIS.md)**
  - Should EduMind.AI use .NET Aspire? ✅ **YES**
  - Benefits and migration strategy
  - Comparison with docker-compose approach
  - Implementation phases

- **[Aspire Migration Log](./ASPIRE_MIGRATION_LOG.md)**
  - Complete migration history to .NET Aspire
  - AppHost implementation details
  - Service orchestration configuration
  - Lessons learned and best practices

### Authentication & Security

- **[Authentication Setup](./AUTHENTICATION_SETUP.md)**
  - Azure AD B2C configuration
  - JWT token flow
  - Role-based access control (RBAC)
  - Multi-tenant considerations

### Operations

- **[Demo Environment](./DEMO.md)**
  - Demo environment setup
  - Test data and scenarios
  - Admin credentials and access

- **[Self-Service Onboarding](./SELF_SERVICE_ONBOARDING.md)**
  - School/institution onboarding process
  - Automated provisioning
  - User management

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                       EduMind.AI Platform                        │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
    ┌──────────────────┐            ┌──────────────────┐
    │  CI/CD Pipeline  │            │  Azure Cloud     │
    │  (GitHub Actions)│            │  Infrastructure  │
    └──────────────────┘            └──────────────────┘
              │                               │
              │                               │
    ┌─────────┴─────────┐          ┌─────────┴─────────┐
    │  Reusable         │          │  Aspire AppHost   │
    │  Workflows        │          │  Orchestration    │
    │  - Build          │          │  - PostgreSQL     │
    │  - Code Quality   │          │  - Redis          │
    │  - Security Scan  │          │  - Web API        │
    │  - Integration    │          │  - Dashboard      │
    └───────────────────┘          └───────────────────┘
```

## 🚀 Deployment Models

### 1. Local Development (Aspire)
```bash
cd src/EduMind.AppHost
dotnet run
```
- Aspire orchestrates all services automatically
- Aspire dashboard at http://localhost:15888
- Web API at http://localhost:5103

### 2. CI/CD (GitHub Actions)
```yaml
# Uses reusable workflows
jobs:
  build:
    uses: ./.github/workflows/_reusable-dotnet-build.yml
  
  deploy:
    needs: build
    # Deploy to Azure with azd
```

### 3. Azure Cloud (Azure Developer CLI)
```bash
azd init
azd up --environment dev
```
- Deploys to Azure Container Apps
- Managed PostgreSQL and Redis
- Azure Monitor + Application Insights

## 📊 CI/CD Pipeline Architecture

```
PR/Push
   │
   ├─> Build & Test (Reusable Workflow)
   │   ├─ Setup .NET 9.0.x
   │   ├─ Install Aspire workload
   │   ├─ Restore dependencies
   │   ├─ Build solution
   │   ├─ Run unit tests
   │   └─ Collect coverage
   │
   ├─> Code Quality (Reusable Workflow)
   │   ├─ Check formatting (dotnet format)
   │   ├─ Run analyzers (Roslyn)
   │   └─ Verify no warnings
   │
   ├─> Security Scan (Reusable Workflow)
   │   ├─ CodeQL analysis (C#, JS)
   │   ├─ Dependency vulnerabilities
   │   └─ Secret scanning (TruffleHog, Gitleaks)
   │
   └─> Integration Tests (Reusable Workflow)
       ├─ Start PostgreSQL container
       ├─ Start Redis container
       ├─ Apply EF migrations
       └─ Run integration tests
```

## 🔐 Security & Compliance

### Automated Security Scanning
- **CodeQL:** SAST for C# and JavaScript
- **Dependency Scanning:** NuGet package vulnerabilities
- **Secret Scanning:** Prevents credential leaks
- **Code Formatting:** Enforces style standards

### Compliance Standards
- ✅ SOC 2 Type II ready
- ✅ ISO 27001 compatible
- ✅ GDPR compliant (secret scanning)
- ✅ OWASP Top 10 coverage (CodeQL)

### Access Control
- Federated identity (OIDC) for Azure
- Branch protection rules
- Required status checks
- Code review requirements

## 🔄 Environment Strategy

| Environment | Branch | Purpose | Azure Location |
|-------------|--------|---------|----------------|
| **Development** | `develop` | Active development, frequent deploys | East US |
| **Staging** | `staging` | Pre-production validation | East US |
| **Production** | `main` | Live customer environment | Multi-region |

### Promotion Flow
```
feature/* → develop → staging → main
              │          │        │
           (auto)     (manual)  (manual)
              ▼          ▼        ▼
             Dev     Staging    Prod
```

## 📖 Key Technologies

### Infrastructure
- **Azure Container Apps:** Serverless container hosting
- **Azure Database for PostgreSQL:** Managed database
- **Azure Cache for Redis:** Managed caching
- **Azure Monitor:** Observability and alerting

### CI/CD
- **GitHub Actions:** Automation platform
- **Azure Developer CLI (azd):** Deployment orchestration
- **.NET Aspire:** Local orchestration and cloud provisioning

### Security
- **CodeQL:** Static application security testing (SAST)
- **TruffleHog:** Secret detection
- **Gitleaks:** Secret scanning
- **Dependabot:** Automated dependency updates

## 📝 Quick Start Guides

### For Developers
1. Read [Aspire Migration Log](./ASPIRE_MIGRATION_LOG.md)
2. Review [ADR: Reusable Workflows](./ADR-REUSABLE-WORKFLOWS.md)
3. Check [Authentication Setup](./AUTHENTICATION_SETUP.md) for auth flows

### For DevOps Engineers
1. Review [Azure Deployment Strategy](./AZURE_DEPLOYMENT_STRATEGY.md)
2. Understand reusable workflows in [ADR](./ADR-REUSABLE-WORKFLOWS.md)
3. Check GitHub Actions workflows in `../../.github/workflows/`

### For Security/Compliance
1. Review [ADR: Reusable Workflows](./ADR-REUSABLE-WORKFLOWS.md) - Security section
2. Check GitHub Security tab for scan results
3. Review branch protection rules
4. Audit access control policies

## 🔧 Maintenance

### Updating Build Process
All build changes should be made in:
- `.github/workflows/_reusable-dotnet-build.yml`
- Changes automatically apply to all workflows

### Adding Security Checks
Update `.github/workflows/_reusable-security-scan.yml`

### Updating Infrastructure
Edit `src/EduMind.AppHost/Program.cs` for Aspire orchestration

## 📞 Support

For questions about:
- **Deployment architecture:** Review this README and linked ADRs
- **CI/CD issues:** Check [ADR: Reusable Workflows](./ADR-REUSABLE-WORKFLOWS.md)
- **Azure infrastructure:** See [Azure Deployment Strategy](./AZURE_DEPLOYMENT_STRATEGY.md)
- **Aspire:** Read [Aspire Analysis](./ASPIRE_ANALYSIS.md) and [Migration Log](./ASPIRE_MIGRATION_LOG.md)

## 📅 Document History

| Date | Version | Changes |
|------|---------|---------|
| 2025-10-16 | 1.0 | Initial comprehensive deployment documentation index |
