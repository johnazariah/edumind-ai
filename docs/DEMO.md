# EduMind.AI End-to-End Demo

This document provides a complete walkthrough of the EduMind.AI Academic Assessment Platform, demonstrating all features with realistic test data.

## Prerequisites

1. ✅ PostgreSQL database running (`docker-compose up -d`)
2. ✅ Database migrations applied
3. ✅ Demo data seeded (`./scripts/seed-demo-data.sh`)
4. ✅ API server running (`dotnet run --project src/AcademicAssessment.Web`)

## Demo Data Overview

The seed script creates a realistic academic environment:

### 🏫 **3 Schools**

- **Lincoln High School** (Central District)
- **Washington Academy** (North District)  
- **Roosevelt Institute** (South District)

### 👥 **31 Users**

- **1 System Administrator** - Full platform access
- **3 School Administrators** - One per school
- **6 Teachers** - Two per school (Math & Science)
- **24 Students** - Eight per school

### 📚 **6 Courses**

- Mathematics: Algebra I, Algebra II, Pre-Calculus
- Physics: Physics I
- Chemistry: Chemistry I
- Biology: Biology I

### 📝 **13 Assessment Templates**

- 183 total questions across all assessments
- Difficulty levels from 0 (Easy) to 4 (Very Hard)
- Multiple-choice questions with detailed learning objectives

### 📊 **Student Performance Data**

- 80-100 student assessment attempts
- 1500+ student responses
- Realistic scores (60-95%)
- Time-stamped data over last 30 days

## Authentication

### Development Mode (Current)

Authentication is disabled in development. The API uses `TenantContextDevelopment` with stubbed user data.

```bash
# Test any endpoint without authentication
curl http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/performance-summary
```

### Production Mode (With Azure AD B2C)

Once Azure AD B2C is configured, authentication will be required. Use the test script:

```bash
# Acquire JWT token and test endpoints
./scripts/test-auth.sh
```

## Test User Credentials

### Student Account

```
Email: emma.wilson@lincoln.edu
User ID: 20000000-0000-0000-0000-000000000101
Student ID: 40000000-0000-0000-0000-000000000001
School: Lincoln High School
Grade: 10
Classes: Grade 10 Math, Grade 10 Physics
```

### Teacher Account

```
Email: math.teacher@lincoln.edu
User ID: 20000000-0000-0000-0000-000000000021
School: Lincoln High School
Classes: Grade 10 Math - Period 1
Students: 8 students in class
```

### School Admin Account

```
Email: admin@lincoln.edu
User ID: 20000000-0000-0000-0000-000000000011
School: Lincoln High School
Access: All students and classes in Lincoln High
```

### System Admin Account

```
Email: admin@edumind.ai
User ID: 20000000-0000-0000-0000-000000000001
Access: All schools, all students, all data
```

## API Endpoints Demo

### Base URL

```
http://localhost:5000/api/v1
```

### 1. Performance Summary

**Get overall performance metrics for a student**

```bash
# Emma Wilson (Lincoln High - Grade 10)
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/performance-summary" \
  -H "accept: application/json" | jq '.'
```

**Expected Response:**

```json
{
  "studentId": "40000000-0000-0000-0000-000000000001",
  "assessmentsTaken": 4,
  "averageScore": 82.5,
  "totalTimeSpentMinutes": 180,
  "subjectScores": {
    "Mathematics": 85.0,
    "Physics": 80.0
  },
  "currentStreak": 3,
  "lastAssessmentDate": "2024-10-05T14:30:00Z"
}
```

**What it shows:**

- Total assessments taken
- Average score across all subjects
- Time investment
- Performance by subject
- Engagement metrics (streak, last activity)

---

### 2. Subject Performance

**Get detailed performance for a specific subject**

```bash
# Mathematics performance for Emma Wilson
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/subject-performance?subject=0" \
  -H "accept: application/json" | jq '.'
```

**Subject Enum Values:**

- 0 = Mathematics
- 1 = Physics
- 2 = Chemistry
- 3 = Biology
- 4 = English

**Expected Response:**

