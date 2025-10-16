# EduMind.AI Documentation

**Comprehensive documentation for the Academic Assessment Multi-Agent System**

## 🎯 Quick Start

### For Developers

1. **Set up your environment**: See [Development Setup](development/setup/DEVCONTAINER_SETUP.md)
2. **Review the architecture**: Start with [Architecture Summary](architecture/ARCHITECTURE_SUMMARY.md)
3. **Follow the workflow**: Read [Development Workflow](instructions/DEVELOPMENT_WORKFLOW.md)
4. **Check current status**: See [Task Journal](planning/TASK_JOURNAL.md)

### For Contributors

- **Workflow Guidelines**: [Development Workflow](instructions/DEVELOPMENT_WORKFLOW.md)
- **Coding Standards**: [Copilot Instructions](instructions/copilot-instructions.md)
- **Testing Requirements**: [Testing Strategy](development/testing/TESTING_STRATEGY.md)
- **Sprint Progress**: [Current Sprint](planning/SPRINT_ROADMAP.md)

### Current Sprint Status

**Week 1, Day 4 Complete** ✅  
**Current Branch**: `main`  
**Latest Milestone**: Multi-agent orchestration with decision-making improvements

See [Task Journal](planning/TASK_JOURNAL.md) for detailed progress tracking.

---

## 📚 Documentation Structure

### 📋 [Instructions](instructions/)
*Guidelines for development and working with GitHub Copilot*

- [Copilot Instructions](instructions/copilot-instructions.md) - AI assistance guidelines and system context
- [Development Workflow](instructions/DEVELOPMENT_WORKFLOW.md) - Branching, commits, PRs, testing, documentation

### 🏗️ [Architecture](architecture/)
*System design, patterns, and technical specifications*

- [Architecture Summary](architecture/ARCHITECTURE_SUMMARY.md) - Complete technical architecture
- [Solution Structure](architecture/SOLUTION_STRUCTURE.md) - Codebase organization
- [RBAC Architecture](architecture/RBAC_ARCHITECTURE.md) - Authorization and access control
- [Privacy & Security](architecture/PRIVACY_AND_SECURITY.md) - Security architecture and compliance
- [Privacy Executive Summary](architecture/PRIVACY_EXECUTIVE_SUMMARY.md) - Privacy overview for stakeholders
- [Content Metadata Strategy](architecture/CONTENT_METADATA_STRATEGY.md) - Content tagging and curriculum alignment
- [Observability Strategy](architecture/OBSERVABILITY_STRATEGY.md) - Monitoring and telemetry
- [System Diagram](architecture/SYSTEM_DIAGRAM.md) - Visual architecture overview
- [A2A Agent Integration Plan](architecture/A2A_AGENT_INTEGRATION_PLAN.md) - Agent communication patterns

### 📅 [Planning](planning/)
*Sprint planning, roadmaps, and project management*

- [Task Journal](planning/TASK_JOURNAL.md) - **PRIMARY** development log and status tracker
- [Sprint Roadmap](planning/SPRINT_ROADMAP.md) - Multi-sprint implementation plan
- [Sprint Executive Summary](planning/SPRINT_EXECUTIVE_SUMMARY.md) - High-level progress overview
- [CI/CD Deployment Status](planning/CI_CD_DEPLOYMENT_STATUS.md) - Pipeline and deployment tracking
- [Gap Analysis](planning/GAP_ANALYSIS.md) - Feature gaps and competitive analysis
- [Competitor System Specification](planning/COMPETITOR_SYSTEM_SPECIFICATION.md) - Market analysis
- [B2B vs B2C Comparison](planning/B2B_VS_B2C_COMPARISON.md) - Business model comparison
- **Sprint Summaries**: [Week 1](planning/sprints/week1/) - Daily progress reports

### 💻 [Development](development/)
*Setup guides, testing strategies, and integration documentation*

