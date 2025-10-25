# Architecture Decision Records (ADRs) - EduMind.AI

## Introduction

This directory contains **Architecture Decision Records (ADRs)** documenting significant architectural and technical decisions made during the EduMind.AI project development from October 2025 onwards.

### What is an ADR?

An Architecture Decision Record (ADR) captures an important architectural decision made along with its context and consequences. ADRs help teams:

- **Document decisions**: Create institutional memory
- **Understand context**: Why certain choices were made
- **Learn from history**: Avoid repeating past mistakes
- **Onboard new team members**: Understand the system's evolution
- **Review trade-offs**: Revisit decisions when circumstances change

### ADR Format

Each ADR follows this structure:

- **Status**: ✅ Accepted | ⚠️ Superseded | 🔄 Proposed | ❌ Rejected
- **Date**: When the decision was made
- **Context**: The problem or situation requiring a decision
- **Decision**: What was decided
- **Rationale**: Why this decision was made
- **Consequences**: Positive/negative impacts and trade-offs
- **Alternatives Considered**: Other options and why they were rejected
- **Related Decisions**: Links to other ADRs
- **References**: Code, commits, documentation

## ADR Categories

### Technology Stack Decisions (001-009)

Core framework and platform choices that define the system's foundation.

- **[ADR-001](001-dotnet-9-framework-selection.md)**: .NET 9.0 Framework Selection
- **[ADR-002](002-blazor-server-for-student-app.md)**: Blazor Server for Student App
- **[ADR-003](003-semantic-kernel-for-ai-integration.md)**: Semantic Kernel for AI Integration
- **[ADR-004](004-ollama-for-local-llm.md)**: Ollama for Local LLM Execution
- **[ADR-005](005-postgresql-as-primary-database.md)**: PostgreSQL as Primary Database
- **[ADR-006](006-redis-for-caching-layer.md)**: Redis for Caching Layer
- **[ADR-007](007-aspire-for-local-orchestration.md)**: .NET Aspire for Local Orchestration

### Architecture Pattern Decisions (010-019)

System design patterns and architectural approaches.

- **[ADR-010](010-multi-agent-architecture.md)**: Multi-Agent Architecture for Assessment Generation
- **[ADR-011](011-repository-pattern-for-data-access.md)**: Repository Pattern for Data Access

### Azure Deployment Decisions (020-029)

Cloud deployment and infrastructure choices.

- **[ADR-020](020-azure-container-apps-for-hosting.md)**: Azure Container Apps for Hosting
- **[ADR-021](021-azure-database-postgresql-flexible-server.md)**: Azure Database for PostgreSQL Flexible Server
- **[ADR-022](022-runtime-fqdn-detection.md)**: Runtime FQDN Detection for Connection Strings
- **[ADR-028](028-upgrade-to-dotnet-9-aspire-9.md)**: Upgrade to .NET 9 and Aspire 9.5.1

### Security & Authentication Decisions (030-039)

Security, privacy, and authentication approaches.

- **[ADR-033](033-client-secret-authentication-over-oidc.md)**: Client-Secret Authentication over OIDC

### Development Workflow Decisions (040-049)

CI/CD, development practices, and tooling.

- **[ADR-040](040-reusable-github-actions-workflows.md)**: Reusable GitHub Actions Workflows

### Testing Strategy Decisions (050-059)

Testing frameworks and approaches.

- **[ADR-050](050-xunit-test-framework.md)**: xUnit Test Framework

### Frontend Decisions (060-069)

UI/UX and frontend technology choices.

- **[ADR-063](063-html-links-vs-blazor-navigation.md)**: HTML Links vs Blazor Navigation (Workaround)

### Data Architecture Decisions (070-079)

Database, ORM, and data persistence choices.

- **[ADR-070](070-entity-framework-core-for-orm.md)**: Entity Framework Core for ORM

### Observability Decisions (080-089)

Monitoring, logging, and observability.

- **[ADR-080](080-application-insights-for-observability.md)**: Application Insights for Observability

## Key Decision Themes

### Privacy-First Architecture

Multiple ADRs emphasize student data privacy:

