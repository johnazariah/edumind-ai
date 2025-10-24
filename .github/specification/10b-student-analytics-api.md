# Student Analytics API Reference

**Version:** 1.0  
**Base Path:** `/api/v1/students/{studentId:guid}/analytics`  
**Controller:** `StudentAnalyticsController`  
**Source:** `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`

## Table of Contents

- [Overview](#overview)
- [Authentication & Authorization](#authentication--authorization)
- [Endpoints](#endpoints)
  - [1. Get Performance Summary](#1-get-performance-summary)
  - [2. Get Subject Performance](#2-get-subject-performance)
  - [3. Get Learning Objective Mastery](#3-get-learning-objective-mastery)
  - [4. Get Ability Estimates](#4-get-ability-estimates)
  - [5. Get Improvement Areas](#5-get-improvement-areas)
  - [6. Get Progress Timeline](#6-get-progress-timeline)
  - [7. Get Peer Comparison](#7-get-peer-comparison)
- [Data Models](#data-models)
- [Error Responses](#error-responses)
- [Usage Examples](#usage-examples)
- [Implementation Status](#implementation-status)

---

## Overview

The Student Analytics API provides comprehensive performance tracking, progress analysis, and personalized recommendations for students. All endpoints implement privacy-preserving analytics with k-anonymity protection for peer comparisons.

**Key Features:**

- Individual student performance metrics across all subjects
- Subject-specific detailed analysis (accuracy, time spent, topic mastery)
- IRT-based ability estimates for adaptive assessment
- Personalized learning recommendations based on weak areas
- Time-series progress tracking with growth rate calculations
- Privacy-preserving peer comparisons (k-anonymity with minimum 5 peers)
- Role-based access control with multi-tenant isolation

**Content Type:** `application/json`

---

## Authentication & Authorization

All endpoints require authentication via JWT bearer token:

```
Authorization: Bearer <jwt-token>
```

**Authorization Policy:** `AllUsersPolicy` (all authenticated users)

### Access Control Rules

The `CanAccessStudentDataAsync` method enforces fine-grained authorization:

| Role | Access Permissions |
|------|-------------------|
| **Student** | Can only access their own data (`studentId` must match token user ID) |
| **Teacher** | Can access students in their assigned classes within the same school |
| **SchoolAdmin** | Can access all students in their school |
| **CourseAdmin** | Can access all students across all schools |
| **SystemAdmin** | Full access to all student data |
| **BusinessAdmin** | Full access to all student data |

**403 Forbidden** is returned when:

- Student attempts to access another student's data
- Teacher attempts to access student outside their classes
- Teacher/SchoolAdmin attempts to access student from different school
- User lacks required role permissions

---

## Endpoints

### 1. Get Performance Summary

**Endpoint:** `GET /api/v1/students/{studentId}/analytics/performance-summary`

**Description:** Retrieves overall performance summary for a student across all subjects.

**Path Parameters:**

- `studentId` (guid, required) - The unique identifier of the student

**Response:** `200 OK`

```json
{
  "studentId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "totalAssessmentsTaken": 47,
  "averageScore": 78.5,
  "overallMastery": 0.72,
  "subjectScores": {
    "Mathematics": 82.3,
    "Physics": 76.8,
    "Chemistry": 79.1,
    "Biology": 75.2,
    "English": 79.0
  },
  "totalTimeSpent": "PT45H30M",
  "lastAssessmentDate": "2025-01-20T15:30:00Z",
  "currentStreak": 5
}
```

**Response Fields:**

- `studentId` (guid) - Student identifier
- `totalAssessmentsTaken` (int) - Number of completed assessments
- `averageScore` (double) - Average score across all assessments (0-100)
- `overallMastery` (double) - Overall mastery level (0.0-1.0)
- `subjectScores` (dictionary) - Average scores by subject
- `totalTimeSpent` (TimeSpan) - ISO 8601 duration format
- `lastAssessmentDate` (DateTimeOffset) - ISO 8601 timestamp
- `currentStreak` (int) - Consecutive days with activity

**Error Responses:**

- `403 Forbidden` - User doesn't have access to this student's data
- `404 Not Found` - Student not found or no assessment data available

---

### 2. Get Subject Performance

**Endpoint:** `GET /api/v1/students/{studentId}/analytics/subject-performance`

**Description:** Retrieves detailed performance metrics for a specific subject or aggregated across all subjects.

**Path Parameters:**

- `studentId` (guid, required) - The unique identifier of the student

**Query Parameters:**

- `subject` (enum, optional) - Subject filter (Mathematics, Physics, Chemistry, Biology, English). If omitted, defaults to Mathematics for backward compatibility.

**Response:** `200 OK`

```json
{
  "subject": "Mathematics",
  "assessmentsTaken": 12,
  "averageScore": 82.3,
  "masteryLevel": 0.78,
  "abilityEstimate": 1.45,
  "questionsAnswered": 180,
  "questionsCorrect": 148,
  "accuracyRate": 0.8222,
  "averageTimePerQuestion": "PT2M15S",
  "strongTopics": [
    "Algebra - Linear Equations",
    "Geometry - Triangles",
    "Calculus - Derivatives"
  ],
  "weakTopics": [
    "Statistics - Probability",
    "Algebra - Quadratic Equations"
  ]
}
```

**Response Fields:**

- `subject` (Subject enum) - The subject analyzed
- `assessmentsTaken` (int) - Number of assessments completed for this subject
- `averageScore` (double) - Average score (0-100)
- `masteryLevel` (double) - Subject mastery level (0.0-1.0)
- `abilityEstimate` (double) - IRT ability estimate (-3 to +3 scale)
- `questionsAnswered` (int) - Total questions attempted
- `questionsCorrect` (int) - Total correct answers
- `accuracyRate` (double) - Percentage correct (0.0-1.0)
- `averageTimePerQuestion` (TimeSpan) - ISO 8601 duration
- `strongTopics` (string array) - Topics with high mastery
- `weakTopics` (string array) - Topics needing improvement

**Error Responses:**

- `400 Bad Request` - Invalid subject parameter
- `403 Forbidden` - User doesn't have access
- `404 Not Found` - No assessment data for specified subject

---

### 3. Get Learning Objective Mastery

**Endpoint:** `GET /api/v1/students/{studentId}/analytics/recommended-topics`

**Description:** Returns recommended learning objectives for a student to study based on performance analysis.

**Path Parameters:**

- `studentId` (guid, required) - The unique identifier of the student

**Query Parameters:**

- `subject` (enum, optional) - Subject filter (if omitted, returns all subjects)

**Response:** `200 OK`

```json
[
  {
    "learningObjective": "Solve quadratic equations using factoring",
    "subject": "Mathematics",
    "masteryLevel": 0.45,
    "timesAssessed": 8,
    "timesCorrect": 3,
    "lastAssessedAt": "2025-01-18T10:15:00Z",
    "status": "Developing"
  },
  {
    "learningObjective": "Calculate probability of compound events",
    "subject": "Mathematics",
    "masteryLevel": 0.52,
    "timesAssessed": 5,
    "timesCorrect": 2,
    "lastAssessedAt": "2025-01-20T14:30:00Z",
    "status": "Developing"
  },
  {
    "learningObjective": "Apply Newton's laws to solve motion problems",
    "subject": "Physics",
    "masteryLevel": 0.88,
    "timesAssessed": 12,
    "timesCorrect": 11,
    "lastAssessedAt": "2025-01-19T09:45:00Z",
    "status": "Proficient"
  }
]
```

**Response Fields:**

- `learningObjective` (string) - Description of the learning objective
- `subject` (Subject enum) - Subject area
- `masteryLevel` (double) - Current mastery (0.0-1.0)
- `timesAssessed` (int) - Number of times evaluated
- `timesCorrect` (int) - Number of correct attempts
- `lastAssessedAt` (DateTimeOffset) - ISO 8601 timestamp
- `status` (MasteryStatus enum) - NotStarted, Beginning, Developing, Proficient, Advanced, Mastered

**Error Responses:**

- `403 Forbidden` - User doesn't have access
- `404 Not Found` - No data available for recommendations

---

### 4. Get Ability Estimates

**Endpoint:** `GET /api/v1/students/{studentId}/analytics/ability-estimates`

**Description:** Retrieves IRT-based ability estimates for a student across all subjects.

**Path Parameters:**

- `studentId` (guid, required) - The unique identifier of the student

**Response:** `200 OK`

```json
{
  "Mathematics": 1.45,
  "Physics": 0.87,
  "Chemistry": 1.12,
  "Biology": 0.65,
  "English": 1.03
}
```

**Response Format:** Dictionary mapping Subject enum to ability estimate (double)

**Ability Estimate Scale:**

- `-3 to -2`: Significantly below grade level
- `-2 to -1`: Below grade level
- `-1 to +1`: At grade level
- `+1 to +2`: Above grade level
- `+2 to +3`: Significantly above grade level

**Note:** Ability estimates are calculated using Item Response Theory (IRT) based on question difficulty, discrimination, and guessing parameters. Higher values indicate higher ability.

**Error Responses:**

- `403 Forbidden` - User doesn't have access
- `404 Not Found` - Insufficient data to calculate estimates (requires minimum assessment history)

---

### 5. Get Improvement Areas

**Endpoint:** `GET /api/v1/students/{studentId}/analytics/weak-areas`

**Description:** Returns priority-ordered list of areas where the student needs improvement.

**Path Parameters:**

- `studentId` (guid, required) - The unique identifier of the student

**Query Parameters:**

- `topN` (int, optional, default: 5) - Number of top weak areas to return (1-20)

**Response:** `200 OK`

```json
[
  {
    "subject": "Mathematics",
    "topic": "Quadratic Equations",
    "learningObjective": "Solve quadratic equations using the quadratic formula",
    "currentMastery": 0.35,
    "targetMastery": 0.75,
    "questionsAttempted": 12,
    "accuracyRate": 0.33,
    "recommendedAction": "Review fundamentals of factoring and discriminant calculation. Practice 10-15 problems with step-by-step solutions.",
    "priority": "High"
  },
  {
    "subject": "Physics",
    "topic": "Kinematics",
    "learningObjective": "Calculate velocity and acceleration from displacement-time graphs",
    "currentMastery": 0.48,
    "targetMastery": 0.75,
    "questionsAttempted": 8,
    "accuracyRate": 0.50,
    "recommendedAction": "Practice graph interpretation and slope calculations. Focus on understanding the relationship between displacement, velocity, and acceleration.",
    "priority": "Medium"
  }
]
```

**Response Fields:**

- `subject` (Subject enum) - Subject area
- `topic` (string) - High-level topic name
- `learningObjective` (string) - Specific learning objective
- `currentMastery` (double) - Current mastery level (0.0-1.0)
- `targetMastery` (double) - Target mastery level (0.0-1.0)
- `questionsAttempted` (int) - Number of questions tried
- `accuracyRate` (double) - Percentage correct (0.0-1.0)
- `recommendedAction` (string) - Personalized recommendation
- `priority` (PriorityLevel enum) - Low, Medium, High, Critical

**Priority Calculation:**

- `Critical`: Current mastery < 0.3 and ≥10 questions attempted
- `High`: Current mastery < 0.5 and ≥8 questions attempted
- `Medium`: Current mastery < 0.7 and ≥5 questions attempted
- `Low`: All other cases

**Error Responses:**

- `400 Bad Request` - Invalid topN parameter (must be 1-20)
- `403 Forbidden` - User doesn't have access
- `404 Not Found` - No data available to identify weak areas

---

### 6. Get Progress Timeline

**Endpoint:** `GET /api/v1/students/{studentId}/analytics/progress-over-time`

**Description:** Retrieves time-series progress data for a student with growth rate calculations.

**Path Parameters:**

- `studentId` (guid, required) - The unique identifier of the student

**Query Parameters:**

- `startDate` (DateTimeOffset, optional) - Start date for timeline (ISO 8601 format). If omitted, uses earliest assessment date.
- `endDate` (DateTimeOffset, optional) - End date for timeline (ISO 8601 format). If omitted, uses current date.

**Response:** `200 OK`

```json
{
  "studentId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "startDate": "2024-09-01T00:00:00Z",
  "endDate": "2025-01-20T23:59:59Z",
  "dataPoints": [
    {
      "date": "2024-09-05T14:30:00Z",
      "subject": "Mathematics",
      "score": 65.0,
      "masteryLevel": 0.55,
      "assessmentType": "Diagnostic"
    },
    {
      "date": "2024-09-12T10:15:00Z",
      "subject": "Mathematics",
      "score": 72.0,
      "masteryLevel": 0.62,
      "assessmentType": "Practice"
    },
    {
      "date": "2024-09-19T15:45:00Z",
      "subject": "Mathematics",
      "score": 78.0,
      "masteryLevel": 0.70,
      "assessmentType": "Practice"
    },
    {
      "date": "2024-10-03T09:00:00Z",
      "subject": "Mathematics",
      "score": 82.0,
      "masteryLevel": 0.76,
      "assessmentType": "Summative"
    }
  ],
  "averageGrowthRate": 0.045,
  "subjectGrowthRates": {
    "Mathematics": 0.052,
    "Physics": 0.038,
    "Chemistry": 0.041,
    "Biology": 0.047,
    "English": 0.043
  }
}
```

**Response Fields:**

- `studentId` (guid) - Student identifier
- `startDate` (DateTimeOffset) - Timeline start date
- `endDate` (DateTimeOffset) - Timeline end date
- `dataPoints` (array) - Chronologically ordered assessment results
  - `date` (DateTimeOffset) - Assessment completion time
  - `subject` (Subject enum) - Subject assessed
  - `score` (double) - Score achieved (0-100)
  - `masteryLevel` (double) - Mastery level (0.0-1.0)
  - `assessmentType` (AssessmentType enum) - Diagnostic, Practice, Summative, or Adaptive
- `averageGrowthRate` (double) - Average mastery growth per week across all subjects
- `subjectGrowthRates` (dictionary) - Growth rates by subject (mastery points per week)

**Growth Rate Calculation:**
Growth rate = (Final Mastery - Initial Mastery) / Number of Weeks

**Error Responses:**

- `400 Bad Request` - Invalid date range (startDate after endDate)
- `403 Forbidden` - User doesn't have access
- `404 Not Found` - No assessment data for specified date range

---

### 7. Get Peer Comparison

**Endpoint:** `GET /api/v1/students/{studentId}/analytics/peer-comparison`

**Description:** Retrieves privacy-preserving peer comparison data with k-anonymity protection.

**Path Parameters:**

- `studentId` (guid, required) - The unique identifier of the student

**Query Parameters:**

- `gradeLevel` (GradeLevel enum, optional) - Grade level for peer comparison (Grade6-Grade12). If omitted, uses student's current grade.
- `subject` (Subject enum, optional) - Subject filter for comparison. If omitted, compares overall performance.

**Response:** `200 OK`

```json
{
  "studentId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "studentScore": 82.3,
  "peerAverageScore": 74.5,
  "peerMedianScore": 76.0,
  "percentile": 78,
  "peerGroupSize": 127,
  "gradeLevel": "Grade10",
  "subject": "Mathematics",
  "meetsKAnonymity": true
}
```

**Response Fields:**

- `studentId` (guid) - Student identifier
- `studentScore` (double) - Student's average score (0-100)
- `peerAverageScore` (double) - Peer group average (0-100)
- `peerMedianScore` (double) - Peer group median (0-100)
- `percentile` (int) - Student's percentile rank (0-100)
- `peerGroupSize` (int) - Number of students in comparison group
- `gradeLevel` (GradeLevel enum) - Grade level for comparison
- `subject` (Subject enum, nullable) - Subject compared (null for overall)
- `meetsKAnonymity` (bool) - True if minimum 5 peers available

**K-Anonymity Protection:**

- Minimum 5 students required in peer group
- If fewer than 5 peers, returns 404 Not Found
- Protects individual student privacy by ensuring sufficient group size
- Peer data aggregated across same grade level and school

**Percentile Interpretation:**

- `90-100`: Top 10% of peer group
- `75-89`: Upper quartile
- `50-74`: Above median
- `25-49`: Below median
- `0-24`: Lower quartile

**Error Responses:**

- `400 Bad Request` - Invalid grade level or subject parameter
- `403 Forbidden` - User doesn't have access
- `404 Not Found` - Insufficient peer data (k-anonymity threshold not met, <5 students)

---

## Data Models

### StudentPerformanceSummary

```csharp
public record StudentPerformanceSummary
{
    public required Guid StudentId { get; init; }
    public required int TotalAssessmentsTaken { get; init; }
    public required double AverageScore { get; init; }
    public required double OverallMastery { get; init; }
    public required IReadOnlyDictionary<Subject, double> SubjectScores { get; init; }
    public required TimeSpan TotalTimeSpent { get; init; }
    public required DateTimeOffset LastAssessmentDate { get; init; }
    public required int CurrentStreak { get; init; }
}
```

### SubjectPerformance

```csharp
public record SubjectPerformance
{
    public required Subject Subject { get; init; }
    public required int AssessmentsTaken { get; init; }
    public required double AverageScore { get; init; }
    public required double MasteryLevel { get; init; }
    public required double AbilityEstimate { get; init; }
    public required int QuestionsAnswered { get; init; }
    public required int QuestionsCorrect { get; init; }
    public required double AccuracyRate { get; init; }
    public required TimeSpan AverageTimePerQuestion { get; init; }
    public required IReadOnlyList<string> StrongTopics { get; init; }
    public required IReadOnlyList<string> WeakTopics { get; init; }
}
```

### LearningObjectiveMastery

```csharp
public record LearningObjectiveMastery
{
    public required string LearningObjective { get; init; }
    public required Subject Subject { get; init; }
    public required double MasteryLevel { get; init; }
    public required int TimesAssessed { get; init; }
    public required int TimesCorrect { get; init; }
    public required DateTimeOffset LastAssessedAt { get; init; }
    public required MasteryStatus Status { get; init; }
}
```

### ImprovementArea

```csharp
public record ImprovementArea
{
    public required Subject Subject { get; init; }
    public required string Topic { get; init; }
    public required string LearningObjective { get; init; }
    public required double CurrentMastery { get; init; }
    public required double TargetMastery { get; init; }
    public required int QuestionsAttempted { get; init; }
    public required double AccuracyRate { get; init; }
    public required string RecommendedAction { get; init; }
    public required PriorityLevel Priority { get; init; }
}
```

### ProgressTimeline

```csharp
public record ProgressTimeline
{
    public required Guid StudentId { get; init; }
    public required DateTimeOffset StartDate { get; init; }
    public required DateTimeOffset EndDate { get; init; }
    public required IReadOnlyList<ProgressDataPoint> DataPoints { get; init; }
    public required double AverageGrowthRate { get; init; }
    public required IReadOnlyDictionary<Subject, double> SubjectGrowthRates { get; init; }
}
```

### ProgressDataPoint

```csharp
public record ProgressDataPoint
{
    public required DateTimeOffset Date { get; init; }
    public required Subject Subject { get; init; }
    public required double Score { get; init; }
    public required double MasteryLevel { get; init; }
    public required AssessmentType AssessmentType { get; init; }
}
```

### PeerComparison

```csharp
public record PeerComparison
{
    public required Guid StudentId { get; init; }
    public required double StudentScore { get; init; }
    public required double PeerAverageScore { get; init; }
    public required double PeerMedianScore { get; init; }
    public required int Percentile { get; init; }
    public required int PeerGroupSize { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public required Subject? Subject { get; init; }
    public required bool MeetsKAnonymity { get; init; }
}
```

### Supporting Enums

```csharp
public enum MasteryStatus
{
    NotStarted,
    Beginning,
    Developing,
    Proficient,
    Advanced,
    Mastered
}

public enum PriorityLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum Subject
{
    Mathematics,
    Physics,
    Chemistry,
    Biology,
    English
}

public enum GradeLevel
{
    Grade6,
    Grade7,
    Grade8,
    Grade9,
    Grade10,
    Grade11,
    Grade12
}

public enum AssessmentType
{
    Diagnostic,
    Practice,
    Summative,
    Adaptive
}
```

---

## Error Responses

All endpoints return consistent error response format:

### 400 Bad Request

```json
{
  "error": "topN must be between 1 and 20"
}
```

**Common Causes:**

- Invalid query parameters (topN out of range, invalid date range)
- Invalid enum values (subject, gradeLevel)

### 403 Forbidden

**Response:** Empty body with HTTP 403 status

**Causes:**

- Student attempting to access another student's data
- Teacher accessing student outside their classes
- Teacher/SchoolAdmin accessing student from different school
- User lacking required role permissions

### 404 Not Found

```json
{
  "error": "No assessment data available for the specified subject"
}
```

**Common Causes:**

- Student not found
- No assessment history for student
- No data for specified subject or date range
- Peer comparison: insufficient peers (<5 students, k-anonymity threshold not met)
- Insufficient data to calculate ability estimates or recommendations

---

## Usage Examples

### Example 1: Student Viewing Their Own Performance

**Request:**

```http
GET /api/v1/students/a1b2c3d4-e5f6-7890-1234-567890abcdef/analytics/performance-summary
Authorization: Bearer <student-jwt-token>
```

**Response:** `200 OK`

```json
{
  "studentId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "totalAssessmentsTaken": 47,
  "averageScore": 78.5,
  "overallMastery": 0.72,
  "subjectScores": {
    "Mathematics": 82.3,
    "Physics": 76.8,
    "Chemistry": 79.1,
    "Biology": 75.2,
    "English": 79.0
  },
  "totalTimeSpent": "PT45H30M",
  "lastAssessmentDate": "2025-01-20T15:30:00Z",
  "currentStreak": 5
}
```

### Example 2: Teacher Viewing Student's Math Performance

**Request:**

```http
GET /api/v1/students/a1b2c3d4-e5f6-7890-1234-567890abcdef/analytics/subject-performance?subject=Mathematics
Authorization: Bearer <teacher-jwt-token>
```

**Response:** `200 OK`

```json
{
  "subject": "Mathematics",
  "assessmentsTaken": 12,
  "averageScore": 82.3,
  "masteryLevel": 0.78,
  "abilityEstimate": 1.45,
  "questionsAnswered": 180,
  "questionsCorrect": 148,
  "accuracyRate": 0.8222,
  "averageTimePerQuestion": "PT2M15S",
  "strongTopics": [
    "Algebra - Linear Equations",
    "Geometry - Triangles"
  ],
  "weakTopics": [
    "Statistics - Probability",
    "Algebra - Quadratic Equations"
  ]
}
```

### Example 3: Teacher Identifying Student's Top 3 Weak Areas

**Request:**

```http
GET /api/v1/students/a1b2c3d4-e5f6-7890-1234-567890abcdef/analytics/weak-areas?topN=3
Authorization: Bearer <teacher-jwt-token>
```

**Response:** `200 OK`

```json
[
  {
    "subject": "Mathematics",
    "topic": "Quadratic Equations",
    "learningObjective": "Solve quadratic equations using the quadratic formula",
    "currentMastery": 0.35,
    "targetMastery": 0.75,
    "questionsAttempted": 12,
    "accuracyRate": 0.33,
    "recommendedAction": "Review fundamentals of factoring and discriminant calculation",
    "priority": "High"
  },
  {
    "subject": "Physics",
    "topic": "Kinematics",
    "learningObjective": "Calculate velocity from displacement-time graphs",
    "currentMastery": 0.48,
    "targetMastery": 0.75,
    "questionsAttempted": 8,
    "accuracyRate": 0.50,
    "recommendedAction": "Practice graph interpretation and slope calculations",
    "priority": "Medium"
  },
  {
    "subject": "Chemistry",
    "topic": "Chemical Bonding",
    "learningObjective": "Identify types of chemical bonds",
    "currentMastery": 0.52,
    "targetMastery": 0.75,
    "questionsAttempted": 10,
    "accuracyRate": 0.60,
    "recommendedAction": "Review electronegativity and Lewis structures",
    "priority": "Medium"
  }
]
```

### Example 4: School Admin Viewing Progress Over Custom Date Range

**Request:**

```http
GET /api/v1/students/a1b2c3d4-e5f6-7890-1234-567890abcdef/analytics/progress-over-time?startDate=2024-09-01T00:00:00Z&endDate=2024-12-31T23:59:59Z
Authorization: Bearer <admin-jwt-token>
```

**Response:** `200 OK`

```json
{
  "studentId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "startDate": "2024-09-01T00:00:00Z",
  "endDate": "2024-12-31T23:59:59Z",
  "dataPoints": [
    {
      "date": "2024-09-05T14:30:00Z",
      "subject": "Mathematics",
      "score": 65.0,
      "masteryLevel": 0.55,
      "assessmentType": "Diagnostic"
    },
    {
      "date": "2024-12-18T10:00:00Z",
      "subject": "Mathematics",
      "score": 82.0,
      "masteryLevel": 0.76,
      "assessmentType": "Summative"
    }
  ],
  "averageGrowthRate": 0.045,
  "subjectGrowthRates": {
    "Mathematics": 0.052,
    "Physics": 0.038,
    "Chemistry": 0.041,
    "Biology": 0.047,
    "English": 0.043
  }
}
```

### Example 5: Privacy-Preserving Peer Comparison

**Request:**

```http
GET /api/v1/students/a1b2c3d4-e5f6-7890-1234-567890abcdef/analytics/peer-comparison?gradeLevel=Grade10&subject=Mathematics
Authorization: Bearer <student-jwt-token>
```

**Response:** `200 OK`

```json
{
  "studentId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "studentScore": 82.3,
  "peerAverageScore": 74.5,
  "peerMedianScore": 76.0,
  "percentile": 78,
  "peerGroupSize": 127,
  "gradeLevel": "Grade10",
  "subject": "Mathematics",
  "meetsKAnonymity": true
}
```

### Example 6: Unauthorized Access Attempt

**Request:**

```http
GET /api/v1/students/b2c3d4e5-f6g7-8901-2345-678901bcdefg/analytics/performance-summary
Authorization: Bearer <student-a-jwt-token>
```

**Response:** `403 Forbidden`
(Empty body)

**Reason:** Student A attempting to access Student B's data

---

## Implementation Status

### ✅ Fully Implemented

1. **Authentication & Authorization**
   - JWT bearer token validation
   - `AllUsersPolicy` enforcement
   - `CanAccessStudentDataAsync` method with role-based access control
   - Multi-tenant isolation via `ITenantContext`

2. **Endpoint Routing**
   - All 7 endpoints properly routed
   - API versioning (v1.0)
   - Proper HTTP method bindings

3. **Service Integration**
   - `IStudentAnalyticsService` interface fully defined
   - All 7 service methods declared with Result<T> pattern

4. **Data Models**
   - 7 response DTOs fully defined (StudentPerformanceSummary, SubjectPerformance, LearningObjectiveMastery, ImprovementArea, ProgressTimeline, ProgressDataPoint, PeerComparison)
   - 2 supporting enums (MasteryStatus, PriorityLevel)

5. **Error Handling**
   - Consistent error response format
   - 400, 403, 404 status codes
   - Structured logging for all error cases

6. **Logging**
   - Structured logging with context (userId, studentId, parameters)
   - Warning logs for unauthorized access attempts
   - Error logs for service failures

### 🚧 Partially Implemented

**Service Implementation (`StudentAnalyticsService`):**

- Interface fully defined in `IStudentAnalyticsService.cs`
- Implementation exists but service logic details not examined in this specification
- Likely requires database queries, IRT calculations, and analytics algorithms

**Key Implementation Areas (likely in service layer):**

1. IRT ability estimate calculations (3-parameter logistic model)
2. Mastery level calculations (based on accuracy and question difficulty)
3. Growth rate calculations (linear regression on time-series data)
4. Recommendation algorithms (priority scoring for weak areas)
5. K-anonymity enforcement (peer comparison minimum threshold)
6. Multi-tenant data filtering (school and class isolation)

### 📝 Known TODOs

**From Source Code:**

- Line 122: Comment "// In the future, we might want to aggregate all subjects or return an error" - Currently defaults to Mathematics if subject not specified in GetSubjectPerformance

**Testing Requirements:**

1. Unit tests for authorization logic (CanAccessStudentDataAsync)
2. Integration tests for IRT calculations
3. Load tests for analytics queries (potential performance bottleneck)
4. Privacy tests for k-anonymity enforcement

**Future Enhancements:**

1. Caching for frequently accessed analytics (Redis)
2. Aggregated analytics across all subjects for GetSubjectPerformance
3. Real-time analytics updates via SignalR
4. Export analytics reports (PDF, CSV)
5. Comparative analytics (class-level, school-level)
6. Longitudinal trend detection (detecting plateaus, regressions)

---

## Related Documentation

- **Assessment API**: `.github/specification/10a-assessment-api.md`
- **Security & Privacy**: `.github/specification/07-security-privacy.md` (k-anonymity, data protection)
- **Analytics Features**: `.github/specification/09d-analytics-reporting-features.md`
- **Domain Model**: `.github/specification/03-domain-model.md` (Student, Assessment entities)
- **Data Storage**: `.github/specification/05-data-storage.md` (PostgreSQL schema)

---

**Document Status:** Complete  
**Last Updated:** 2025-01-20  
**Version:** 1.0  
**Contributors:** GitHub Copilot
