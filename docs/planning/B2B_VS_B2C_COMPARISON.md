# Dual Onboarding Model: B2B vs B2C Comparison

## Overview

EduMind.AI supports two distinct onboarding and deployment models to serve different markets:

1. **School-Based (B2B)** - White-box SaaS for educational institutions
2. **Self-Service (B2C)** - Consumer-facing casual signup for individual learners

This document provides a side-by-side comparison of both models.

---

## Quick Comparison Matrix

| Dimension | School-Based (B2B) | Self-Service (B2C) |
|-----------|-------------------|-------------------|
| **Target Market** | Schools, Districts | Individual Students |
| **Typical Users** | 100-10,000 students | 1 student at a time |
| **Signup Process** | Admin adds students | Self-signup (email/OAuth) |
| **Account Approval** | Immediate (admin-provisioned) | Immediate (or parental consent if <13) |
| **Payment Model** | School subscription ($5-15/student/year) | Freemium ($0-24.99/month) |
| **Database Model** | Dedicated per school | Shared "selfservice" tenant |
| **Data Isolation** | Physical (separate DB) | Logical (row-level security) |
| **Privacy Compliance** | FERPA (strictest) | COPPA + FERPA |
| **Features** | Full suite + teacher tools | Core features + gamification |
| **Social Features** | Class leaderboards | Anonymized global leaderboards |
| **Support** | Priority + dedicated | Self-help + community |
| **Reports** | Teacher/admin dashboards | Personal progress only |
| **Migration Path** | N/A | Can upgrade to school account |

---

## Detailed Comparison

### Target Users

#### School-Based (B2B)

- **Primary:** K-12 schools and districts
- **Secondary:** Homeschool co-ops, tutoring centers
- **Buyer:** School administrators, principals, district IT
- **End User:** Students (no payment required)
- **Scale:** 100-10,000 students per deployment

#### Self-Service (B2C)

- **Primary:** Individual students seeking extra practice
- **Secondary:** Homeschoolers, adult learners
- **Buyer:** Student or parent (consumer decision)
- **End User:** Same as buyer
- **Scale:** 1 student per signup

---

### Onboarding Journey

#### School-Based (B2B)

```
1. Sales Process
   └→ School contacts sales team
      └→ Demo and proposal
         └→ Contract negotiation
            └→ Purchase order

2. School Onboarding (Manual, 30-60 minutes)
   └→ Business admin creates school in system
      └→ Provision dedicated database
         └→ Run migrations and security checks
            └→ Create school admin account
               └→ Send welcome email

3. School Admin Setup
   └→ Configure school settings
      └→ Add teachers to system
         └→ Create classes
            └→ Import student roster (CSV or manual)
               └→ Assign students to classes

4. Student Access
   └→ Students receive login credentials
      └→ First login: password change required
         └→ Begin assessments

Timeline: 1-2 weeks from contract to first assessment
```

#### Self-Service (B2C)

```
1. Discovery
   └→ Student finds website (organic, ads, word-of-mouth)
      └→ Lands on marketing page

2. Signup (2-3 minutes)
   └→ Click "Start Learning Free"
      └→ Choose signup method:
         • Continue with Google
         • Continue with Apple
         • Continue with Email
      └→ Fill profile (name, age, grade)
         └→ Select subjects of interest
            └→ Set learning goals

3. Parental Consent (if under 13)
   └→ Enter parent email
      └→ Parent receives consent request
         └→ Parent reviews privacy policy
            └→ Parent clicks "Approve"
               └→ Account activated

4. Start Learning
   └→ Complete diagnostic assessment
      └→ AI generates personalized study plan
         └→ Begin practice

Timeline: 2-10 minutes from discovery to first assessment
         (or 24-48 hours if parental consent required)
```

---

### Privacy & Data Architecture

#### School-Based (B2B)

**Database Strategy:**

```
┌────────────────────────────────────────┐
│    PostgreSQL Cluster                  │
│                                        │
│  ┌──────────────┐  ┌──────────────┐  │
│  │  Database:   │  │  Database:   │  │
│  │  school_001  │  │  school_002  │  │
│  │              │  │              │  │
│  │  ISOLATED ✓  │  │  ISOLATED ✓  │  │
│  └──────────────┘  └──────────────┘  │
└────────────────────────────────────────┘

Physical isolation - impossible to cross-query
```

