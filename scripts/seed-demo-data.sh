#!/bin/bash

# ============================================================================
# EduMind.AI Demo Data Seed Script
# ============================================================================
# This script populates the PostgreSQL database with realistic demo data
# Usage: ./seed-demo-data.sh
# ============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Database connection details (from docker-compose.yml)
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-edumind_dev}"
DB_USER="${DB_USER:-edumind_user}"
DB_PASSWORD="${DB_PASSWORD:-edumind_dev_password}"

echo -e "${BLUE}============================================================================${NC}"
echo -e "${BLUE}EduMind.AI Demo Data Seeding${NC}"
echo -e "${BLUE}============================================================================${NC}"
echo ""

# Check if PostgreSQL is running
echo -e "${YELLOW}[1/6] Checking database connection...${NC}"
if ! PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c '\q' 2>/dev/null; then
    echo -e "${RED}✗ Cannot connect to PostgreSQL database${NC}"
    echo -e "${YELLOW}  Make sure PostgreSQL is running: docker-compose up -d${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Database connection successful${NC}"
echo ""

# Run the SQL seed script
echo -e "${YELLOW}[2/6] Seeding base data (schools, users, classes, courses, assessments)...${NC}"
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f "$(dirname "$0")/seed-demo-data.sql" > /dev/null
echo -e "${GREEN}✓ Base data seeded successfully${NC}"
echo -e "  - 3 schools created${NC}"
echo -e "  - 31 users created (1 admin, 3 school admins, 6 teachers, 24 students)${NC}"
echo -e "  - 6 classes created${NC}"
echo -e "  - 6 courses created${NC}"
echo -e "  - 13 assessments created${NC}"
echo ""

# Add questions to assessments
echo -e "${YELLOW}[3/6] Adding questions to assessments...${NC}"

# Function to add questions
add_questions() {
    local ASSESSMENT_ID=$1
    local NUM_QUESTIONS=$2
    local SUBJECT=$3
    local DIFFICULTY=$4
    
    for i in $(seq 1 $NUM_QUESTIONS); do
        QUESTION_ID=$(uuidgen | tr '[:upper:]' '[:lower:]')
        
        # Create question based on subject
        case $SUBJECT in
            "Mathematics")
                TEXT="Solve the equation: 2x + 5 = ${i}x - 3"
                CORRECT_ANSWER="Option A"
                OPTIONS='["Option A", "Option B", "Option C", "Option D"]'
                ;;
            "Physics")
                TEXT="A ball is thrown upward with velocity ${i}0 m/s. What is its maximum height?"
                CORRECT_ANSWER="Option B"
                OPTIONS='["50m", "125m", "200m", "300m"]'
                ;;
            "Chemistry")
                TEXT="Balance the equation: C${i}H8 + O2 → CO2 + H2O"
                CORRECT_ANSWER="Option C"
                OPTIONS='["1,2,3,4", "2,4,6,8", "2,5,4,4", "1,1,1,1"]'
                ;;
            "Biology")
                TEXT="Which organelle is responsible for energy production in cell type ${i}?"
                CORRECT_ANSWER="Mitochondria"
                OPTIONS='["Nucleus", "Mitochondria", "Ribosome", "Golgi Apparatus"]'
                ;;
            *)
                TEXT="Question ${i} for assessment"
                CORRECT_ANSWER="Option A"
                OPTIONS='["Option A", "Option B", "Option C", "Option D"]'
                ;;
        esac
        
        PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME <<EOF > /dev/null
INSERT INTO academic."Questions" ("Id", "AssessmentId", "QuestionText", "QuestionType", "DifficultyLevel", "Points", "CorrectAnswer", "Options", "LearningObjective", "Subject", "CreatedAt", "UpdatedAt")
VALUES (
    '$QUESTION_ID',
    '$ASSESSMENT_ID',
    '$TEXT',
    0,
    $DIFFICULTY,
    $(($DIFFICULTY + 1)),
    '$CORRECT_ANSWER',
    '$OPTIONS'::jsonb,
    'Learning Objective $i',
    $(case $SUBJECT in "Mathematics") echo 0;; "Physics") echo 1;; "Chemistry") echo 2;; "Biology") echo 3;; *) echo 0;; esac),
    NOW(),
    NOW()
);
EOF
    done
}

