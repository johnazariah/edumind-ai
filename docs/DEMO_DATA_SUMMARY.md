# Demo Data Setup - Complete Summary

## Overview
Successfully created comprehensive demo data for EduMind.AI testing and demonstration purposes.

## Database Schema
All tables created via EF Core migrations in the `public` schema:
- ✅ schools
- ✅ users  
- ✅ students
- ✅ classes
- ✅ courses
- ✅ assessments
- ✅ questions
- ✅ student_assessments
- ✅ student_responses

## Demo Data Statistics

### Base Entities
- **3 Schools**
  - Lincoln High School (LHS) - Central District
  - Washington Academy (WA) - North District  
  - Roosevelt Institute (RI) - South District

- **34 Users**
  - 1 System Administrator
  - 3 School Administrators (1 per school)
  - 6 Teachers (2 per school - Math & Science)
  - 24 Students (8 per school)

- **24 Students**
  - Lincoln High: 8 students (Grade 9)
  - Washington Academy: 8 students (Grade 10)
  - Roosevelt Institute: 8 students (Grade 11)

- **6 Classes**
  - 2 per school (Math and Science)
  - Each class has 8 students enrolled

- **6 Courses**
  - Algebra I (Grade 9, Mathematics)
  - Algebra II (Grade 10, Mathematics)
  - Pre-Calculus (Grade 11, Mathematics)
  - Physics I (Grade 10)
  - Chemistry I (Grade 10)
  - Biology I (Grade 9)

- **13 Assessments**
  - Distributed across all 6 courses
  - Types: Practice (0), Quiz (1), Test (2), Exam (3)
  - Time limits: 25-60 minutes

### Generated Content
- **171 Questions**
  - 10-20 questions per assessment
  - Multiple choice format
  - Subject-specific content (Math, Physics, Chemistry, Biology)
  - Difficulty levels: 0-4 (VeryEasy to VeryHard)
  - IRT parameters configured:
    - Discrimination: 0.5-2.0
    - Difficulty: -2.0 to 2.0
    - Guessing: 0.2-0.3
  - Each question includes:
    - Question text
    - Answer options (A, B, C, D)
    - Correct answer
    - Explanation
    - Points (5, 10, or 15)
    - Topics and learning objectives

- **89 Student Assessments**
  - 3-5 attempts per student
  - Distributed over last 30 days
  - Status: All completed
  - Realistic scores: 60-95%
  - Time tracking: 60-100% of time limit used
  - XP earned: 60-950 per assessment

- **1,179 Student Responses**
  - ~13 responses per student assessment (varies by assessment length)
  - Realistic answer patterns based on target score
  - Time spent: 30-180 seconds per question
  - Feedback and AI explanations included
  - Question statistics automatically updated

## Test Credentials

### Students
- **Emma Wilson** (Lincoln HS, Grade 9)
  - Email: emma.wilson@lincoln.edu
  - Student ID: 40000000-0000-0000-0000-000000000001
  - User ID: 20000000-0000-0000-0000-000000000101
  - Assessments: 4 completed

### Teachers
- **Dr. Robert Williams** (Lincoln HS, Math)
  - Email: math.teacher@lincoln.edu
  - User ID: 20000000-0000-0000-0000-000000000021
  - School ID: 10000000-0000-0000-0000-000000000001

- **Dr. Lisa Anderson** (Lincoln HS, Science)
  - Email: science.teacher@lincoln.edu
  - User ID: 20000000-0000-0000-0000-000000000022

### Administrators
- **Sarah Johnson** (Lincoln HS School Admin)
  - Email: admin@lincoln.edu
  - User ID: 20000000-0000-0000-0000-000000000011
  - School ID: 10000000-0000-0000-0000-000000000001

- **System Administrator**
  - Email: admin@edumind.ai
  - User ID: 20000000-0000-0000-0000-000000000001

## Data Loading Scripts

### 1. seed-demo-data-v2.sql
**Purpose**: Load base entity data
**Content**:
- Schools with contact information
- All 34 users (system admin, school admins, teachers, students)
- Student records with gamification data (XP, streaks, subscription tiers)
- Classes with teacher and student assignments
- Courses with learning objectives and topics
- Assessment templates

**Usage**:
```bash
PGPASSWORD=edumind_dev_password psql -h localhost -U edumind_user -d edumind_dev -f scripts/seed-demo-data-v2.sql
```

### 2. add-questions-and-responses.sql
**Purpose**: Generate questions and student activity data
**Content**:
- Questions for all assessments with realistic content
- Student assessment attempts with varied scores
- Student responses with timing and feedback
- Automatic question statistics updates

**Features**:
- Uses PostgreSQL procedural code (DO blocks)
- Generates UUIDs with `gen_random_uuid()`
- Creates realistic score distributions
- Timestamps spread over 30 days
- Subject-specific question content

**Usage**:
```bash
PGPASSWORD=edumind_dev_password psql -h localhost -U edumind_user -d edumind_dev -f scripts/add-questions-and-responses.sql
```

## Setup Instructions

### Prerequisites
1. PostgreSQL running (localhost:5432)
2. Database `edumind_dev` created
3. User `edumind_user` with password `edumind_dev_password`
4. EF Core migrations applied

### Quick Setup
```bash
# 1. Apply EF Core migrations (creates tables)
cd /workspaces/edumind-ai
dotnet ef database update --project src/AcademicAssessment.Infrastructure

# 2. Load base data
PGPASSWORD=edumind_dev_password psql -h localhost -U edumind_user -d edumind_dev \
  -f scripts/seed-demo-data-v2.sql

# 3. Generate questions and responses
PGPASSWORD=edumind_dev_password psql -h localhost -U edumind_user -d edumind_dev \
  -f scripts/add-questions-and-responses.sql

# 4. Verify data
PGPASSWORD=edumind_dev_password psql -h localhost -U edumind_user -d edumind_dev \
  -c "SELECT 'Schools: ' || COUNT(*) FROM schools
      UNION ALL SELECT 'Users: ' || COUNT(*) FROM users
      UNION ALL SELECT 'Questions: ' || COUNT(*) FROM questions
      UNION ALL SELECT 'Assessments: ' || COUNT(*) FROM student_assessments
      UNION ALL SELECT 'Responses: ' || COUNT(*) FROM student_responses;"
```