**Privacy Model:**

- ✅ FERPA compliant (strictest educational privacy law)
- ✅ Physical database per school
- ✅ No cross-school data access possible
- ✅ Teacher access to assigned classes only
- ✅ Privacy-preserving aggregation (min 5 students)
- ✅ Comprehensive audit logging
- ✅ School owns their data

**Data Ownership:**

- School owns student data
- School can export all data at any time
- School controls retention policies
- Student data deleted when school requests

#### Self-Service (B2C)

**Database Strategy:**

```
┌────────────────────────────────────────┐
│    PostgreSQL Cluster                  │
│                                        │
│  ┌──────────────────────────────────┐ │
│  │  Database: edumind_selfservice   │ │
│  │                                  │ │
│  │  Virtual Class: "Grade 8 Math"  │ │
│  │  • Student 1 (row-level filter) │ │
│  │  • Student 2 (row-level filter) │ │
│  │  • Student 3 (row-level filter) │ │
│  │                                  │ │
│  │  LOGICAL ISOLATION ✓             │ │
│  └──────────────────────────────────┘ │
└────────────────────────────────────────┘

Shared database with row-level security
```

**Privacy Model:**

- ✅ COPPA compliant (parental consent for <13)
- ✅ FERPA compliant for educational records
- ✅ Logical isolation via row-level security
- ✅ No cross-student visibility
- ✅ Anonymized leaderboards only
- ✅ Audit logging of all access
- ✅ Student/parent owns their data

**Data Ownership:**

- Student/parent owns data
- Can export data at any time
- Can delete account (right to be forgotten)
- No data sharing with third parties

---

### Feature Comparison

#### School-Based (B2B) - Full Suite

**Assessment Features:**

- ✅ All assessment types (Diagnostic, Formative, Summative, Adaptive)
- ✅ Teacher-assigned assessments
- ✅ Scheduled assessments (class-wide)
- ✅ Custom assessment creation
- ✅ Question bank access (school-licensed)
- ✅ Essay grading by teachers
- ✅ Partial credit support
- ✅ Multi-attempt assessments
- ✅ Time limits and proctoring

**Progress Tracking:**

- ✅ Individual student progress
- ✅ Class-level analytics
- ✅ School-wide reports
- ✅ Learning objective mastery
- ✅ IRT-based ability estimation
- ✅ Predictive intervention alerts
- ✅ Parent progress reports
- ✅ Historical trend analysis

**Teacher Tools:**

- ✅ Class dashboard
- ✅ Real-time monitoring during assessments
- ✅ Grading interface
- ✅ Feedback management
- ✅ Class comparison tools
- ✅ Intervention identification
- ✅ Custom rubrics
- ✅ Bulk operations

**Administrative Tools:**

- ✅ School-wide dashboard
- ✅ Teacher effectiveness metrics
- ✅ Class performance comparison
- ✅ Compliance reporting
- ✅ User management
- ✅ Settings configuration
- ✅ Backup and export

**Support:**

- ✅ Priority support (24-48 hour response)
- ✅ Dedicated account manager
- ✅ Training sessions for teachers
- ✅ Implementation support
- ✅ Regular check-ins

#### Self-Service (B2C) - Core + Gamification

**Free Tier:**

- ✅ 5 assessments per week
- ✅ All subjects (Math, Physics, Chemistry, Biology, English)
- ✅ Adaptive difficulty
- ✅ Basic progress tracking
- ✅ Achievement system
- ✅ Streak tracking
- ✅ Anonymized leaderboards
- ✅ Mobile app access
- ❌ No detailed analytics
- ❌ No personalized study plans
- ❌ Community support only

**Premium Tier ($9.99/month):**

- ✅ Everything in Free
- ✅ Unlimited assessments
- ✅ Detailed analytics and insights
- ✅ Personalized AI study plans
- ✅ Ad-free experience
- ✅ Priority support (7-day response)
- ✅ Export progress reports
- ✅ Custom goal setting