#### [Setup Guides](development/setup/)
- [Dev Container Setup](development/setup/DEVCONTAINER_SETUP.md) - Local development environment
- [Project Setup Guide](development/setup/PROJECT_SETUP_GUIDE.md) - Complete project initialization
- [GitHub Setup](development/setup/GITHUB_SETUP.md) - Repository configuration
- [GitHub CLI Quickstart](development/setup/GITHUB_CLI_QUICKSTART.md) - CLI tools and commands

#### [Testing](development/testing/)
- [Testing Strategy](development/testing/TESTING_STRATEGY.md) - Comprehensive testing approach
- [API Testing Guide](development/testing/API_TESTING_GUIDE.md) - How to test REST endpoints
- [JWT Authentication Testing](development/testing/JWT_AUTHENTICATION_TESTING.md) - Auth testing guide
- [Aspire Testing Guide](development/testing/ASPIRE_TESTING_GUIDE.md) - Testing with .NET Aspire

#### [Integrations](development/integrations/)
- [OLLAMA Evaluation](development/integrations/OLLAMA_EVALUATION.md) - Local LLM evaluation
- [OLLAMA Integration Complete](development/integrations/OLLAMA_INTEGRATION_COMPLETE.md) - Implementation summary

#### [Known Issues](development/KNOWN_ISSUES.md)
Current bugs and limitations

### 🚀 [Deployment](deployment/)
*Deployment guides, authentication, and production configuration*

- [Azure Deployment Strategy](deployment/AZURE_DEPLOYMENT_STRATEGY.md) - Cloud infrastructure and deployment
- [Authentication Setup](deployment/AUTHENTICATION_SETUP.md) - **COMPREHENSIVE** auth guide (Azure AD, JWT, database, Google OAuth)
- [Aspire Migration Log](deployment/ASPIRE_MIGRATION_LOG.md) - .NET Aspire implementation journey
- [Aspire Analysis](deployment/ASPIRE_ANALYSIS.md) - Aspire benefits and considerations
- [Self-Service Onboarding](deployment/SELF_SERVICE_ONBOARDING.md) - B2C user onboarding
- [Demo Setup](deployment/DEMO.md) - End-to-end demonstration guide

### 📦 [Archive](archive/historical/)
*Historical documents, completed milestones, and legacy documentation*

Completed implementation summaries, old test results, and superseded documentation.

---

## 🔍 Finding Information

### By Task

| What you need | Where to look |
|---------------|---------------|
| **Current work status** | [Task Journal](planning/TASK_JOURNAL.md) |
| **How to contribute** | [Development Workflow](instructions/DEVELOPMENT_WORKFLOW.md) |
| **Set up dev environment** | [Dev Container Setup](development/setup/DEVCONTAINER_SETUP.md) |
| **Understand architecture** | [Architecture Summary](architecture/ARCHITECTURE_SUMMARY.md) |
| **Test the API** | [API Testing Guide](development/testing/API_TESTING_GUIDE.md) |
| **Set up authentication** | [Authentication Setup](deployment/AUTHENTICATION_SETUP.md) |
| **Deploy to Azure** | [Azure Deployment Strategy](deployment/AZURE_DEPLOYMENT_STRATEGY.md) |
| **Sprint progress** | [Sprint Roadmap](planning/SPRINT_ROADMAP.md) + [Week 1 Summaries](planning/sprints/week1/) |

### By Role

**New Developer**:
1. [Dev Container Setup](development/setup/DEVCONTAINER_SETUP.md)
2. [Solution Structure](architecture/SOLUTION_STRUCTURE.md)
3. [Development Workflow](instructions/DEVELOPMENT_WORKFLOW.md)
4. [Task Journal](planning/TASK_JOURNAL.md) - Current status

**Project Manager**:
1. [Sprint Executive Summary](planning/SPRINT_EXECUTIVE_SUMMARY.md)
2. [Task Journal](planning/TASK_JOURNAL.md)
3. [Sprint Roadmap](planning/SPRINT_ROADMAP.md)

