# Role-Based Access Control (RBAC) Architecture

## Overview
EduMind.AI implements a comprehensive multi-tenant, role-based access control system with six distinct user personas, each with dedicated application interfaces and specific capabilities.

### Privacy-First Architecture

**Guiding Principle: "Student data is sacred and must be protected at all costs."**

EduMind.AI uses **physical database partitioning** where each school has its own dedicated database instance, ensuring absolute data isolation and making inadvertent data leaking impossible. See [PRIVACY_AND_SECURITY.md](PRIVACY_AND_SECURITY.md) for comprehensive privacy protections including:

- Physical database-level isolation per school (one database per school)
- Privacy-preserving aggregation (minimum 5 students for reports)
- Comprehensive audit logging (FERPA/GDPR compliance)
- Right to be forgotten implementation
- Differential privacy for aggregate reports
- Anonymized reporting for course administrators

## User Personas and Interfaces

### 1. Student
**Primary Interface**: `AcademicAssessment.StudentApp` (Blazor Web App)  
**Access Level**: Individual  
**Tenant Scope**: Self only

#### Capabilities
- Take assessments in assigned subjects
- View personal progress and performance metrics
- Access personalized study recommendations
- Review completed assessments and feedback
- Track learning objectives and mastery levels
- Receive real-time progress updates
- Access adaptive learning resources

#### Key Features
- Assessment taking interface with various question types
- Personal dashboard with progress visualization
- Study recommendation engine
- Historical performance charts
- Learning path tracker
- Real-time feedback display
- Accessibility features (screen readers, keyboard navigation)

#### Data Access
- **Read**: Own assessments, progress, recommendations, grades
- **Write**: Submit assessment responses, update preferences
- **No Access**: Other students' data, class/school aggregates, administrative functions

---

### 2. Class (Group of Students)
**Primary Interface**: `AcademicAssessment.ClassApp` (Blazor Web App - NEW)  
**Access Level**: Class/Teacher  
**Tenant Scope**: Assigned classes only

#### Capabilities
- Monitor real-time class progress during assessments
- View aggregated class performance analytics
- Compare individual student performance within class
- Assign and schedule assessments for the class
- Review and grade subjective questions (essays, short answers)
- Provide personalized feedback to students
- Identify struggling students requiring intervention
- Generate class performance reports
- Manage class roster and student groupings

#### Key Features
- Live assessment monitoring dashboard
- Class-level analytics and visualizations
- Student comparison tools
- Grading interface with rubrics
- Feedback management system
- Intervention identification alerts
- Class performance reports (PDF/Excel export)
- Communication tools (announcements, feedback)

#### Data Access
- **Read**: All students in assigned classes, class aggregates, course materials
- **Write**: Grades, feedback, assessment assignments, class settings
- **No Access**: Other classes' detailed data, school-wide administration, course content management

---

### 3. School (Group of Classes)
**Primary Interface**: `AcademicAssessment.SchoolAdminApp` (Blazor Web App - NEW)  
**Access Level**: School Administrator/Principal  
**Tenant Scope**: Single school (all classes within school)

#### Capabilities
- View school-wide performance analytics
- Monitor all classes and teachers in the school
- Compare class performance across grade levels
- Manage school-level assessment schedules
- View teacher effectiveness metrics
- Generate school performance reports for stakeholders
- Configure school-specific settings
- Manage teacher assignments to classes
- Access intervention reports and trends
- Export compliance and audit reports

#### Key Features
- School-wide dashboard with KPIs
- Multi-class performance comparison
- Teacher effectiveness analytics
- Grade-level performance tracking
- Trend analysis and forecasting
- Compliance reporting tools
- School settings management
- Teacher and class roster management
- Stakeholder report generation

#### Data Access
- **Read**: All classes, teachers, students within school, school aggregates
- **Write**: School settings, teacher assignments, school-wide announcements
- **No Access**: Other schools' data, course content management, system configuration, pricing

---

### 4. Course Administrator
**Primary Interface**: `AcademicAssessment.CourseAdminApp` (Blazor Web App - NEW)  
**Access Level**: Subject Matter Expert/Curriculum Designer  
**Tenant Scope**: Specific courses/subjects across all schools

#### Capabilities
- Design and manage course curriculum and learning objectives
- Create and curate question banks for specific courses
- Define assessment templates and rubrics
- Configure adaptive testing parameters for the course
- Review and approve LLM-generated questions
- Manage curriculum alignment with educational standards
- Analyze cross-school course performance
- Update course difficulty calibration
- Create teaching resources and study materials
- Version control for course content

