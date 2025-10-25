-- ============================================================================
-- Add Questions, Student Assessments, and Responses
-- ============================================================================
-- This script adds the remaining demo data:
-- - Questions for each assessment (10-20 per assessment)
-- - Student assessment attempts (3-5 per student)
-- - Student responses with realistic scoring
-- ============================================================================

\set QUIET on
\set ON_ERROR_STOP on

-- Track progress
\echo 'Starting to add questions, student assessments, and responses...'
\echo ''

-- ============================================================================
-- QUESTIONS
-- ============================================================================
\echo '[1/3] Adding questions to assessments...'

-- Helper function to generate questions
DO $$
DECLARE
    assessment_rec RECORD;
    question_count INT;
    i INT;
    new_question_id UUID;
    question_ids TEXT[];
BEGIN
    -- Loop through all assessments
    FOR assessment_rec IN 
        SELECT "Id", "Title", "Subject", "GradeLevel", "CourseId"
        FROM assessments 
        ORDER BY "Id"
    LOOP
        -- Determine how many questions based on assessment type
        question_count := 10 + (RANDOM() * 10)::INT; -- 10-20 questions
        question_ids := ARRAY[]::TEXT[];
        
        -- Generate questions for this assessment
        FOR i IN 1..question_count LOOP
            new_question_id := gen_random_uuid();
            question_ids := array_append(question_ids, new_question_id::TEXT);
            
            -- Insert question with subject-specific content
            INSERT INTO questions (
                "Id", "CourseId", "QuestionText", "QuestionType", "Subject", 
                "GradeLevel", "DifficultyLevel", "Topics", "LearningObjectives",
                "AnswerOptions", "CorrectAnswer", "Explanation", "Points",
                "IrtDiscrimination", "IrtDifficulty", "IrtGuessing",
                "IsActive", "TimesAnswered", "TimesCorrect",
                "CreatedAt", "UpdatedAt", "IsAiGenerated", "ContentHash"
            ) VALUES (
                new_question_id,
                assessment_rec."CourseId",
                CASE assessment_rec."Subject"
                    WHEN 0 THEN 'Solve the equation: 2x + ' || i || ' = ' || (i * 3) || 'x - ' || (i * 2)
                    WHEN 1 THEN 'A ball is thrown upward with velocity ' || (i * 10) || ' m/s. What is the maximum height?'
                    WHEN 2 THEN 'Balance the equation: C' || i || 'H8 + O2 → CO2 + H2O. What is the coefficient of O2?'
                    WHEN 3 THEN 'Which organelle is responsible for energy production in question ' || i || '?'
                    ELSE 'Question ' || i || ' about the topic'
                END,
                0, -- MultipleChoice
                assessment_rec."Subject",
                assessment_rec."GradeLevel",
                (i % 5), -- DifficultyLevel 0-4
                '["Topic ' || i || '", "Concept ' || ((i % 3) + 1) || '"]',
                '["Learning Objective ' || i || '"]',
                CASE assessment_rec."Subject"
                    WHEN 0 THEN '{"A": "' || (i * 2) || '", "B": "' || (i * 3) || '", "C": "' || (i * 4) || '", "D": "' || (i * 5) || '"}'
                    WHEN 1 THEN '{"A": "' || (i * 10) || ' m", "B": "' || (i * 15) || ' m", "C": "' || (i * 20) || ' m", "D": "' || (i * 25) || ' m"}'
                    WHEN 2 THEN '{"A": "' || i || '", "B": "' || (i + 1) || '", "C": "' || (i + 2) || '", "D": "' || (i + 3) || '"}'
                    WHEN 3 THEN '{"A": "Nucleus", "B": "Mitochondria", "C": "Ribosome", "D": "Golgi"}'
                    ELSE '{"A": "Option A", "B": "Option B", "C": "Option C", "D": "Option D"}'
                END,
                CASE (i % 4) WHEN 0 THEN 'A' WHEN 1 THEN 'B' WHEN 2 THEN 'C' ELSE 'D' END,
                'This is the explanation for question ' || i,
                CASE WHEN (i % 5) < 2 THEN 5 WHEN (i % 5) < 4 THEN 10 ELSE 15 END,
                0.5 + (RANDOM() * 1.5), -- IRT Discrimination 0.5-2.0
                -2.0 + (RANDOM() * 4.0), -- IRT Difficulty -2.0 to 2.0
                0.2 + (RANDOM() * 0.1), -- IRT Guessing 0.2-0.3
                true,
                0, -- TimesAnswered (will be updated by responses)
                0, -- TimesCorrect (will be updated by responses)
                NOW(),
                NOW(),
                true,
                md5(random()::text)
            );
        END LOOP;
        
        -- Update assessment with question IDs
        UPDATE assessments 
        SET "QuestionIds" = array_to_json(question_ids)::TEXT,
            "UpdatedAt" = NOW()
        WHERE "Id" = assessment_rec."Id";
        
    END LOOP;
    
    RAISE NOTICE 'Added % questions across % assessments', 
        (SELECT COUNT(*) FROM questions),
        (SELECT COUNT(*) FROM assessments);