**Premium Plus Tier ($24.99/month):**

- ✅ Everything in Premium
- ✅ 1-on-1 virtual tutoring (4 hours/month)
- ✅ Live homework help
- ✅ College prep resources
- ✅ Parent dashboard access
- ✅ Expedited support (48-hour response)

**Gamification (All Tiers):**

- ✅ Experience points (XP)
- ✅ Daily streak tracking
- ✅ Achievements and badges
- ✅ Level progression
- ✅ Anonymized leaderboards
- ✅ Subject mastery milestones
- ✅ Social sharing (opt-in)

---

### Pricing Comparison

#### School-Based (B2B)

**Annual Subscription (per student):**

- **Basic:** $5/student/year
  - Core assessment features
  - Teacher dashboards
  - Basic analytics
  - Email support

- **Standard:** $10/student/year
  - Everything in Basic
  - Advanced analytics
  - Predictive intervention
  - Priority support
  - Training sessions

- **Premium:** $15/student/year
  - Everything in Standard
  - Custom content creation
  - API access
  - Dedicated account manager
  - 24/7 support

**Minimum:** 100 students  
**Volume Discounts:** 10% off for 500+ students, 20% off for 1000+

**Example:**

- School with 500 students on Standard plan
- $10/student × 500 = $5,000/year
- With 10% volume discount = **$4,500/year**

#### Self-Service (B2C)

**Monthly Subscription (per student):**

- **Free:** $0/month
  - 5 assessments/week
  - All subjects
  - Basic features
  - Ad-supported

- **Premium:** $9.99/month or $99/year (2 months free)
  - Unlimited assessments
  - Advanced features
  - Ad-free

- **Premium Plus:** $24.99/month or $249/year (2 months free)
  - Everything in Premium
  - 1-on-1 tutoring
  - Parent dashboard

**No minimum, no contract**

**Example:**

- Individual student on Premium annual plan
- **$99/year** ($8.25/month effective rate)

---

### Revenue Comparison

#### School-Based (B2B)

**Average School (500 students):**

- Revenue: $5,000/year (Standard plan)
- Cost: ~$1,500/year (infrastructure, support)
- Margin: ~70%
- LTV (5-year contract): $25,000
- CAC: ~$2,000 (sales team)
- Payback: 5 months

**Key Metrics:**

- High contract value
- Long sales cycle (1-3 months)
- High retention (80%+ annual renewal)
- Predictable revenue (annual contracts)
- Lower churn (institutional commitment)

#### Self-Service (B2C)

**Average Premium User:**

- Revenue: $99/year
- Cost: ~$15/year (infrastructure, support)
- Margin: ~85%
- LTV (2-year average): $198
- CAC: ~$20 (digital marketing)
- Payback: 2 months

**Key Metrics:**

- Low individual value
- Instant conversion
- Moderate retention (60% annual renewal)
- Variable revenue (monthly churn)
- Higher churn (consumer behavior)

**Conversion Funnel:**

- 100 free signups
- 5-10 convert to Premium (5-10%)
- 1-2 convert to Premium Plus (1-2%)

---

### Support Model

#### School-Based (B2B)

**Support Channels:**

- Dedicated account manager (Premium)
- Phone support during business hours
- Email support (24-48 hour SLA)
- Knowledge base
- Training sessions
- Implementation support
- Regular check-ins

**Support Team:**

- Tier 1: Customer success managers
- Tier 2: Technical support engineers
- Tier 3: Engineering escalation

**Proactive Support:**

- Quarterly business reviews
- Usage analytics sharing
- Best practices recommendations
- New feature training

#### Self-Service (B2C)

**Support Channels:**

- Email support (7-day SLA free, 48-hour SLA premium)
- In-app help center
- Community forum
- Video tutorials
- FAQ and knowledge base
- Chatbot for common questions

**Support Team:**

- Tier 1: Support specialists (email)
- Tier 2: Technical support (escalations)

**Self-Service:**

- Comprehensive documentation
- Interactive tutorials
- Community peer support
- AI-powered help suggestions

---

### Technical Architecture Differences

#### School-Based (B2B)