#### Key Features
- Curriculum design interface
- Question bank management system
- Assessment template builder
- Adaptive testing configuration
- LLM prompt management for question generation
- Standards alignment tools
- Cross-school analytics for the course
- Content versioning and approval workflow
- Teaching resource library
- Difficulty calibration dashboard

#### Data Access
- **Read**: All assessment data for assigned courses (cross-school), question performance metrics
- **Write**: Course content, questions, learning objectives, assessment templates, standards
- **No Access**: Individual student PII (anonymized data only), school business operations, system infrastructure

---

### 5. Business Administrator
**Primary Interface**: `AcademicAssessment.BusinessAdminApp` (Blazor Web App - NEW)  
**Access Level**: Operations/Business Management  
**Tenant Scope**: System-wide (multi-tenant management)

#### Capabilities
- Onboard new schools and districts
- Create and manage school hierarchies
- Add and configure classes for schools
- Enroll students and assign to classes
- Manage course offerings and subscriptions
- Configure pricing and billing
- Assign teachers and administrators to schools
- Manage user accounts and initial access
- Generate business intelligence reports
- Monitor system usage and capacity
- Handle subscription renewals and changes

#### Key Features
- School onboarding wizard
- Multi-tenant management dashboard
- Student bulk import/enrollment tools
- Class and roster management
- User account provisioning
- Subscription and licensing management
- Billing and invoice generation
- Usage analytics and capacity planning
- SLA monitoring dashboard
- Customer relationship management

#### Data Access
- **Read**: All schools, classes, users, subscriptions, billing data, usage metrics
- **Write**: School/class/user creation, subscriptions, billing, assignments
- **No Access**: Assessment content, student assessment responses, course curriculum design, system infrastructure configuration

---

### 6. System Administrator
**Primary Interface**: `AcademicAssessment.SysAdminApp` (Blazor Web App - NEW)  
**Access Level**: Technical/DevOps  
**Tenant Scope**: Entire system (infrastructure and operations)

#### Capabilities
- Monitor system health and performance
- Manage infrastructure and deployments
- Configure system-wide settings and features
- Monitor and manage LLM usage and costs
- Handle security and compliance
- Manage database backups and disaster recovery
- Configure authentication and authorization
- Monitor API usage and rate limiting
- Manage caching and performance optimization
- Review audit logs and security events
- Troubleshoot technical issues

#### Key Features
- System health monitoring dashboard
- Infrastructure management console
- LLM provider configuration and cost tracking
- Database administration tools
- Security and compliance dashboard
- Backup and disaster recovery management
- API management and monitoring
- Performance tuning interface
- Audit log viewer and analysis
- Feature flag management
- Environment configuration

#### Data Access
- **Read**: All system data, logs, metrics, configurations
- **Write**: System configuration, infrastructure settings, feature flags, security policies
- **No Access**: Student assessment content creation (read-only for troubleshooting)

---

## Access Control Matrix

| Capability | Student | Class/Teacher | School Admin | Course Admin | Business Admin | System Admin |
|-----------|---------|---------------|--------------|--------------|----------------|--------------|
| Take Assessments | ✅ Own | ❌ | ❌ | ❌ | ❌ | ❌ |
| View Own Progress | ✅ | ✅ | ✅ | 📊 Aggregated | 📊 Aggregated | 📊 Aggregated |
| Grade Assessments | ❌ | ✅ Own Classes | ❌ | ❌ | ❌ | ❌ |
| View Class Analytics | ❌ | ✅ Own Classes | ✅ All Classes | 📊 Cross-School | 📊 Aggregated | 📊 System-Wide |
| Manage Curriculum | ❌ | ❌ | ❌ | ✅ | ❌ | ⚙️ Config Only |
| Create Questions | ❌ | 🔒 Limited | ❌ | ✅ | ❌ | ❌ |
| Manage Students | ❌ | 🔒 Own Classes | 🔒 Own School | ❌ | ✅ | ⚙️ User Admin |
| Manage Schools | ❌ | ❌ | 🔒 Own School | ❌ | ✅ | ⚙️ Config Only |
| Configure System | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| View Billing | ❌ | ❌ | 📊 Own School | ❌ | ✅ | 📊 System-Wide |
| Manage Subscriptions | ❌ | ❌ | ❌ | ❌ | ✅ | ⚙️ Config Only |
| LLM Configuration | ❌ | ❌ | ❌ | 🔒 Prompts Only | ❌ | ✅ |
| Security & Compliance | ❌ | ❌ | 📊 Reports Only | ❌ | 📊 Reports Only | ✅ |

