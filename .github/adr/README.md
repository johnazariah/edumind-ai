# Architecture Decision Records (ADRs)

> **Documenting the architectural decisions that shaped EduMind.AI**

This directory contains Architecture Decision Records (ADRs) - lightweight documentation capturing significant technical and architectural decisions made during the project's development.

## 📖 Quick Start

- **New to ADRs?** Read the [Introduction](introduction.md)
- **Looking for a specific decision?** See [Categories](#by-category) below
- **Want to understand the system?** Start with [Key Decisions](#key-decisions)

## 🗂️ ADR Index

### By Category

#### 🏗️ Technology Stack (001-009)

| ADR | Title | Status | Key Impact |
|-----|-------|--------|------------|
| [001](001-dotnet-9-framework-selection.md) | .NET 9.0 Framework Selection | ✅ Accepted | Foundation framework |
| [002](002-blazor-server-for-student-app.md) | Blazor Server for Student App | ✅ Accepted | Student UI architecture |
| [003](003-semantic-kernel-for-ai-integration.md) | Semantic Kernel for AI Integration | ✅ Accepted | LLM abstraction layer |
| [004](004-ollama-for-local-llm.md) | Ollama for Local LLM Execution | ✅ Accepted | Dev/test LLM provider |
| [005](005-postgresql-as-primary-database.md) | PostgreSQL as Primary Database | ✅ Accepted | Data persistence |
| [006](006-redis-for-caching-layer.md) | Redis for Caching Layer | ✅ Accepted | Distributed caching |
| [007](007-aspire-for-local-orchestration.md) | .NET Aspire for Local Orchestration | ✅ Accepted | Local development |

#### 🔧 Architecture Patterns (010-019)

| ADR | Title | Status | Key Impact |
|-----|-------|--------|------------|
| [010](010-multi-agent-architecture.md) | Multi-Agent Architecture | ✅ Accepted | Assessment generation |
| [011](011-repository-pattern-for-data-access.md) | Repository Pattern for Data Access | ✅ Accepted | Data abstraction |

#### ☁️ Azure Deployment (020-029)

| ADR | Title | Status | Key Impact |
|-----|-------|--------|------------|
| [020](020-azure-container-apps-for-hosting.md) | Azure Container Apps for Hosting | ✅ Accepted | Production platform |
| [021](021-azure-database-postgresql-flexible-server.md) | Azure Database for PostgreSQL | ✅ Accepted | Managed database |
| [022](022-runtime-fqdn-detection.md) | Runtime FQDN Detection | ✅ Accepted | Service discovery |
| [028](028-upgrade-to-dotnet-9-aspire-9.md) | Upgrade to .NET 9 & Aspire 9.5.1 | ✅ Accepted | Version upgrade |

#### 🔐 Security & Authentication (030-039)

| ADR | Title | Status | Key Impact |
|-----|-------|--------|------------|
| [033](033-client-secret-authentication-over-oidc.md) | Client-Secret Authentication | ✅ Accepted | CI/CD auth |

#### 🔄 Development Workflow (040-049)

| ADR | Title | Status | Key Impact |
|-----|-------|--------|------------|
| [040](040-reusable-github-actions-workflows.md) | Reusable GitHub Actions Workflows | ✅ Accepted | CI/CD standardization |

#### ✅ Testing Strategy (050-059)

| ADR | Title | Status | Key Impact |
|-----|-------|--------|------------|
| [050](050-xunit-test-framework.md) | xUnit Test Framework | ✅ Accepted | Testing foundation |

#### 🎨 Frontend (060-069)

| ADR | Title | Status | Key Impact |
|-----|-------|--------|------------|
| [063](063-html-links-vs-blazor-navigation.md) | HTML Links vs Blazor Navigation | ⚠️ Workaround | Navigation reliability |

#### 💾 Data Architecture (070-079)

| ADR | Title | Status | Key Impact |
|-----|-------|--------|------------|
| [070](070-entity-framework-core-for-orm.md) | Entity Framework Core for ORM | ✅ Accepted | ORM choice |

#### 📊 Observability (080-089)

| ADR | Title | Status | Key Impact |
|-----|-------|--------|------------|
| [080](080-application-insights-for-observability.md) | Application Insights | ✅ Accepted | Production monitoring |

## 🌟 Key Decisions

If you only read 5 ADRs, read these:

1. **[ADR-001](001-dotnet-9-framework-selection.md)**: Why .NET 9.0 - Foundation technology choice
2. **[ADR-010](010-multi-agent-architecture.md)**: Multi-Agent Architecture - Core system design
3. **[ADR-020](020-azure-container-apps-for-hosting.md)**: Azure Container Apps - Production platform
4. **[ADR-021](021-azure-database-postgresql-flexible-server.md)**: Managed PostgreSQL - Database strategy
5. **[ADR-022](022-runtime-fqdn-detection.md)**: Runtime FQDN Detection - Solving Azure networking

## 📊 Decision Statistics

- **Total ADRs**: 15
- **Accepted**: 14
- **Workarounds**: 1 (ADR-063)
- **Superseded**: 4 implicit changes
- **Date Range**: October 2025 - Present

## 🎯 Decision Principles

The following principles guided our architectural decisions:

### 1. Privacy First

Student data is sacred. Decisions prioritize:

- Physical database isolation per school
- No client-side exposure of sensitive data
- FERPA/GDPR compliance by design

**Examples**: ADR-002 (Blazor Server), ADR-005 (PostgreSQL), ADR-021 (Database isolation)

### 2. Cloud-Native

Optimize for Azure deployment:

- Managed services over self-hosted
- Auto-scaling container orchestration
- Observability-first architecture

**Examples**: ADR-020 (Container Apps), ADR-021 (Managed PostgreSQL), ADR-080 (Application Insights)

### 3. Developer Productivity

Make development fast and enjoyable:

- Modern frameworks and languages
- One-command local setup
- Fast feedback loops

**Examples**: ADR-001 (.NET 9), ADR-007 (Aspire), ADR-004 (Ollama)

### 4. Pragmatic Over Perfect

Real-world constraints matter:

- Delivery speed over architectural purity
- Workarounds when blockers exist
- Future-ready but not over-engineered

**Examples**: ADR-033 (Client-secret), ADR-063 (HTML links), ADR-022 (Runtime detection)

## 🔄 Superseded Decisions

| Original Decision | Superseded By | Reason |
|-------------------|---------------|--------|
| .NET 8.0 | ADR-028 (.NET 9.0) | Performance and features |
| Containerized PostgreSQL | ADR-021 (Managed PostgreSQL) | Azure Files permissions |
| OIDC Authentication | ADR-033 (Client-secret) | Conditional Access blocking |
| Template Variable Injection | ADR-022 (Runtime detection) | azd template issues |

## 📅 Timeline

```
October 2025
├── Week 1: Core Technology Stack (ADR 001-007)
├── Week 2: Architecture Patterns (ADR 010-011)
├── Week 3: Azure Deployment (ADR 020-022)
├── Week 4: Security & CI/CD (ADR 033, 040)
└── Ongoing: Testing, Observability (ADR 050, 070, 080)
```

## 🔗 Related Documentation

- **[Architecture Summary](../ARCHITECTURE_SUMMARY.md)**: High-level system overview
- **[System Diagram](../SYSTEM_DIAGRAM.md)**: Visual architecture
- **[Deployment Strategy](../../deployment/AZURE_DEPLOYMENT_STRATEGY.md)**: Deployment details
- **[A2A Integration Plan](../A2A_AGENT_INTEGRATION_PLAN.md)**: Agent architecture

## 📝 Contributing

### Creating a New ADR

1. Use the next available number in the appropriate category range
2. Follow the template in [introduction.md](introduction.md)
3. Get review from 2+ team members
4. Update this README with the new entry

### Updating Existing ADRs

- **Don't modify historical decisions** - They document what was decided at the time
- **Create new ADRs to supersede** - If a decision changes, create a new ADR
- **Add cross-references** - Link related ADRs for context

## 📬 Questions?

- **About a specific decision?** Check the ADR and its references (commits, docs)
- **Why was X chosen over Y?** Look for "Alternative Considered" section
- **Can we change decision Z?** Create a new ADR proposing the change
- **General architecture questions?** Start with [Architecture Summary](../ARCHITECTURE_SUMMARY.md)

## 🏷️ Legend

- ✅ **Accepted**: Decision is active and implemented
- ⚠️ **Workaround**: Temporary solution, may be revised
- 🔄 **Proposed**: Under consideration, not yet implemented
- ❌ **Rejected**: Considered but not selected
- 📜 **Superseded**: Replaced by a newer decision

---

**Last Updated**: October 24, 2025  
**Maintained By**: Development Team  
**Total Word Count**: ~50,000 words across all ADRs