**Architect**:
1. [Architecture Summary](architecture/ARCHITECTURE_SUMMARY.md)
2. [RBAC Architecture](architecture/RBAC_ARCHITECTURE.md)
3. [Privacy & Security](architecture/PRIVACY_AND_SECURITY.md)
4. [Observability Strategy](architecture/OBSERVABILITY_STRATEGY.md)

**DevOps Engineer**:
1. [Azure Deployment Strategy](deployment/AZURE_DEPLOYMENT_STRATEGY.md)
2. [CI/CD Deployment Status](planning/CI_CD_DEPLOYMENT_STATUS.md)
3. [Aspire Migration Log](deployment/ASPIRE_MIGRATION_LOG.md)

---

## 📊 Project Status

### ✅ Completed
- Multi-agent architecture with 5 subject agents
- Student Progress Orchestrator with intelligent decision-making
- Analytics API with 7 endpoints
- PostgreSQL database integration
- JWT authentication infrastructure
- OLLAMA local LLM integration
- .NET Aspire orchestration
- CI/CD pipeline
- Comprehensive testing (80%+ coverage)

### 🔄 In Progress
- Real-time monitoring dashboard (Day 5)
- Advanced orchestrator features
- Performance optimization

### 📋 Planned
- Additional AI agent types
- Enhanced adaptive learning
- Mobile application support

See [Task Journal](planning/TASK_JOURNAL.md) for detailed status.

---

## 🤝 Contributing

1. **Read the workflow**: [Development Workflow](instructions/DEVELOPMENT_WORKFLOW.md)
2. **Check current work**: [Task Journal](planning/TASK_JOURNAL.md)
3. **Follow conventions**: Branch naming, commit messages, PR templates
4. **Write tests**: Maintain 80%+ code coverage
5. **Update docs**: Keep Task Journal current

---

## 📞 Getting Help

- **Documentation questions**: Check this README's "Finding Information" section
- **Technical issues**: See [Known Issues](development/KNOWN_ISSUES.md)
- **Setup problems**: Review [Dev Container Setup](development/setup/DEVCONTAINER_SETUP.md)
- **Architecture questions**: Start with [Architecture Summary](architecture/ARCHITECTURE_SUMMARY.md)

---

## 📖 Documentation Standards

- **Single source of truth**: [Task Journal](planning/TASK_JOURNAL.md) for all status updates
- **Update immediately**: Document changes when they happen, not later
- **Keep organized**: Use the folder structure, don't create duplicates
- **Link generously**: Cross-reference related documents
- **Markdown format**: All documentation in `.md` format

See [Copilot Instructions](instructions/copilot-instructions.md) for detailed documentation guidelines.

---

**Last Updated**: October 16, 2025  
**Documentation Version**: 2.0 (reorganized structure)

- [PHASE_1_COMPLETE.md](./PHASE_1_COMPLETE.md) - Phase 1 completion summary
- [PHASE_2_COMPLETE.md](./PHASE_2_COMPLETE.md) - Phase 2 completion summary
- [QUICK_WINS_COMPLETE.md](./QUICK_WINS_COMPLETE.md) - Quick wins milestone completion

### Business Documentation

- [B2B_VS_B2C_COMPARISON.md](./B2B_VS_B2C_COMPARISON.md) - Business model analysis
- [SELF_SERVICE_ONBOARDING.md](./SELF_SERVICE_ONBOARDING.md) - Self-service onboarding design

### Instructions

- [copilot-instructions.md](./copilot-instructions.md) - AI coding assistant guidelines

## Key Files in Codebase

### Recently Created/Modified

#### Controllers

- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs` - 7 analytics endpoints

#### Services  

- `src/AcademicAssessment.Web/Services/TenantContextDevelopment.cs` - Development tenant context
- `src/AcademicAssessment.Web/Services/StubRepositoryBase.cs` - Base class for stub repositories
- `src/AcademicAssessment.Web/Services/StubStudentAssessmentRepository.cs` - Student assessment stub
- `src/AcademicAssessment.Web/Services/StubStudentResponseRepository.cs` - Student response stub
- `src/AcademicAssessment.Web/Services/StubQuestionRepository.cs` - Question stub
- `src/AcademicAssessment.Web/Services/StubAssessmentRepository.cs` - Assessment stub

#### Tests

- `tests/AcademicAssessment.Tests.Integration/Controllers/StudentAnalyticsControllerTests.cs` - 24 integration tests

#### Configuration

- `src/AcademicAssessment.Web/Program.cs` - Service registrations and middleware (lines 203-212)

## Quick Commands

### Build & Run

```bash
# Build solution
dotnet build

# Run Web API
dotnet run --project src/AcademicAssessment.Web/AcademicAssessment.Web.csproj

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/AcademicAssessment.Tests.Integration/AcademicAssessment.Tests.Integration.csproj
```

### Database (When Configured)

```bash
# Add migration
dotnet ef migrations add MigrationName --project src/AcademicAssessment.Infrastructure

# Update database
dotnet ef database update --project src/AcademicAssessment.Infrastructure
```

### Docker

```bash
# Start services (PostgreSQL, Redis)
docker-compose up -d

# Stop services
docker-compose down
```

### Git

```bash
# Check status
git status

# View recent commits
git log --oneline -10

# Check CI/CD runs
gh run list --limit 5
```

## API Endpoints (Currently Implemented)

All endpoints are prefixed with `/api/v1/students/{studentId:guid}/analytics/`

1. **GET** `/performance-summary` - Overall performance metrics
2. **GET** `/subject-performance?subject={optional}` - Subject-specific analytics
3. **GET** `/learning-objectives?subject={optional}` - Learning objective mastery
4. **GET** `/ability-estimates` - IRT ability estimates
5. **GET** `/improvement-areas?topN={optional}` - Areas needing improvement
6. **GET** `/progress-timeline?startDate={optional}&endDate={optional}` - Time-series progress
7. **GET** `/peer-comparison?gradeLevel={required}&subject={optional}` - Peer comparison

**Note:** Currently returning stub/empty data. Will return real data after database integration.

## Troubleshooting

### Tests Failing

- Check if stub repositories are registered correctly
- Verify Program class is exposed for testing (`public partial class Program { }`)
- Ensure test expectations match ASP.NET Core behavior (invalid GUID → 404, not 400)

### API Not Starting

- Check if all repository dependencies are registered
- Verify connection strings in appsettings.json
- Ensure port 5103 is available

### CI/CD Pipeline Failing

- Check GitHub Actions logs: <https://github.com/johnazariah/edumind-ai/actions>
- Verify all tests pass locally first
- Check for missing dependencies or configuration

## Performance Benchmarks

### Current (with stub data)

- All endpoints: <100ms response time
- Build time: ~20-30 seconds
- Test execution: ~3-5 seconds for 24 integration tests

### Target (with real data)

- Analytics endpoints: <500ms response time
- Complex queries: <2s response time
- Support 1000+ concurrent users

## Contact & Resources

- **Repository:** <https://github.com/johnazariah/edumind-ai>
- **CI/CD:** <https://github.com/johnazariah/edumind-ai/actions>
- **Swagger UI (when running):** <http://localhost:5103/swagger>

## Recent Commits

- `1b5ef9f` - test: Fix integration test expectations to match ASP.NET Core behavior
- `b83d6f0` - docs: Update task journal with StudentAnalyticsController implementation
- `0c741ae` - feat: Implement StudentAnalyticsController with 7 endpoints and comprehensive tests

---
**Last Updated:** October 14, 2025
**Status:** ✅ Ready for database integration and authentication