**Legend:**
- ✅ Full Access
- ❌ No Access
- 🔒 Limited Access (specific scope)
- 📊 Read-Only/Aggregated Data
- ⚙️ Configuration Only

---

## Multi-Tenant Data Isolation

### Tenant Hierarchy
```
System (Root)
└── Schools (Tenant Level 1)
    ├── Classes (Tenant Level 2)
    │   └── Students (Tenant Level 3)
    └── Courses (Cross-School)
        └── Content & Questions
```

### Data Isolation Strategy

#### 1. Row-Level Security (RLS)
- Every entity has `SchoolId` and/or `ClassId` for filtering
- Database-level RLS policies enforce tenant boundaries
- Claims-based filtering in Entity Framework Core

#### 2. Claim-Based Authorization
```csharp
public class UserClaims
{
    public string UserId { get; set; }
    public UserRole Role { get; set; }
    public string? SchoolId { get; set; }      // Null for System/Business Admins
    public List<string>? ClassIds { get; set; } // Null for non-teachers
    public List<string>? CourseIds { get; set; }// For Course Admins
}
```

#### 3. API Endpoint Security
- All API endpoints validate tenant context
- Automatic filtering based on user claims
- Middleware enforces data isolation

---

## Authentication & Authorization Flow

### 1. Authentication
- **Azure AD B2C** for enterprise SSO
- **Multi-factor authentication** for administrative roles
- **Student authentication** via school-issued credentials
- **Password policies** based on role

### 2. Authorization Policies
```csharp
public static class AuthorizationPolicies
{
    public const string StudentOnly = "StudentOnly";
    public const string TeacherOnly = "TeacherOnly";
    public const string SchoolAdminOnly = "SchoolAdminOnly";
    public const string CourseAdminOnly = "CourseAdminOnly";
    public const string BusinessAdminOnly = "BusinessAdminOnly";
    public const string SystemAdminOnly = "SystemAdminOnly";
    
    public const string TeacherOrAbove = "TeacherOrAbove";
    public const string SchoolAdminOrAbove = "SchoolAdminOrAbove";
    public const string BusinessOrSystemAdmin = "BusinessOrSystemAdmin";
}
```

### 3. Permission Granularity
- **Coarse-grained**: Role-based (Teacher, Admin, etc.)
- **Fine-grained**: Resource-based (specific class, school, course)
- **Dynamic**: Context-aware (time-based, location-based)

---

## Application Architecture

### Updated Solution Structure

```
src/
├── AcademicAssessment.Core/              # Shared domain models
├── AcademicAssessment.Infrastructure/    # Shared infrastructure
├── AcademicAssessment.Agents/            # AI agents
├── AcademicAssessment.Orchestration/     # Orchestration layer
├── AcademicAssessment.Analytics/         # Analytics engine
├── AcademicAssessment.Web/               # Unified Web API
│   ├── Controllers/
│   │   ├── StudentController.cs
│   │   ├── TeacherController.cs
│   │   ├── SchoolAdminController.cs
│   │   ├── CourseAdminController.cs
│   │   ├── BusinessAdminController.cs
│   │   └── SystemAdminController.cs
│   ├── Hubs/
│   │   ├── StudentProgressHub.cs         # For students
│   │   ├── ClassMonitoringHub.cs         # For teachers
│   │   └── AdminDashboardHub.cs          # For all admins
│   └── Middleware/
│       └── TenantContextMiddleware.cs
├── AcademicAssessment.StudentApp/        # Student Blazor app
├── AcademicAssessment.ClassApp/          # Teacher/Class Blazor app (NEW)
├── AcademicAssessment.SchoolAdminApp/    # School Admin Blazor app (NEW)
├── AcademicAssessment.CourseAdminApp/    # Course Admin Blazor app (NEW)
├── AcademicAssessment.BusinessAdminApp/  # Business Admin Blazor app (NEW)
└── AcademicAssessment.SysAdminApp/       # System Admin Blazor app (NEW)
```

