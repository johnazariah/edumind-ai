# Context for Academic Assessment Multi-Agent System

## Project Overview
We're building an **Academic Test Preparation Multi-Agent System** called "EduMind.AI" in C# .NET that provides personalized educational assessment for 1000+ students across grades 8-12 in 5 subjects (Mathematics, Physics, Chemistry, Biology, English).

## Core Architecture Patterns (from existing A2A system)
- **Multi-Agent Communication**: Based on Agent-to-Agent (A2A) protocol for agent discovery and task coordination
- **Base Agent Structure**: All agents inherit from `A2ABaseAgent` with standard task processing patterns
- **Real-time Updates**: SignalR hubs for live progress tracking and notifications
- **Task Management**: Structured task system with request/response patterns and streaming capabilities
- **Agent Discovery**: Dynamic agent registration and capability advertising

## Key Components to Build
1. **Subject Assessment Agents** (5 agents: Math, Physics, Chemistry, Biology, English)
2. **Student Progress Orchestrator** (central coordinator)
3. **Adaptive Testing Engine** (IRT-based question selection)
4. **Performance Analytics Agent** (statistical analysis and reporting)
5. **Real-time Progress Tracking** (SignalR hubs)

## Technology Stack
- **.NET 8** with ASP.NET Core Web API
- **Entity Framework Core** with PostgreSQL
- **SignalR** for real-time communication
- **ML.NET** for adaptive algorithms
- **Redis** for caching and session management
- **Blazor Server** for admin dashboards

## LLM Strategy
- **Primary LLM**: Azure OpenAI GPT-4o (~$100-150/month for 1000 students)
- **Backup LLM**: Anthropic Claude 3.5 Sonnet for writing evaluation
- **Secondary Backup**: Google Gemini 1.5 Pro for math/science
- **Cost Optimization**: Caching, batch processing, smart routing
- **Compliance**: FERPA-compliant Azure OpenAI with enterprise security
- **Performance**: <2 second response times, 99.9% uptime during school hours

## Educational Domain
- **Students**: 1000+ concurrent users
- **Grade Levels**: 8th, 9th, 10th, 11th, 12th
- **Subjects**: Mathematics, Physics, Chemistry, Biology, English
- **Assessment Types**: Diagnostic, Formative, Summative, Adaptive, Remedial, Challenge

## Implementation Strategy
Start by adapting the existing Python A2A patterns to C#:
1. Create base agent infrastructure (port from `common/a2a/`)
2. Implement Student Progress Orchestrator (similar to host_agent pattern)
3. Build first subject agent (Mathematics) using assessment generation patterns
4. Add adaptive testing engine with ML.NET
5. Integrate real-time progress tracking

## LLM Considerations


## Key Reference Files
- **Python A2A Base**: `common/a2a/server.py` and `common/a2a/client.py` 
- **Agent Pattern**: `agents/host_agent/agent.py` for orchestration patterns
- **Task Management**: How agents communicate via structured tasks
- **Real-time Updates**: Web UI patterns from `web_ui/app.py`

## Success Criteria
- Support 1000+ concurrent students taking assessments
- <2 second response times for assessment generation
- 95%+ accuracy in automated scoring across all subjects
- Real-time progress updates with <500ms latency
- Adaptive testing that improves learning outcomes by 30%

## Next Steps
The complete implementation guide is in `academic-assessment-copilot-instructions.md` with detailed code examples, data models, and architectural patterns. We'll start by creating the foundational agent infrastructure and then incrementally add subject-specific assessment capabilities.

## File Placement in New Repository
Place this file and the complete copilot instructions in the root of your new repository:
- `/CONTEXT_FOR_NEW_REPO.md` (this file)
- `/copilot-instructions.md` (the complete implementation guide)

Then we can begin implementing the multi-agent academic assessment system step by step.