### Expected Output
```
         ?column?         
-------------------------
 Schools: 3
 Users: 34
 Questions: 171
 Assessments: 89
 Responses: 1179
```

## API Testing

### Start the API
```bash
dotnet run --project src/AcademicAssessment.Web/AcademicAssessment.Web.csproj
```

The API will start on port 5103 (or check with `netstat -tlnp | grep LISTEN`).

### Test Endpoints

#### Performance Summary
```bash
curl "http://127.0.0.1:5103/api/analytics/students/40000000-0000-0000-0000-000000000001/performance-summary"
```

#### Subject Performance
```bash
# Mathematics (Subject = 0)
curl "http://127.0.0.1:5103/api/analytics/students/40000000-0000-0000-0000-000000000001/subject-performance?subject=0"
```

#### Learning Objectives
```bash
curl "http://127.0.0.1:5103/api/analytics/students/40000000-0000-0000-0000-000000000001/learning-objectives"
```

#### Ability Estimates (IRT)
```bash
curl "http://127.0.0.1:5103/api/analytics/students/40000000-0000-0000-0000-000000000001/ability-estimates"
```

#### Improvement Areas
```bash
curl "http://127.0.0.1:5103/api/analytics/students/40000000-0000-0000-0000-000000000001/improvement-areas"
```

#### Progress Timeline
```bash
curl "http://127.0.0.1:5103/api/analytics/students/40000000-0000-0000-0000-000000000001/progress-timeline"
```

#### Peer Comparison
```bash
curl "http://127.0.0.1:5103/api/analytics/students/40000000-0000-0000-0000-000000000001/peer-comparison"
```

## Data Characteristics

### Realistic Patterns
- **Score Distribution**: 60-95% (simulates real student performance)
- **Time Management**: Students use 60-100% of allocated time
- **Attempt Frequency**: 3-5 assessments per student over 30 days
- **Question Difficulty**: Mix of 5 difficulty levels (0-4)
- **Answer Patterns**: Correct answers match target scores

### IRT (Item Response Theory) Support
All questions include:
- **Discrimination** (a): How well the question differentiates ability levels (0.5-2.0)
- **Difficulty** (b): Question difficulty on ability scale (-2.0 to 2.0)
- **Guessing** (c): Probability of guessing correctly (0.2-0.3)

### Gamification Data
Students include:
- XP points (90-400 based on grade/performance)
- Daily streaks (1-20 days)
- Subscription tiers (Basic=1, Premium=2)
- Activity levels tracked

## Troubleshooting

### No Tables Found
**Error**: `relation "schools" does not exist`
**Solution**: Apply EF Core migrations first
```bash
dotnet ef database update --project src/AcademicAssessment.Infrastructure
```

### No Data Returned
**Check 1**: Verify data exists
```bash
PGPASSWORD=edumind_dev_password psql -h localhost -U edumind_user -d edumind_dev \
  -c "SELECT COUNT(*) FROM students;"
```

**Check 2**: Verify foreign key relationships
```bash
PGPASSWORD=edumind_dev_password psql -h localhost -U edumind_user -d edumind_dev \
  -c "SELECT s.\"Id\", u.\"Email\", COUNT(sa.\"Id\") as assessments
      FROM students s 
      JOIN users u ON s.\"UserId\" = u.\"Id\"
      LEFT JOIN student_assessments sa ON s.\"Id\" = sa.\"StudentId\"
      GROUP BY s.\"Id\", u.\"Email\"
      LIMIT 5;"
```

### API Not Responding
**Check 1**: Verify API is running
```bash
ps aux | grep "AcademicAssess"
netstat -tlnp | grep LISTEN
```

**Check 2**: Check API logs in the run-web-api terminal

**Check 3**: Test basic connectivity
```bash
curl -v "http://127.0.0.1:5103/" 2>&1 | grep HTTP
```

## Next Steps

### Recommended Testing Order
1. ✅ Verify database tables exist
2. ✅ Confirm data loaded correctly (all 9 tables populated)
3. ⏳ Start Web API
4. ⏳ Test analytics endpoints with real data
5. ⏳ Verify role-based access control
6. ⏳ Test with real Azure AD B2C tokens (Task 4)

### Integration with AI Agents
Once demo data is verified working:
1. Test AI agents with real assessment data
2. Generate adaptive questions based on student performance
3. Test recommendation engine with actual student histories
4. Validate IRT calculations with response data

## Files Created

### Scripts
- `scripts/seed-demo-data-v2.sql` - Base entity data
- `scripts/add-questions-and-responses.sql` - Generated content

### Documentation
- `docs/DEMO.md` - Comprehensive demo guide (800+ lines)
- `docs/JWT_AUTHENTICATION_TESTING.md` - Auth testing guide
- `docs/DEMO_DATA_SUMMARY.md` - This file

### Infrastructure
- `src/AcademicAssessment.Infrastructure/Data/AcademicContextFactory.cs` - EF Core design-time factory

## Git Commits
- `cd5e46c` - Add EF Core design-time factory and load demo base data
- `69f5ee1` - Complete demo data: add questions, assessments, and responses

## Status
✅ Task 3 (Create end-to-end demo with sample data) - **COMPLETE**

All demo data successfully loaded and ready for testing!
