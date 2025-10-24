# 09d. Analytics and Reporting Features

**Document Version:** 1.0  
**Last Updated:** October 24, 2025  
**Status:** Active Development

---

## Table of Contents

1. [Overview](#overview)
2. [Student Analytics](#student-analytics)
3. [Class Analytics](#class-analytics)
4. [Privacy-Preserving Analytics](#privacy-preserving-analytics)
5. [Reporting Features](#reporting-features)
6. [Feature Status Summary](#feature-status-summary)

---

## Overview

This document catalogs the analytics and reporting features that provide insights into student performance, progress tracking, and learning outcomes. All analytics respect student privacy with k-anonymity enforcement (minimum 5 students).

### Related Documents

- [07-security-privacy.md](07-security-privacy.md) - Privacy compliance and k-anonymity
- [09a-core-assessment-features.md](09a-core-assessment-features.md) - Assessment data sources
- [09b-agent-orchestration-features.md](09b-agent-orchestration-features.md) - Orchestration metrics

---

## Student Analytics

### Performance Summary

#### ✅ Overall Performance API

**Status:** Fully Implemented

Comprehensive performance summary for individual students:

**Endpoint:** `GET /api/v1/students/{studentId}/performance-summary`

**Response:**

```csharp
public record PerformanceSummaryDto
{
    public Guid StudentId { get; init; }
    public int TotalAssessments { get; init; }
    public double AverageScore { get; init; }
    public double OverallAccuracy { get; init; }
    public int TotalQuestionsAttempted { get; init; }
    public TimeSpan TotalTimeSpent { get; init; }
    public DateTime? LastAssessmentDate { get; init; }
    public List<SubjectPerformanceDto> SubjectBreakdown { get; init; }
}
```

**Metrics:**

- Total assessments completed
- Average score across all assessments
- Overall accuracy percentage
- Total questions attempted
- Total time spent on assessments
- Last assessment date
- Subject-wise breakdown

**Privacy:** Individual student data (no aggregation needed)

**Files:**

- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`
- Endpoint implementation at line ~50

**Authorization:** Requires `Student` role or higher

### Subject Performance

#### ✅ Subject-Specific Analytics

**Status:** Fully Implemented (5 Endpoints)

Performance breakdown for each of the 5 subjects:

**Endpoints:**

- `GET /api/v1/students/{studentId}/performance/mathematics`
- `GET /api/v1/students/{studentId}/performance/physics`
- `GET /api/v1/students/{studentId}/performance/chemistry`
- `GET /api/v1/students/{studentId}/performance/biology`
- `GET /api/v1/students/{studentId}/performance/english`

**Response:**

```csharp
public record SubjectPerformanceDto
{
    public string Subject { get; init; }
    public int AssessmentsCompleted { get; init; }
    public int QuestionsAttempted { get; init; }
    public int CorrectAnswers { get; init; }
    public double AccuracyPercentage { get; init; }
    public double AverageScore { get; init; }
    public string PerformanceLevel { get; init; }  // Excellent/Good/Needs Improvement
    public List<string> StrongTopics { get; init; }
    public List<string> WeakTopics { get; init; }
}
```

**Performance Levels:**

- **Excellent:** ≥90% accuracy
- **Good:** 70-89% accuracy
- **Needs Improvement:** <70% accuracy

**Features:**

- Assessments completed per subject
- Questions attempted and correct answers
- Accuracy percentage calculation
- Average score across subject assessments
- Categorized performance level
- Strong and weak topic identification

**Files:**

- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`
- 5 separate endpoint implementations (lines ~100-300)

**Authorization:** Requires `Student` role or higher

### Progress Tracking

#### ✅ Progress Over Time API

**Status:** Fully Implemented

Trend analysis showing improvement or decline over time:

**Endpoint:** `GET /api/v1/students/{studentId}/progress-over-time?days=30`

**Query Parameters:**

- `days` (optional): Number of days to analyze (default: 30)

**Response:**

```csharp
public record ProgressOverTimeDto
{
    public Guid StudentId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public List<ProgressDataPoint> DataPoints { get; init; }
    public string Trend { get; init; }  // Improving/Stable/Declining
    public double TrendPercentage { get; init; }
}

public record ProgressDataPoint
{
    public DateTime Date { get; init; }
    public double Score { get; init; }
    public double Accuracy { get; init; }
    public string Subject { get; init; }
}
```

**Trend Calculation:**

- **Improving:** Score increasing by >5% over period
- **Stable:** Score within ±5% range
- **Declining:** Score decreasing by >5% over period

**Features:**

- Time-series data points
- Trend detection (improving/stable/declining)
- Percentage change calculation
- Subject-wise trend analysis
- Configurable time range

**Files:**

- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`
- Endpoint implementation at line ~350

**Authorization:** Requires `Student` role or higher

### Weak Areas Identification

#### ✅ Weak Areas API

**Status:** Fully Implemented

Identify topics where student needs improvement:

**Endpoint:** `GET /api/v1/students/{studentId}/weak-areas?threshold=70`

**Query Parameters:**

- `threshold` (optional): Accuracy threshold percentage (default: 70)

**Response:**

```csharp
public record WeakAreasDto
{
    public Guid StudentId { get; init; }
    public List<WeakAreaDto> WeakAreas { get; init; }
    public int TotalWeakAreas { get; init; }
}

public record WeakAreaDto
{
    public string Subject { get; init; }
    public string Topic { get; init; }
    public int QuestionsAttempted { get; init; }
    public int CorrectAnswers { get; init; }
    public double AccuracyPercentage { get; init; }
    public int Priority { get; init; }  // 1-5, higher = more urgent
    public List<string> RecommendedResources { get; init; }
}
```

**Priority Calculation:**

```csharp
Priority = 5 if accuracy < 40%
Priority = 4 if accuracy 40-54%
Priority = 3 if accuracy 55-69%
Priority = 2 if accuracy 70-79%
Priority = 1 if accuracy 80-89%
```

**Features:**

- Below-threshold topic identification
- Question attempt count
- Accuracy percentage per topic
- Priority ranking (1-5 scale)
- Recommended learning resources (planned)

**Files:**

- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`
- Endpoint implementation at line ~400

**Authorization:** Requires `Student` role or higher

### Recommended Topics

#### ✅ Recommended Topics API

**Status:** Fully Implemented

Personalized recommendations for next learning topics:

**Endpoint:** `GET /api/v1/students/{studentId}/recommended-topics?count=5`

**Query Parameters:**

- `count` (optional): Number of recommendations (default: 5)

**Response:**

```csharp
public record RecommendedTopicsDto
{
    public Guid StudentId { get; init; }
    public List<RecommendedTopicDto> Recommendations { get; init; }
}

public record RecommendedTopicDto
{
    public string Subject { get; init; }
    public string Topic { get; init; }
    public string Reason { get; init; }  // Why recommended
    public int DifficultyLevel { get; init; }  // 1-5
    public int EstimatedTimeMinutes { get; init; }
    public List<string> Prerequisites { get; init; }
    public List<string> Resources { get; init; }
}
```

**Recommendation Algorithm:**

1. Analyze weak areas (accuracy <70%)
2. Consider prerequisite topics
3. Factor in student's current ability level
4. Balance difficulty progression
5. Align with curriculum standards

**Features:**

- Personalized topic suggestions
- Reason explanation for each recommendation
- Difficulty level indication
- Estimated completion time
- Prerequisite requirements
- Learning resources (planned)

**Files:**

- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`
- Endpoint implementation at line ~450

**Authorization:** Requires `Student` role or higher

---

## Class Analytics

### Class-Wide Performance

#### ⚠️ Class Performance Summary

**Status:** Partially Implemented (Privacy enforcement pending)

Aggregated performance metrics for entire class:

**Endpoint:** `GET /api/v1/classes/{classId}/performance-summary`

**Planned Response:**

```csharp
public record ClassPerformanceSummaryDto
{
    public Guid ClassId { get; init; }
    public string ClassName { get; init; }
    public int TotalStudents { get; init; }
    public int ActiveStudents { get; init; }
    public double AverageScore { get; init; }
    public double MedianScore { get; init; }
    public int TotalAssessmentsCompleted { get; init; }
    public List<SubjectPerformanceDto> SubjectBreakdown { get; init; }
    public List<PerformanceDistribution> ScoreDistribution { get; init; }
}
```

**Privacy Enforcement:**

- **K-Anonymity Check:** Require minimum 5 students
- **Error Response:** If class has <5 students, return 403 Forbidden
- **Message:** "Insufficient students for privacy-preserving analytics (minimum 5 required)"

**Current Status:**

- Endpoint structure defined
- Privacy logic not yet implemented
- Planned for Week 4

**Files:**

- Planned in `src/AcademicAssessment.Web/Controllers/ClassAnalyticsController.cs` (not yet created)

**Authorization:** Requires `Teacher` role or higher

#### 📋 Student Comparison (Anonymized)

**Status:** Planned

Compare individual student to class averages (privacy-preserving):

**Planned Features:**

- Percentile ranking (without revealing exact students)
- Above/below average indicators
- Performance quartiles (Q1, Q2, Q3, Q4)
- Subject-wise class position
- Growth percentile (improvement over time)

**Privacy Considerations:**

- Never show exact ranking if class <10 students
- Use ranges instead of specific positions
- Anonymize all comparative data
- No peer identification possible

#### 📋 Class Trends

**Status:** Planned

Track class performance over academic term:

**Planned Features:**

- Average score trends
- Completion rate trends
- Subject-wise class progress
- Identify struggling topics (class-wide)
- Cohort analysis

---

## Privacy-Preserving Analytics

### K-Anonymity Enforcement

#### ⚠️ K-Anonymity Implementation

**Status:** Defined, Implementation Pending

Privacy protection ensuring no group <5 students analyzed:

**Concept:**

K-anonymity ensures that any individual cannot be distinguished from at least k-1 other individuals in the dataset. For EduMind.AI, k=5 (minimum 5 students required).

**Enforcement Points:**

1. **Class Analytics**
   - Require ≥5 students in class
   - Block queries if class size <5
   - Return error: "Insufficient students for privacy-preserving analytics"

2. **School-Wide Analytics**
   - Require ≥5 students per subject
   - Aggregate across larger groups when possible
   - Suppress data for small cohorts

3. **Demographic Filters**
   - Any filter (grade, subject, difficulty) must result in ≥5 students
   - Block query if filter too specific

**Implementation:**

```csharp
public async Task<Result<ClassAnalytics>> GetClassAnalytics(Guid classId)
{
    var studentCount = await _repository.GetStudentCountAsync(classId);
    
    if (studentCount < 5)
    {
        return Result<ClassAnalytics>.Failure(
            "Insufficient students for privacy-preserving analytics (minimum 5 required)");
    }
    
    // Proceed with analytics
    var analytics = await ComputeClassAnalytics(classId);
    return Result<ClassAnalytics>.Success(analytics);
}
```

**Files:**

- Planned middleware: `src/AcademicAssessment.Web/Middleware/KAnonymityMiddleware.cs`
- Repository methods for student counting

**Compliance:** FERPA, COPPA, GDPR

**See Also:** [07-security-privacy.md](07-security-privacy.md) for complete privacy strategy

### Data Aggregation

#### 📋 Aggregation Rules

**Status:** Planned

Rules for privacy-preserving data aggregation:

**Planned Rules:**

1. **Minimum Group Size:** ≥5 students
2. **Rounding:** Round percentages to nearest 5%
3. **Binning:** Group scores into ranges (e.g., 80-89, 90-100)
4. **Suppression:** Hide data for groups <5
5. **Noise Addition:** Add small random noise to aggregates (optional)

**Example:**

```
❌ BAD:  "Class A (3 students) average: 85.3%"
✅ GOOD: "Class A has insufficient students for reporting"

❌ BAD:  "John scored in 97th percentile"
✅ GOOD: "Student scored in top quartile (75-100%)"
```

---

## Reporting Features

### Student Reports

#### 📋 Individual Progress Report

**Status:** Planned (Week 4)

Comprehensive PDF report for students and parents:

**Planned Sections:**

1. **Summary Page**
   - Student name, ID, class
   - Reporting period
   - Overall performance summary
   - Key achievements

2. **Subject Performance**
   - Score trends per subject
   - Accuracy charts
   - Strengths and weaknesses
   - Comparison to class average (anonymized)

3. **Assessment History**
   - List of completed assessments
   - Scores and dates
   - Time spent
   - Progress indicators

4. **Recommendations**
   - Topics to focus on
   - Suggested learning resources
   - Next assessment suggestions

5. **Skills Assessment**
   - Mastery levels per skill
   - Growth indicators
   - Milestone achievements

**Export Formats:**

- PDF (primary)
- Excel (data export)
- JSON (API integration)

**Files:** Not yet implemented

#### 📋 Parent Portal Reports

**Status:** Planned (Week 5)

Simplified reports for parents:

**Planned Features:**

- Summary dashboard
- Easy-to-understand visualizations
- Progress notifications
- Email reports (weekly/monthly)
- Downloadable PDF reports

### Teacher Reports

#### 📋 Class Performance Report

**Status:** Planned (Week 4)

Comprehensive class-wide analytics:

**Planned Sections:**

1. **Class Overview**
   - Total students, active students
   - Assessment completion rates
   - Average performance

2. **Student Performance Matrix**
   - Grid showing all students (rows) and subjects (columns)
   - Color-coded performance indicators
   - Sort by any column
   - Filter by performance level

3. **Subject Analysis**
   - Strengths and weaknesses by subject
   - Topic-wise performance
   - Difficulty analysis

4. **Trends**
   - Performance over time
   - Improvement/decline indicators
   - Comparative analysis

5. **Recommendations**
   - Students needing intervention
   - Class-wide focus areas
   - Suggested remedial topics

**Privacy:** All reports enforce k-anonymity

**Files:** Not yet implemented

#### 📋 Assessment Analytics

**Status:** Planned (Week 4)

Per-assessment detailed analytics:

**Planned Metrics:**

- Completion rate
- Average score
- Time statistics
- Question difficulty analysis
- Most missed questions
- Discrimination index (how well questions differentiate ability)

### Admin Reports

#### 📋 School-Wide Analytics

**Status:** Planned (Week 5)

System-wide performance dashboards:

**Planned Features:**

1. **School Performance**
   - Overall averages across all classes
   - Grade-level breakdowns
   - Subject-wise trends
   - Year-over-year comparisons

2. **Usage Statistics**
   - Active users (students, teachers)
   - Assessments created and completed
   - System utilization metrics
   - Peak usage times

3. **ROI Metrics**
   - Student improvement rates
   - Time saved vs traditional methods
   - Cost per assessment
   - Teacher satisfaction

4. **Predictive Analytics** (Future)
   - At-risk student identification
   - Performance forecasting
   - Resource allocation recommendations

**Privacy:** All aggregations enforce k-anonymity

**Files:** Not yet implemented

---

## Feature Status Summary

### Completed Features (✅)

**Student Analytics APIs:**

- ✅ Performance summary endpoint (overall metrics)
- ✅ Subject-specific performance (5 endpoints for 5 subjects)
- ✅ Progress over time with trend detection
- ✅ Weak areas identification with priority ranking
- ✅ Recommended topics with algorithm-based suggestions
- ✅ SubjectPerformanceDto model with complete fields
- ✅ Performance level categorization (Excellent/Good/Needs Improvement)

**Authorization:**

- ✅ Role-based access control on all endpoints
- ✅ Student role requirement for personal analytics

**Database:**

- ✅ Data sources: 295 student assessments, 1,179 responses
- ✅ Query infrastructure for analytics

### In Progress (⚠️)

- ⚠️ Class performance summary (endpoint structure defined, privacy enforcement pending)
- ⚠️ K-anonymity implementation (concept defined, middleware pending)

### Planned Features (📋)

**Privacy-Preserving Analytics:**

- 📋 K-anonymity middleware enforcement
- 📋 Aggregation rules implementation
- 📋 Data suppression for small groups
- 📋 Anonymized student comparison

**Class Analytics:**

- 📋 Class performance summary (with k-anonymity)
- 📋 Student comparison (anonymized, percentile-based)
- 📋 Class trends over time
- 📋 Cohort analysis

**Reporting:**

- 📋 Individual progress reports (PDF generation)
- 📋 Parent portal reports (simplified dashboards)
- 📋 Class performance reports (teacher-facing)
- 📋 Assessment analytics (per-assessment insights)
- 📋 School-wide analytics dashboards

**Advanced Analytics:**

- 📋 Predictive analytics (at-risk identification)
- 📋 Performance forecasting
- 📋 Learning path optimization
- 📋 Curriculum effectiveness analysis
- 📋 ROI metrics and reporting

**Visualizations:**

- 📋 Interactive charts (Chart.js or Recharts)
- 📋 Heatmaps for topic mastery
- 📋 Trend lines with forecasting
- 📋 Radar charts for skill profiles
- 📋 Sankey diagrams for learning paths

**Export Formats:**

- 📋 PDF report generation
- 📋 Excel/CSV data export
- 📋 JSON API responses
- 📋 Print-friendly views

---

## Implementation Locations

### Key Files

**API Controllers:**

- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs` (400+ lines)
  - 7 implemented endpoints
  - DTOs: PerformanceSummaryDto, SubjectPerformanceDto, ProgressOverTimeDto, WeakAreasDto, RecommendedTopicsDto

**Planned Controllers:**

- `src/AcademicAssessment.Web/Controllers/ClassAnalyticsController.cs` (not yet created)
- `src/AcademicAssessment.Web/Controllers/SchoolAnalyticsController.cs` (not yet created)

**DTOs:**

- `src/AcademicAssessment.Web/Models/PerformanceSummaryDto.cs`
- `src/AcademicAssessment.Web/Models/SubjectPerformanceDto.cs`
- `src/AcademicAssessment.Web/Models/ProgressOverTimeDto.cs`
- `src/AcademicAssessment.Web/Models/WeakAreasDto.cs`
- `src/AcademicAssessment.Web/Models/RecommendedTopicsDto.cs`

**Repositories:**

- `src/AcademicAssessment.Infrastructure/Repositories/StudentAssessmentRepository.cs`
- `src/AcademicAssessment.Infrastructure/Repositories/StudentResponseRepository.cs`
- Analytics query methods

**Privacy Middleware (Planned):**

- `src/AcademicAssessment.Web/Middleware/KAnonymityMiddleware.cs`

**Report Generation (Planned):**

- `src/AcademicAssessment.Infrastructure/Services/ReportGenerationService.cs`
- PDF generation via iText or similar library

---

## Related Documentation

- **[07-security-privacy.md](07-security-privacy.md)** - K-anonymity and privacy compliance
- **[09a-core-assessment-features.md](09a-core-assessment-features.md)** - Assessment data sources
- **[09b-agent-orchestration-features.md](09b-agent-orchestration-features.md)** - Orchestration metrics
- **[09c-user-interface-features.md](09c-user-interface-features.md)** - Analytics UI dashboards
- **[10-api-reference.md](10-api-reference.md)** - Complete API documentation

---

**Document Status:** Complete  
**Last Review:** October 24, 2025  
**Next Review:** After Week 4 teacher dashboard implementation