```json
{
  "studentId": "40000000-0000-0000-0000-000000000001",
  "subject": "Mathematics",
  "averageScore": 85.0,
  "assessmentCount": 3,
  "totalQuestions": 45,
  "correctAnswers": 38,
  "accuracy": 84.4,
  "averageTimePerQuestion": 90,
  "trend": "Improving",
  "recentScores": [78, 85, 92]
}
```

**What it shows:**

- Subject-specific average score
- Question-level accuracy
- Time efficiency
- Performance trend (Improving/Stable/Declining)
- Recent assessment scores

---

### 3. Learning Objectives Mastery

**Get mastery levels for learning objectives**

```bash
# All learning objectives for Emma Wilson
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/learning-objectives" \
  -H "accept: application/json" | jq '.'

# Filter by subject (Mathematics)
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/learning-objectives?subject=0" \
  -H "accept: application/json" | jq '.'
```

**Expected Response:**

```json
[
  {
    "learningObjective": "Solve quadratic equations using factoring",
    "subject": "Mathematics",
    "masteryLevel": 0.85,
    "questionsAttempted": 12,
    "questionsCorrect": 10,
    "averageScore": 83.3,
    "lastPracticed": "2024-10-05T14:30:00Z"
  },
  {
    "learningObjective": "Graph polynomial functions",
    "subject": "Mathematics",
    "masteryLevel": 0.72,
    "questionsAttempted": 8,
    "questionsCorrect": 6,
    "averageScore": 75.0,
    "lastPracticed": "2024-10-03T10:15:00Z"
  }
]
```

**What it shows:**

- Specific skills and concept mastery
- Granular performance tracking
- Practice frequency
- Areas of strength and weakness

---

### 4. Ability Estimates (IRT)

**Get Item Response Theory (IRT) ability estimates**

```bash
# Ability estimates across all subjects
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/ability-estimates" \
  -H "accept: application/json" | jq '.'
```

**Expected Response:**

```json
{
  "Mathematics": 1.25,
  "Physics": 0.85,
  "Chemistry": null,
  "Biology": null
}
```

**Ability Scale:**

- **-3.0 to -1.0**: Below grade level
- **-1.0 to 1.0**: At grade level
- **1.0 to 3.0**: Above grade level

**What it shows:**

- Standardized ability measurement
- Cross-subject comparison
- Adaptive assessment readiness
- Null values indicate insufficient data

---

### 5. Improvement Areas

**Get recommended areas for improvement**

```bash
# Top 5 improvement areas (default)
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/improvement-areas" \
  -H "accept: application/json" | jq '.'

# Top 10 improvement areas
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/improvement-areas?topN=10" \
  -H "accept: application/json" | jq '.'
```

**Expected Response:**

```json
[
  {
    "learningObjective": "Solve systems of equations using substitution",
    "subject": "Mathematics",
    "currentMastery": 0.45,
    "targetMastery": 0.80,
    "priority": "High",
    "recommendedPracticeQuestions": 15,
    "estimatedPracticeTimeMinutes": 45
  },
  {
    "learningObjective": "Apply Newton's Second Law",
    "subject": "Physics",
    "currentMastery": 0.58,
    "targetMastery": 0.80,
    "priority": "Medium",
    "recommendedPracticeQuestions": 10,
    "estimatedPracticeTimeMinutes": 30
  }
]
```

**What it shows:**

- Prioritized learning gaps
- Actionable recommendations
- Estimated time investment
- Clear target goals

---

### 6. Progress Timeline

**Get performance over time**

```bash
# Last 90 days (default)
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/progress-timeline" \
  -H "accept: application/json" | jq '.'

# Custom date range
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/progress-timeline?startDate=2024-09-01&endDate=2024-10-15" \
  -H "accept: application/json" | jq '.'
```

**Expected Response:**