- Physical database isolation per school (ADR-005, ADR-021)
- No client-side assessment data exposure (ADR-002)
- FERPA/GDPR compliance by design

### Cloud-Native Design

Decisions optimized for Azure deployment:

- Container Apps for auto-scaling (ADR-020)
- Managed services over self-hosted (ADR-021, ADR-006)
- OpenTelemetry for observability (ADR-080)
- Runtime configuration over deployment-time (ADR-022)

### Developer Experience

Choices prioritizing productivity:

- Aspire for local development (ADR-007)
- Ollama for cost-free LLM testing (ADR-004)
- Reusable CI/CD workflows (ADR-040)
- Strong typing with C# 13 (ADR-001, ADR-028)

### Pragmatic Over Perfect

Real-world decisions over theoretical purity:

- Client-secret auth when OIDC blocked (ADR-033)
- HTML links when SignalR unreliable (ADR-063)
- Azure Database when containerized PostgreSQL failed (ADR-021)
- Runtime FQDN detection when templates failed (ADR-022)

## Superseded Decisions

Decisions that were later changed:

- **ADR-001**: Partially superseded by ADR-028 (upgraded .NET 8 → .NET 9)
- **Containerized PostgreSQL**: Superseded by ADR-021 (migrated to managed PostgreSQL)
- **OIDC Authentication**: Superseded by ADR-033 (switched to client-secret)
- **Template Variable Injection**: Superseded by ADR-022 (runtime detection)

## Deferred Decisions

Areas where decisions are pending:

- **Multi-Tenant Strategy**: Currently one database per school, may need shared schema for B2C
- **LLM Provider Selection**: Currently Azure OpenAI for production, may add Anthropic/others
- **Frontend Framework**: Currently Blazor Server, may evaluate Blazor WebAssembly for offline support
- **Distributed Agents**: Currently in-process agents, may move to microservices at scale

## Decision-Making Process

### When to Create an ADR

Create an ADR for decisions that:

- Are architecturally significant (affect structure, dependencies, performance)
- Are difficult to reverse (database choice, authentication approach)
- Have long-term consequences (2+ years)
- Were controversial or had multiple viable alternatives
- Required significant research or debate

### When NOT to Create an ADR

Don't create ADRs for:

- Trivial implementation details (variable naming, file organization)
- Technology evaluations that didn't lead to decisions
- Decisions mandated by external factors (client requirements)
- Temporary workarounds (unless architecturally interesting like ADR-063)

## Contributing to ADRs

### Creating a New ADR

1. Copy the template (see below)
2. Assign next available number in appropriate category
3. Fill in all sections with clear, concise content
4. Get review from 2+ team members
5. Merge to main branch

### Updating an Existing ADR

- **Never modify past decisions**: ADRs are historical records
- **To change a decision**: Create new ADR that supersedes the old one
- **To add context**: Add "See also" reference or create supplementary ADR

### ADR Template

```markdown
# ADR-XXX: [Title]

**Status:** [Proposed | Accepted | Superseded | Rejected]  
**Date:** [YYYY-MM-DD]  
**Context:** [Phase/Sprint context]

## Context

[Describe the problem, constraints, and forces at play]

## Decision

[State the decision clearly and concisely]

## Rationale

[Explain why this decision was made]
1. Reason 1
2. Reason 2
...

## Consequences

### Positive
- Benefit 1
- Benefit 2

### Negative
- Trade-off 1
- Trade-off 2

### Risks Mitigated
- How risks were addressed

## Alternative Considered: [Option Name]

**Rejected because:**
- Reason 1
- Reason 2

## Related Decisions

- ADR-XXX: [Title]
- ADR-YYY: [Title]

## References

- File paths
- Commit hashes
- Documentation links
```

## Statistics

- **Total ADRs**: 15+
- **Active Decisions**: 14
- **Superseded Decisions**: 4
- **Deferred Decisions**: 4
- **Date Range**: October 2025 - Present
- **Contributors**: Development Team

## Document Maintenance

This introduction and index are updated:

- When new ADRs are added
- When decisions are superseded
- Quarterly review for accuracy
- Before major milestones

**Last Updated**: October 24, 2025  
**Maintained By**: Development Team  
**Review Schedule**: Quarterly
