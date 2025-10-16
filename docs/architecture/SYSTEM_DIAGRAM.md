# EduMind.AI System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            USER INTERFACES (Blazor Apps)                         │
├───────────────┬───────────────┬───────────────┬───────────────┬─────────────────┤
│   Student     │   Teacher     │ School Admin  │ Course Admin  │ Business/Sys    │
│     App       │   ClassApp    │     App       │     App       │    Admin Apps   │
│               │               │               │               │                 │
│ • Assessments │ • Class View  │ • School View │ • Curriculum  │ • Onboarding    │
│ • Progress    │ • Grading     │ • Analytics   │ • Questions   │ • Billing       │
│ • Feedback    │ • Feedback    │ • Reports     │ • Standards   │ • System Ops    │
└───────┬───────┴───────┬───────┴───────┬───────┴───────┬───────┴─────────┬───────┘
        │               │               │               │                 │
        └───────────────┴───────────────┴───────────────┴─────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        WEB API LAYER (ASP.NET Core)                             │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │   Student    │  │   Teacher    │  │ School Admin │  │ Course Admin │       │
│  │ Controller   │  │ Controller   │  │  Controller  │  │  Controller  │       │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘       │
│                                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────────────┐       │
│  │  Business    │  │   System     │  │     SignalR Hubs               │       │
│  │   Admin      │  │   Admin      │  │  • Student Progress Hub        │       │
│  │ Controller   │  │ Controller   │  │  • Class Monitoring Hub        │       │
│  └──────────────┘  └──────────────┘  │  • Admin Dashboard Hub         │       │
│                                       └────────────────────────────────┘       │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐ │
│  │              AUTHENTICATION & AUTHORIZATION MIDDLEWARE                    │ │
│  │  • Azure AD B2C  • JWT Tokens  • Role-Based Policies                     │ │
│  │  • Tenant Context Extraction  • Claims Transformation                    │ │
│  └───────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          BUSINESS LOGIC LAYER                                    │
├─────────────────┬─────────────────┬─────────────────┬─────────────────────────┤
│  Orchestration  │     Agents      │   Analytics     │   Shared Services       │
├─────────────────┼─────────────────┼─────────────────┼─────────────────────────┤
│                 │                 │                 │                         │
│ • Progress      │ • Math Agent    │ • Performance   │ • LLM Service          │
│   Orchestrator  │ • Physics Agent │   Analytics     │   (Azure OpenAI)       │
│                 │ • Chemistry     │ • Statistical   │ • Adaptive Testing     │
│ • Assessment    │   Agent         │   Analysis      │   Engine (ML.NET)      │
│   Scheduler     │ • Biology Agent │ • Predictive    │ • Caching (Redis)      │
│                 │ • English Agent │   Models        │ • Notification         │
│ • Task Router   │                 │ • Reporting     │   Service              │
│                 │ • Shared Base   │   Engine        │                         │
└─────────────────┴─────────────────┴─────────────────┴─────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          DATA ACCESS LAYER                                       │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐ │
│  │                      TENANT CONTEXT MIDDLEWARE                            │ │
│  │  Automatic filtering by SchoolId, ClassId based on user claims           │ │
│  └───────────────────────────────────────────────────────────────────────────┘ │
│                                                                                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐            │
│  │     Student      │  │   Assessment     │  │    Question      │            │
│  │   Repository     │  │   Repository     │  │   Repository     │            │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘            │
│                                                                                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐            │
│  │     School       │  │     Class        │  │     Course       │            │
│  │   Repository     │  │   Repository     │  │   Repository     │            │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘            │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐ │
│  │                   ENTITY FRAMEWORK CORE CONTEXT                           │ │
│  │  • Row-Level Security (Query Filters)                                    │ │
│  │  • Multi-Tenant Data Isolation                                           │ │
│  │  • Optimistic Concurrency                                                │ │
│  └───────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              DATA STORAGE                                        │
├───────────────────┬────────────────────┬────────────────────┬──────────────────┤
│   PostgreSQL      │      Redis         │  Azure Blob        │   Azure OpenAI   │
│   (Primary DB)    │    (Caching)       │   (Documents)      │   (LLM Service)  │
│                   │                    │                    │                  │
│ • Students        │ • Session Data     │ • Assessment PDFs  │ • GPT-4o         │
│ • Assessments     │ • Question Cache   │ • Student Reports  │ • Question Gen   │
│ • Questions       │ • Real-time State  │ • Backups          │ • Evaluation     │
│ • Progress Data   │ • Rate Limiting    │                    │ • Feedback       │
└───────────────────┴────────────────────┴────────────────────┴──────────────────┘
```

## Data Isolation & Security

### Multi-Tenant Hierarchy

```
┌──────────────────────────────────────────────────────────────────┐
│                       SYSTEM (ROOT)                              │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │            SCHOOL 1 (Tenant Boundary)                      │ │
│  │                                                            │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │ │
│  │  │   Class A   │  │   Class B   │  │   Class C   │      │ │
│  │  │             │  │             │  │             │      │ │
│  │  │ • Student 1 │  │ • Student 4 │  │ • Student 7 │      │ │
│  │  │ • Student 2 │  │ • Student 5 │  │ • Student 8 │      │ │
│  │  │ • Student 3 │  │ • Student 6 │  │ • Student 9 │      │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘      │ │
│  │                                                            │ │
│  │  Teachers: T1, T2, T3                                     │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │            SCHOOL 2 (Tenant Boundary)                      │ │
│  │  [Similar structure]                                       │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │      COURSES (Cross-School Content)                        │ │
│  │  • Grade 8 Math  • Grade 9 Physics  • Grade 10 Biology    │ │
│  │  • Managed by Course Administrators                       │ │
│  │  • Shared across all schools                              │ │
│  └────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