```csharp
// School-specific database context
public class SchoolDbContextFactory
{
    public async Task<AcademicContext> CreateDbContextAsync(
        SchoolId schoolId,
        CancellationToken ct)
    {
        // Resolve school-specific connection string
        var connectionString = await _resolver
            .GetConnectionStringAsync(schoolId, ct);
        
        // Create context connected to school's database
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseNpgsql(connectionString)
            .Options;
        
        return new AcademicContext(options, _tenantContext);
    }
}

// Physical isolation guarantees
// - Impossible to query wrong school's data
// - Database-level security
// - Clear audit trail
```

#### Self-Service (B2C)

```csharp
// Shared database with virtual classes
public class SelfServiceDbContext : AcademicContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Row-level security for self-service students
        modelBuilder.Entity<Student>()
            .HasQueryFilter(s => 
                s.SchoolId == SelfServiceSchoolId &&
                s.Id == _tenantContext.StudentId); // Only own data
        
        // Virtual classes auto-created
        modelBuilder.Entity<VirtualClass>()
            .HasQueryFilter(c => 
                c.IsVirtual == true &&
                c.GradeLevel == _tenantContext.GradeLevel);
    }
}

// Logical isolation with automatic filtering
// - Student can only query own data
// - No cross-student visibility
// - Anonymized aggregates for leaderboards
```

---

## When to Use Which Model?

### Choose School-Based (B2B) When

✅ **School or district deployment**
✅ **100+ students** in same institution
✅ **Teacher oversight required**
✅ **Formal classroom integration**
✅ **Need detailed analytics and reporting**
✅ **FERPA compliance with strictest isolation**
✅ **School pays, students use free**
✅ **Long-term institutional commitment**

**Example Use Cases:**

- Public school district-wide deployment
- Private school seeking assessment platform
- Charter school network
- Homeschool co-op (100+ families)
- Tutoring center with multiple teachers

### Choose Self-Service (B2C) When

✅ **Individual student** seeking practice
✅ **Casual learner** wanting flexibility
✅ **Supplemental to school work**
✅ **No teacher oversight needed**
✅ **Want to try before buying**
✅ **Prefer gamification and social features**
✅ **Consumer payment (student/parent pays)**
✅ **Flexible commitment**

**Example Use Cases:**

- Student preparing for exams independently
- Homeschooler working solo
- Adult learner refreshing skills
- Student whose school doesn't offer EduMind
- Summer break practice
- College student tutoring younger siblings

---

## Migration Path: Self-Service → School

If a self-service student's school later adopts EduMind.AI:

1. **Student notified** of school's adoption
2. **Migration wizard** launched
3. **Historical data exported** from self-service
4. **New account created** in school's database
5. **Data imported** with full history preserved
6. **Old account marked** as migrated (kept for audit)
7. **Student transitions** to school account
8. **Premium subscription** cancelled (school pays)

**Benefits:**

- Student keeps all progress and achievements
- Seamless transition
- No data loss
- Teachers gain visibility into historical performance
- Student gains access to full feature set

---

## Coexistence Strategy

Both models can coexist for the same student:

**Scenario:** Student has school account (forced use) but also wants personal practice

**Solution:**

- School account: Primary (used during school)
- Self-service account: Secondary (personal practice)
- Separate profiles, separate progress
- Option to merge later if desired

**Use Case:**

- Student uses school account for assigned work
- Student uses self-service for extra practice
- School sees only school-assigned assessments
- Student tracks personal goals separately

---

## Summary

| Aspect | School-Based (B2B) | Self-Service (B2C) |
|--------|-------------------|-------------------|
| **Best For** | Institutional adoption | Individual practice |
| **Onboarding** | Manual, careful | Instant, casual |
| **Privacy** | Physical isolation | Logical isolation |
| **Features** | Full suite | Core + gamification |
| **Revenue** | High value, long cycle | Low value, instant |
| **Support** | Dedicated, proactive | Self-service, reactive |
| **Retention** | High (institutional) | Moderate (consumer) |
| **Margin** | ~70% | ~85% |

**Recommendation:** Support both models simultaneously to maximize market coverage and revenue diversification.

---

*Last Updated: October 11, 2025*