# Add questions to each assessment
add_questions '60000000-0000-0000-0000-000000000001' 15 'Mathematics' 2  # Quadratic Equations
add_questions '60000000-0000-0000-0000-000000000002' 10 'Mathematics' 1  # Polynomial Functions
add_questions '60000000-0000-0000-0000-000000000003' 20 'Mathematics' 3  # Systems of Equations
add_questions '60000000-0000-0000-0000-000000000004' 18 'Mathematics' 3  # Trigonometry
add_questions '60000000-0000-0000-0000-000000000005' 12 'Mathematics' 4  # Limits and Continuity
add_questions '60000000-0000-0000-0000-000000000006' 15 'Mathematics' 1  # Linear Equations
add_questions '60000000-0000-0000-0000-000000000007' 10 'Mathematics' 0  # Variables and Expressions
add_questions '60000000-0000-0000-0000-000000000008' 16 'Physics' 2      # Motion and Forces
add_questions '60000000-0000-0000-0000-000000000009' 12 'Physics' 2      # Energy and Work
add_questions '60000000-0000-0000-0000-000000000010' 15 'Chemistry' 2    # Chemical Reactions
add_questions '60000000-0000-0000-0000-000000000011' 14 'Chemistry' 3    # Stoichiometry
add_questions '60000000-0000-0000-0000-000000000012' 15 'Biology' 1      # Cell Structure
add_questions '60000000-0000-0000-0000-000000000013' 12 'Biology' 2      # DNA and Genetics

echo -e "${GREEN}✓ Questions added successfully (183 total questions)${NC}"
echo ""

# Create student assessments and responses
echo -e "${YELLOW}[4/6] Creating student assessment attempts...${NC}"

# Get all student IDs
STUDENT_IDS=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT \"Id\" FROM academic.\"Students\";")

TOTAL_ASSESSMENTS=0
TOTAL_RESPONSES=0

for STUDENT_ID in $STUDENT_IDS; do
    # Each student takes 3-5 random assessments
    NUM_ASSESSMENTS=$(shuf -i 3-5 -n 1)
    
    # Get random assessment IDs
    ASSESSMENT_IDS=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT \"Id\" FROM academic.\"Assessments\" ORDER BY RANDOM() LIMIT $NUM_ASSESSMENTS;")
    
    for ASSESSMENT_ID in $ASSESSMENT_IDS; do
        STUDENT_ASSESSMENT_ID=$(uuidgen | tr '[:upper:]' '[:lower:]')
        
        # Random start time in the last 30 days
        DAYS_AGO=$(shuf -i 1-30 -n 1)
        START_TIME="NOW() - INTERVAL '$DAYS_AGO days' - INTERVAL '2 hours'"
        
        # Random completion time (30-60 minutes after start)
        DURATION_MINUTES=$(shuf -i 30-60 -n 1)
        COMPLETED_TIME="NOW() - INTERVAL '$DAYS_AGO days' - INTERVAL '2 hours' + INTERVAL '$DURATION_MINUTES minutes'"
        
        # Random score between 60-95%
        SCORE=$(shuf -i 60-95 -n 1)
        
        # Insert student assessment
        PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME <<EOF > /dev/null
INSERT INTO academic."StudentAssessments" ("Id", "StudentId", "AssessmentId", "StartedAt", "CompletedAt", "Score", "Status", "TimeSpentSeconds", "CreatedAt", "UpdatedAt")
VALUES (
    '$STUDENT_ASSESSMENT_ID',
    '$STUDENT_ID',
    '$ASSESSMENT_ID',
    $START_TIME,
    $COMPLETED_TIME,
    $SCORE,
    2,
    $(($DURATION_MINUTES * 60)),
    NOW(),
    NOW()
);
EOF
        
        TOTAL_ASSESSMENTS=$((TOTAL_ASSESSMENTS + 1))
        
        # Get questions for this assessment
        QUESTION_IDS=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT \"Id\" FROM academic.\"Questions\" WHERE \"AssessmentId\" = '$ASSESSMENT_ID';")
        
        # Create responses for each question
        QUESTION_NUM=0
        for QUESTION_ID in $QUESTION_IDS; do
            RESPONSE_ID=$(uuidgen | tr '[:upper:]' '[:lower:]')
            QUESTION_NUM=$((QUESTION_NUM + 1))
            
            # Get correct answer
            CORRECT_ANSWER=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT \"CorrectAnswer\" FROM academic.\"Questions\" WHERE \"Id\" = '$QUESTION_ID';" | xargs)
            
            # Determine if answer is correct based on score (probabilistic)
            if [ $((RANDOM % 100)) -lt $SCORE ]; then
                STUDENT_ANSWER="$CORRECT_ANSWER"
                IS_CORRECT="true"
            else
                STUDENT_ANSWER="Wrong Answer"
                IS_CORRECT="false"
            fi
            
            # Random time spent on question (30-180 seconds)
            TIME_SPENT=$(shuf -i 30-180 -n 1)
            
            # Calculate response time
            RESPONSE_TIME="$START_TIME + INTERVAL '$((QUESTION_NUM * TIME_SPENT)) seconds'"
            
            # Insert student response
            PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME <<EOF > /dev/null