### Shared Components Library (NEW)
```
src/AcademicAssessment.SharedUI/
├── Components/
│   ├── Charts/           # Reusable chart components
│   ├── Grids/            # Data grid components
│   ├── Analytics/        # Analytics widgets
│   └── Common/           # Shared UI components
└── Services/
    └── ApiClient/        # Typed API client for all apps
```

---

## API Design Principles

### 1. Unified API with Role-Based Endpoints
Single API (`AcademicAssessment.Web`) with controller segmentation:

```
/api/student/*              - Student endpoints
/api/teacher/*              - Teacher/Class endpoints
/api/school-admin/*         - School administrator endpoints
/api/course-admin/*         - Course administrator endpoints
/api/business-admin/*       - Business administrator endpoints
/api/system-admin/*         - System administrator endpoints
```

### 2. Automatic Tenant Filtering
```csharp
[ApiController]
[Route("api/teacher")]
[Authorize(Policy = AuthorizationPolicies.TeacherOnly)]
public class TeacherController : ControllerBase
{
    // Automatic filtering to teacher's assigned classes
    [HttpGet("classes")]
    public async Task<IActionResult> GetMyClasses()
    {
        var classIds = User.GetClassIds(); // From claims
        // Automatically filtered by middleware
    }
}
```

### 3. Cross-Cutting Concerns
- **Audit Logging**: All admin actions logged
- **Rate Limiting**: Per-role rate limits
- **Data Encryption**: PII encrypted at rest
- **GDPR Compliance**: Right to deletion, data export

---

## Deployment Strategy

### Multi-App Deployment Options

#### Option 1: Separate Deployments (Recommended for Scale)
- Each Blazor app deployed independently
- Better for different scaling requirements
- Independent update cycles
- Separate CDN/hosting for each persona

#### Option 2: Single Deployment with Routing
- One deployment, route based on authentication
- Simpler infrastructure
- Shared resources and caching
- Lower operational complexity

#### Option 3: Hybrid Approach
- Student app separate (highest traffic)
- Admin apps combined
- API layer separate and scaled independently

---

## Security Considerations

### 1. Student Privacy (FERPA Compliance)
- PII encrypted at rest and in transit
- Anonymized data for course administrators
- Audit trail for all data access
- Parental consent management

### 2. Administrative Segregation
- Business admins cannot access assessment content
- Course admins cannot access PII or billing
- School admins limited to their school
- System admins have read-only on business data

### 3. Data Retention Policies
- Student data retention per school policy
- Assessment history maintained for academic records
- Audit logs retained for compliance
- Right to be forgotten implementation

---

## Performance Optimization

### 1. Caching Strategy by Role
- **Students**: Aggressive caching of course content
- **Teachers**: Real-time data, minimal caching
- **Admins**: Cached aggregates, refresh on-demand
- **Course Admins**: Long-lived question bank cache

### 2. SignalR Scaling
- Separate hubs for different use cases
- Redis backplane for horizontal scaling
- Connection-based rate limiting

### 3. Database Optimization
- Indexed tenant columns (SchoolId, ClassId)
- Partitioning by school for large deployments
- Read replicas for analytics queries

---

## Implementation Phases

### Phase 1: Core Infrastructure (Weeks 1-2)
- ✅ Multi-tenant data model
- ✅ Authentication and authorization
- ✅ Tenant context middleware
- ✅ Role-based API controllers

### Phase 2: Student & Teacher Apps (Weeks 3-6)
- ✅ Student assessment interface
- ✅ Teacher class monitoring dashboard
- ✅ Grading interface
- ✅ Real-time progress tracking

### Phase 3: School Administration (Weeks 7-8)
- ✅ School admin dashboard
- ✅ School-wide analytics
- ✅ Reporting tools

### Phase 4: Course & Content Management (Weeks 9-11)
- ✅ Course admin interface
- ✅ Question bank management
- ✅ Curriculum design tools

### Phase 5: Business Operations (Weeks 12-13)
- ✅ Business admin dashboard
- ✅ School onboarding
- ✅ Subscription management

### Phase 6: System Administration (Weeks 14-15)
- ✅ System admin console
- ✅ Monitoring and logging
- ✅ LLM cost tracking

### Phase 7: Testing & Deployment (Week 16)
- ✅ Integration testing
- ✅ Performance testing
- ✅ Security audit
- ✅ Production deployment

---

*Last Updated: October 11, 2025*