END $$;

\echo '  ✓ Questions added'
\echo ''

-- ============================================================================
-- STUDENT ASSESSMENTS
-- ============================================================================
\echo '[2/3] Creating student assessment attempts...'

DO $$
DECLARE
    student_rec RECORD;
    assessment_rec RECORD;
    attempt_count INT;
    i INT;
    new_assessment_id UUID;
    days_ago INT;
    target_score INT;
    question_ids UUID[];
    total_questions INT;
    correct_count INT;
    incorrect_count INT;
    time_spent INT;
    xp_earned INT;
BEGIN
    -- Loop through all students
    FOR student_rec IN 
        SELECT s."Id" as student_id, s."SchoolId", s."GradeLevel", 
               u."Email"
        FROM students s
        JOIN users u ON s."UserId" = u."Id"
        ORDER BY s."Id"
    LOOP
        -- Each student attempts 3-5 assessments
        attempt_count := 3 + (RANDOM() * 2)::INT;
        
        FOR i IN 1..attempt_count LOOP
            -- Pick a random assessment appropriate for this student's grade level
            SELECT "Id", "TimeLimitMinutes", "QuestionIds"
            INTO assessment_rec
            FROM assessments
            WHERE "GradeLevel" <= student_rec."GradeLevel" + 1
              AND "GradeLevel" >= student_rec."GradeLevel" - 1
            ORDER BY RANDOM()
            LIMIT 1;
            
            IF assessment_rec."Id" IS NULL THEN
                CONTINUE;
            END IF;
            
            -- Parse question IDs from JSON array
            SELECT array_agg((value::TEXT)::UUID)
            INTO question_ids
            FROM json_array_elements_text(assessment_rec."QuestionIds"::JSON);
            
            total_questions := array_length(question_ids, 1);
            IF total_questions IS NULL OR total_questions = 0 THEN
                CONTINUE;
            END IF;
            
            new_assessment_id := gen_random_uuid();
            days_ago := (RANDOM() * 30)::INT; -- Within last 30 days
            
            -- Generate realistic score based on student's XP level
            -- Higher XP students tend to score better (60-95%)
            target_score := 60 + (RANDOM() * 35)::INT;
            correct_count := (total_questions * target_score / 100)::INT;
            incorrect_count := total_questions - correct_count;
            time_spent := assessment_rec."TimeLimitMinutes" * 60 * (0.6 + RANDOM() * 0.4)::INT; -- 60-100% of time limit
            xp_earned := (target_score / 10)::INT * 10; -- 10 XP per 10% score
            
            -- Insert student assessment
            INSERT INTO student_assessments (
                "Id", "StudentId", "AssessmentId", "SchoolId", "ClassId",
                "Status", "StartedAt", "CompletedAt", 
                "Score", "MaxScore", "Passed",
                "CurrentQuestionIndex", "EstimatedAbility",
                "TimeSpentSeconds", "CorrectAnswers", "IncorrectAnswers", "SkippedQuestions",
                "Feedback", "Recommendations", "CreatedAt", "UpdatedAt", "XpEarned"
            ) VALUES (
                new_assessment_id,
                student_rec.student_id,
                assessment_rec."Id",
                student_rec."SchoolId",
                NULL, -- ClassId
                2, -- Completed
                NOW() - (days_ago || ' days')::INTERVAL - (time_spent || ' seconds')::INTERVAL,
                NOW() - (days_ago || ' days')::INTERVAL,
                correct_count * 10, -- Assuming 10 points per question
                total_questions * 10,
                CASE WHEN target_score >= 70 THEN true ELSE false END,
                total_questions,
                -1.0 + (RANDOM() * 2.0), -- Ability estimate -1 to 1
                time_spent,
                correct_count,
                incorrect_count,
                0,
                CASE 
                    WHEN target_score >= 90 THEN 'Excellent work! You demonstrated strong mastery of the material.'
                    WHEN target_score >= 80 THEN 'Good job! You have a solid understanding with room for improvement.'
                    WHEN target_score >= 70 THEN 'Satisfactory performance. Review the topics where you had difficulty.'
                    ELSE 'You may need additional practice with this material. Consider reviewing the concepts.'
                END,
                '["Review incorrect questions", "Practice similar problems"]',
                NOW() - (days_ago || ' days')::INTERVAL,
                NOW() - (days_ago || ' days')::INTERVAL,
                xp_earned
            );
            
            -- Now create responses for each question
            FOR j IN 1..total_questions LOOP
                DECLARE
                    is_correct BOOLEAN;
                    question_rec RECORD;
                BEGIN
                    -- Determine if this answer is correct based on target score
                    is_correct := (j <= correct_count);
                    
                    -- Get question details
                    SELECT "CorrectAnswer", "Points"
                    INTO question_rec
                    FROM questions
                    WHERE "Id" = question_ids[j];
                    
                    -- Insert student response
                    INSERT INTO student_responses (
                        "Id", "StudentAssessmentId", "StudentId", "QuestionId", "SchoolId",
                        "StudentAnswer", "IsCorrect", "PointsEarned", "MaxPoints",
                        "TimeSpentSeconds", "QuestionOrder", "AbilityAtTime",
                        "Feedback", "AiExplanation", "SubmittedAt", "CreatedAt"
                    ) VALUES (
                        gen_random_uuid(),
                        new_assessment_id,
                        student_rec.student_id,
                        question_ids[j],
                        student_rec."SchoolId",
                        CASE 
                            WHEN is_correct THEN question_rec."CorrectAnswer"
                            ELSE CASE (j % 3) WHEN 0 THEN 'A' WHEN 1 THEN 'B' ELSE 'C' END
                        END,
                        is_correct,
                        CASE WHEN is_correct THEN question_rec."Points" ELSE 0 END,
                        question_rec."Points",
                        30 + (RANDOM() * 150)::INT, -- 30-180 seconds per question
                        j,
                        -1.0 + (RANDOM() * 2.0),
                        CASE WHEN is_correct THEN 'Correct!' ELSE 'Incorrect. Review this concept.' END,
                        CASE WHEN NOT is_correct THEN 'The correct answer is ' || question_rec."CorrectAnswer" ELSE NULL END,
                        NOW() - (days_ago || ' days')::INTERVAL - ((total_questions - j) * 60 || ' seconds')::INTERVAL,
                        NOW() - (days_ago || ' days')::INTERVAL - ((total_questions - j) * 60 || ' seconds')::INTERVAL
                    );
                    
                    -- Update question statistics
                    UPDATE questions
                    SET "TimesAnswered" = "TimesAnswered" + 1,
                        "TimesCorrect" = "TimesCorrect" + CASE WHEN is_correct THEN 1 ELSE 0 END,
                        "UpdatedAt" = NOW()
                    WHERE "Id" = question_ids[j];
                END;
            END LOOP;
            
        END LOOP;
    END LOOP;
    
    RAISE NOTICE 'Created % student assessments with % total responses', 
        (SELECT COUNT(*) FROM student_assessments),
        (SELECT COUNT(*) FROM student_responses);