INSERT INTO academic."StudentResponses" ("Id", "StudentAssessmentId", "QuestionId", "StudentAnswer", "IsCorrect", "TimeSpentSeconds", "ResponseTime", "CreatedAt", "UpdatedAt")
VALUES (
    '$RESPONSE_ID',
    '$STUDENT_ASSESSMENT_ID',
    '$QUESTION_ID',
    '$STUDENT_ANSWER',
    $IS_CORRECT,
    $TIME_SPENT,
    $RESPONSE_TIME,
    NOW(),
    NOW()
);
EOF
            
            TOTAL_RESPONSES=$((TOTAL_RESPONSES + 1))
        done
    done
done

echo -e "${GREEN}✓ Student assessment attempts created${NC}"
echo -e "  - $TOTAL_ASSESSMENTS student assessments${NC}"
echo -e "  - $TOTAL_RESPONSES student responses${NC}"
echo ""

# Display summary statistics
echo -e "${YELLOW}[5/6] Generating summary statistics...${NC}"

TOTAL_SCHOOLS=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM academic.\"Schools\";" | xargs)
TOTAL_USERS=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM academic.\"Users\";" | xargs)
TOTAL_STUDENTS=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM academic.\"Students\";" | xargs)
TOTAL_CLASSES=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM academic.\"Classes\";" | xargs)
TOTAL_COURSES=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM academic.\"Courses\";" | xargs)
TOTAL_ASSESSMENT_TEMPLATES=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM academic.\"Assessments\";" | xargs)
TOTAL_QUESTIONS=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM academic.\"Questions\";" | xargs)

echo -e "${GREEN}✓ Summary statistics generated${NC}"
echo ""

# Final summary
echo -e "${YELLOW}[6/6] Seeding complete!${NC}"
echo ""
echo -e "${BLUE}============================================================================${NC}"
echo -e "${BLUE}Demo Data Summary${NC}"
echo -e "${BLUE}============================================================================${NC}"
echo -e "${GREEN}Schools:              ${TOTAL_SCHOOLS}${NC}"
echo -e "${GREEN}Total Users:          ${TOTAL_USERS}${NC}"
echo -e "${GREEN}  - Students:         ${TOTAL_STUDENTS}${NC}"
echo -e "${GREEN}  - Teachers:         6${NC}"
echo -e "${GREEN}  - School Admins:    3${NC}"
echo -e "${GREEN}  - System Admin:     1${NC}"
echo -e "${GREEN}Classes:              ${TOTAL_CLASSES}${NC}"
echo -e "${GREEN}Courses:              ${TOTAL_COURSES}${NC}"
echo -e "${GREEN}Assessment Templates: ${TOTAL_ASSESSMENT_TEMPLATES}${NC}"
echo -e "${GREEN}Questions:            ${TOTAL_QUESTIONS}${NC}"
echo -e "${GREEN}Student Assessments:  ${TOTAL_ASSESSMENTS}${NC}"
echo -e "${GREEN}Student Responses:    ${TOTAL_RESPONSES}${NC}"
echo -e "${BLUE}============================================================================${NC}"
echo ""
echo -e "${GREEN}✓ Demo data seeded successfully!${NC}"
echo ""
echo -e "${YELLOW}Test User Credentials:${NC}"
echo -e "  Student:      emma.wilson@lincoln.edu (ID: 20000000-0000-0000-0000-000000000101)"
echo -e "  Teacher:      math.teacher@lincoln.edu (ID: 20000000-0000-0000-0000-000000000021)"
echo -e "  School Admin: admin@lincoln.edu (ID: 20000000-0000-0000-0000-000000000011)"
echo -e "  System Admin: admin@edumind.ai (ID: 20000000-0000-0000-0000-000000000001)"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo -e "  1. Start the API: ${BLUE}dotnet run --project src/AcademicAssessment.Web${NC}"
echo -e "  2. Test endpoints: ${BLUE}./scripts/test-auth.sh${NC}"
echo -e "  3. View demo: ${BLUE}See docs/DEMO.md${NC}"
echo ""