```json
{
  "studentId": "40000000-0000-0000-0000-000000000001",
  "startDate": "2024-07-15T00:00:00Z",
  "endDate": "2024-10-15T00:00:00Z",
  "dataPoints": [
    {
      "date": "2024-09-15T00:00:00Z",
      "averageScore": 78.0,
      "assessmentCount": 1,
      "subjectScores": {
        "Mathematics": 78.0
      }
    },
    {
      "date": "2024-09-28T00:00:00Z",
      "averageScore": 81.5,
      "assessmentCount": 2,
      "subjectScores": {
        "Mathematics": 85.0,
        "Physics": 78.0
      }
    },
    {
      "date": "2024-10-05T00:00:00Z",
      "averageScore": 85.0,
      "assessmentCount": 1,
      "subjectScores": {
        "Mathematics": 85.0
      }
    }
  ],
  "overallTrend": "Improving",
  "improvementRate": 0.15
}
```

**What it shows:**

- Performance trajectory over time
- Assessment frequency
- Subject-specific trends
- Quantified improvement rate

---

### 7. Peer Comparison

**Compare student performance to peers**

```bash
# Compare within same grade level
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/peer-comparison?gradeLevel=10" \
  -H "accept: application/json" | jq '.'

# Compare by grade level and subject
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/peer-comparison?gradeLevel=10&subject=0" \
  -H "accept: application/json" | jq '.'
```

**Expected Response:**

```json
{
  "studentId": "40000000-0000-0000-0000-000000000001",
  "studentScore": 85.0,
  "gradeLevel": "Grade10",
  "subject": "Mathematics",
  "peerAverageScore": 78.5,
  "percentileRank": 75,
  "peerCount": 8,
  "schoolRank": 2,
  "districtRank": null,
  "performance": "AboveAverage"
}
```

**Performance Levels:**

- **BelowAverage**: < 40th percentile
- **Average**: 40-60th percentile
- **AboveAverage**: 60-80th percentile
- **Excellent**: > 80th percentile

**What it shows:**

- Relative performance
- Competitive standing
- Motivational insights
- Context for achievement

---

## Role-Based Access Control Demo

### Student Access (Emma Wilson)

**✅ Can Access Own Data**

```bash
# Emma can view her own performance
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/performance-summary"
# Response: 200 OK with data
```

**❌ Cannot Access Other Students**

```bash
# Emma cannot view Liam's performance
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000002/analytics/performance-summary"
# Response: 403 Forbidden (in production with auth)
```

---

### Teacher Access (Dr. Robert Williams)

**✅ Can Access Students in Same School**

```bash
# Teacher can view Emma's performance (same school)
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/performance-summary"
# Response: 200 OK with data
```

**✅ Can Access All Students in Classes They Teach**

```bash
# Teacher can view all 8 students in Grade 10 Math class
# Students: Emma, Liam, Olivia, Noah, Ava, Ethan, Sophia, Mason
for STUDENT_ID in 40000000-0000-0000-0000-00000000000{1..8}; do
  curl -X GET "http://localhost:5000/api/v1/students/$STUDENT_ID/analytics/performance-summary"
done
```

**❌ Cannot Access Students in Different Schools**

```bash
# Teacher cannot view Isabella's performance (Washington Academy)
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000009/analytics/performance-summary"
# Response: 403 Forbidden (in production with auth)
```

---

### School Admin Access (Sarah Johnson - Lincoln High)

**✅ Can Access All Students in Same School**

```bash
# School admin can view any Lincoln High student
for STUDENT_ID in 40000000-0000-0000-0000-00000000000{1..8}; do
  curl -X GET "http://localhost:5000/api/v1/students/$STUDENT_ID/analytics/performance-summary"
done
# Response: 200 OK for all
```

**❌ Cannot Access Students in Different Schools**

```bash
# School admin cannot view students from Washington or Roosevelt
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000009/analytics/performance-summary"
# Response: 403 Forbidden (in production with auth)
```

---

### System Admin Access

**✅ Can Access All Students in All Schools**

```bash
# System admin can view any student across all schools
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/performance-summary"  # Lincoln
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000009/analytics/performance-summary"  # Washington
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000017/analytics/performance-summary"  # Roosevelt
# Response: 200 OK for all
```

---

## Testing with JWT Authentication

Once Azure AD B2C is configured, use the test script:

### 1. Acquire Token

```bash
./scripts/test-auth.sh
```

Follow the prompts:

1. Opens browser for authentication
2. Login with Google account
3. Token is automatically captured
4. Token is saved to `.jwt_token` file

### 2. Test with Token

**Manual Testing:**