END $$;

\echo '  ✓ Student assessments and responses created'
\echo ''

-- ============================================================================
-- SUMMARY
-- ============================================================================
\echo '[3/3] Summary of demo data:'
\echo ''

SELECT 'Schools: ' || COUNT(*) FROM schools
UNION ALL
SELECT 'Users: ' || COUNT(*) FROM users
UNION ALL
SELECT 'Students: ' || COUNT(*) FROM students
UNION ALL
SELECT 'Classes: ' || COUNT(*) FROM classes
UNION ALL
SELECT 'Courses: ' || COUNT(*) FROM courses
UNION ALL
SELECT 'Assessments: ' || COUNT(*) FROM assessments
UNION ALL
SELECT 'Questions: ' || COUNT(*) FROM questions
UNION ALL
SELECT 'Student Assessments: ' || COUNT(*) FROM student_assessments
UNION ALL
SELECT 'Student Responses: ' || COUNT(*) FROM student_responses;

\echo ''
\echo '✓ Demo data loading complete!'
\echo ''
\echo 'Sample test credentials:'
\echo '  Student: emma.wilson@lincoln.edu (ID: 40000000-0000-0000-0000-000000000001)'
\echo '  Teacher: math.teacher@lincoln.edu (ID: 20000000-0000-0000-0000-000000000021)'
\echo '  School Admin: admin@lincoln.edu (ID: 20000000-0000-0000-0000-000000000011)'
\echo '  System Admin: admin@edumind.ai (ID: 20000000-0000-0000-0000-000000000001)'
\echo ''