### Access Control Matrix

| Role               | Own Data | Class Data | School Data | All Schools | System |
|--------------------|----------|------------|-------------|-------------|--------|
| Student            | ✅ Full  | ❌         | ❌          | ❌          | ❌     |
| Teacher            | ✅ Full  | ✅ Full    | ❌          | ❌          | ❌     |
| School Admin       | ✅ Full  | ✅ Full    | ✅ Full     | ❌          | ❌     |
| Course Admin       | ✅ Full  | 📊 Anon.   | 📊 Anon.    | 📊 Anon.    | ❌     |
| Business Admin     | ✅ Full  | 🔒 Meta    | ✅ Full     | ✅ Full     | ❌     |
| System Admin       | ✅ Full  | ✅ Full    | ✅ Full     | ✅ Full     | ✅ Full|

**Legend:**

- ✅ Full Access
- ❌ No Access
- 📊 Anonymized/Aggregated Only
- 🔒 Metadata Only (no assessment content)

## Request Flow Example: Student Taking Assessment

```
1. Student logs in
   └─→ Azure AD B2C authenticates
       └─→ JWT token with claims: { userId, role: "Student", schoolId, classIds }

2. Student requests assessment
   └─→ GET /api/student/assessments
       └─→ TenantContextMiddleware extracts claims
           └─→ Sets ITenantContext { SchoolId, ClassIds, Role }
               └─→ StudentController validates authorization
                   └─→ AssessmentRepository.GetActiveAssessments()
                       └─→ EF Core automatically filters by SchoolId/ClassId
                           └─→ Returns only assessments for student's school/class
                               └─→ JSON response to client

3. Student submits answers
   └─→ POST /api/student/assessments/{id}/submit
       └─→ Validation: Assessment belongs to student's school/class
           └─→ MathAgent evaluates answers (LLM call)
               └─→ AdaptiveEngine updates ability estimate
                   └─→ Save to database (with SchoolId/ClassId)
                       └─→ SignalR notification to Teacher (ClassMonitoringHub)
                           └─→ Update student progress dashboard

4. Teacher views results (real-time)
   └─→ SignalR connection to ClassMonitoringHub
       └─→ Filtered to teacher's assigned classes only
           └─→ Live updates appear in dashboard
```

## Functional Programming Patterns

### Railway-Oriented Programming

```
GetStudent(id)
    ↓ (Result<Student>)
ValidateEligibility()
    ↓ (Result<Student>)
GenerateAssessment()
    ↓ (Result<Assessment>)
SelectQuestions()
    ↓ (Result<Assessment>)
Success! ✓

OR at any point:
    ↓ (Error)
Failure ✗
```

### Composition Pipeline

```
student
  |> getById
  |> bindAsync validateEligibility
  |> bindAsync estimateAbility
  |> map calculateDifficulty
  |> bindAsync selectQuestions
  |> map createAssessment
  |> match
      success -> return Ok
      failure -> return BadRequest
```

---

*This diagram represents the complete system architecture with multi-tenant isolation, role-based access control, and functional programming patterns.*

---

*Last Updated: October 11, 2025*