```bash
# Load token from file
TOKEN=$(cat .jwt_token)

# Test endpoint with authentication
curl -X GET "http://localhost:5000/api/v1/students/40000000-0000-0000-0000-000000000001/analytics/performance-summary" \
  -H "Authorization: Bearer $TOKEN" \
  -H "accept: application/json" | jq '.'
```

**Automated Testing:**

```bash
# test-auth.sh automatically tests 3 endpoints after acquiring token:
# 1. Performance Summary
# 2. Subject Performance (Mathematics)
# 3. Learning Objectives

# Run the script
./scripts/test-auth.sh
```

---

## Integration Test Demo

Run the comprehensive authentication test suite:

```bash
# Run all 45+ authentication/authorization tests
dotnet test --filter "FullyQualifiedName~StudentAnalyticsControllerAuthTests"

# Run specific test categories
dotnet test --filter "FullyQualifiedName~StudentAnalyticsControllerAuthTests.StudentCanAccessOwnData"
dotnet test --filter "FullyQualifiedName~StudentAnalyticsControllerAuthTests.TeacherCannotAccessStudentInDifferentSchool"
dotnet test --filter "FullyQualifiedName~StudentAnalyticsControllerAuthTests.SystemAdminCanAccessAnyStudent"
```

**Test Coverage:**

- ✅ Authentication (401 without token)
- ✅ Authorization (403 for unauthorized access)
- ✅ All 7 endpoints protected
- ✅ All 6 user roles tested
- ✅ Multi-tenant security validated

---

## Database Queries

Explore the data directly:

```bash
# Connect to database
docker exec -it edumind-postgres psql -U edumind_user -d edumind_dev
```

**Useful Queries:**

```sql
-- View all schools
SELECT "Id", "Name", "District" FROM academic."Schools";

-- View all students in Lincoln High
SELECT u."FullName", s."GradeLevel", u."Email" 
FROM academic."Students" s
JOIN academic."Users" u ON s."UserId" = u."Id"
WHERE s."SchoolId" = '10000000-0000-0000-0000-000000000001'
ORDER BY u."FullName";

-- View Emma Wilson's assessment scores
SELECT 
    a."Title",
    c."Name" as "Course",
    sa."Score",
    sa."TimeSpentSeconds" / 60 as "TimeMinutes",
    sa."CompletedAt"
FROM academic."StudentAssessments" sa
JOIN academic."Assessments" a ON sa."AssessmentId" = a."Id"
JOIN academic."Courses" c ON a."CourseId" = c."Id"
WHERE sa."StudentId" = '40000000-0000-0000-0000-000000000001'
ORDER BY sa."CompletedAt" DESC;

-- View question-level performance for an assessment
SELECT 
    q."QuestionText",
    q."DifficultyLevel",
    sr."IsCorrect",
    sr."TimeSpentSeconds"
FROM academic."StudentResponses" sr
JOIN academic."Questions" q ON sr."QuestionId" = q."Id"
WHERE sr."StudentAssessmentId" IN (
    SELECT "Id" 
    FROM academic."StudentAssessments" 
    WHERE "StudentId" = '40000000-0000-0000-0000-000000000001'
    LIMIT 1
)
ORDER BY sr."ResponseTime";

-- Subject performance across all students
SELECT 
    c."Subject",
    AVG(sa."Score") as "AverageScore",
    COUNT(sa."Id") as "AssessmentCount"
FROM academic."StudentAssessments" sa
JOIN academic."Assessments" a ON sa."AssessmentId" = a."Id"
JOIN academic."Courses" c ON a."CourseId" = c."Id"
GROUP BY c."Subject"
ORDER BY "AverageScore" DESC;
```

---

## Demo Scenarios

### Scenario 1: Teacher Dashboard

**Goal:** View class performance for Grade 10 Math

```bash
# Get all students in class
CLASS_ID="30000000-0000-0000-0000-000000000001"

# For each student, get performance summary
# (In production, this would be a batch endpoint)
for STUDENT_ID in 40000000-0000-0000-0000-00000000000{1..8}; do
  echo "Student: $STUDENT_ID"
  curl -s "http://localhost:5000/api/v1/students/$STUDENT_ID/analytics/performance-summary" | jq '.averageScore'
done
```

### Scenario 2: Student Progress Report

**Goal:** Generate comprehensive report for Emma Wilson

```bash
STUDENT_ID="40000000-0000-0000-0000-000000000001"

echo "=== Performance Summary ==="
curl -s "http://localhost:5000/api/v1/students/$STUDENT_ID/analytics/performance-summary" | jq '.'

echo ""
echo "=== Mathematics Performance ==="
curl -s "http://localhost:5000/api/v1/students/$STUDENT_ID/analytics/subject-performance?subject=0" | jq '.'

echo ""
echo "=== Learning Objectives ==="
curl -s "http://localhost:5000/api/v1/students/$STUDENT_ID/analytics/learning-objectives" | jq '.[] | {objective: .learningObjective, mastery: .masteryLevel}'

echo ""
echo "=== Improvement Areas ==="
curl -s "http://localhost:5000/api/v1/students/$STUDENT_ID/analytics/improvement-areas" | jq '.[] | {objective: .learningObjective, priority: .priority}'

echo ""
echo "=== Progress Timeline ==="
curl -s "http://localhost:5000/api/v1/students/$STUDENT_ID/analytics/progress-timeline" | jq '.overallTrend'
```

### Scenario 3: School Admin Overview

**Goal:** School-wide analytics for Lincoln High

```bash
# Get average scores for all Lincoln High students
echo "=== Lincoln High School Performance ==="

for STUDENT_ID in 40000000-0000-0000-0000-00000000000{1..8}; do
  SCORE=$(curl -s "http://localhost:5000/api/v1/students/$STUDENT_ID/analytics/performance-summary" | jq '.averageScore')
  echo "Student $STUDENT_ID: $SCORE"
done

# Calculate school average
# (In production, this would be a dedicated endpoint)
```

---

## Next Steps

### 1. Complete Azure AD B2C Setup

- Follow `docs/AZURE_AD_B2C_SETUP_GUIDE.md`
- Configure Google OAuth integration
- Test with real authentication tokens

### 2. Integrate AI Agents

- Connect assessment generation agents
- Enable adaptive questioning
- Implement real-time feedback

### 3. Build Frontend

- Student dashboard
- Teacher analytics portal
- Admin management console

### 4. Add More Features

- Real-time progress tracking
- Parent/guardian access
- Collaborative learning tools
- Gamification elements

---

## Troubleshooting

### Database Connection Issues

```bash
# Check if PostgreSQL is running
docker ps | grep postgres

# Restart PostgreSQL
docker-compose restart postgres

# Check connection
psql -h localhost -p 5432 -U edumind_user -d edumind_dev
```

### API Not Responding

```bash
# Check if API is running
curl http://localhost:5000/health

# Check API logs
dotnet run --project src/AcademicAssessment.Web

# Restart API
# Ctrl+C to stop, then restart
```

### No Data Returned

```bash
# Verify demo data was seeded
psql -h localhost -p 5432 -U edumind_user -d edumind_dev -c "SELECT COUNT(*) FROM academic.\"Students\";"

# Re-seed if needed
./scripts/seed-demo-data.sh
```

### Authentication Errors

```bash
# In development mode, authentication is disabled
# Check appsettings.Development.json:
# "Authentication": { "Enabled": false }

# For production testing, acquire fresh token:
./scripts/test-auth.sh
```

---

## Resources

- **API Documentation**: <http://localhost:5000/swagger>
- **Azure AD B2C Setup**: `docs/AZURE_AD_B2C_SETUP_GUIDE.md`
- **JWT Testing Guide**: `docs/JWT_AUTHENTICATION_TESTING.md`
- **Architecture Overview**: `docs/SOLUTION_STRUCTURE.md`
- **Task Journal**: `docs/TASK_JOURNAL.md`

---

## Support

For issues or questions:

1. Check troubleshooting section above
2. Review `docs/` directory for detailed documentation
3. Check `TASK_JOURNAL.md` for implementation context
4. Run integration tests to verify system state

---

**Demo Date**: October 15, 2024  
**Version**: 1.0.0  
**Status**: ✅ Ready for Testing
