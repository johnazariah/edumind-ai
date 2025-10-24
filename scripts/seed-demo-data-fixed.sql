-- Fixed seed data for EduMind.AI demo
-- Matches actual database schema (no EnrollmentDate or DifficultyLevel columns)

-- Clean existing data
TRUNCATE TABLE student_responses CASCADE;
TRUNCATE TABLE student_assessments CASCADE;
TRUNCATE TABLE questions CASCADE;
TRUNCATE TABLE assessments CASCADE;
TRUNCATE TABLE students CASCADE;
TRUNCATE TABLE classes CASCADE;
TRUNCATE TABLE courses CASCADE;
TRUNCATE TABLE teachers CASCADE;
TRUNCATE TABLE agent_tasks CASCADE;

-- Insert sample schools (using UUIDs)
INSERT INTO courses ("Id", "SchoolId", "TeacherId", "Name", "Subject", "GradeLevel", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440001'::uuid, NULL, '550e8400-e29b-41d4-a716-446655440011'::uuid, 'Mathematics Grade 8', 1, 8, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440002'::uuid, NULL, '550e8400-e29b-41d4-a716-446655440012'::uuid, 'English Grade 7', 2, 7, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440003'::uuid, NULL, '550e8400-e29b-41d4-a716-446655440013'::uuid, 'Science Grade 9', 3, 9, true, NOW(), NOW());

-- Insert sample teachers
INSERT INTO teachers ("Id", "UserId", "SchoolId", "Subject", "GradeLevel", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440011'::uuid, '550e8400-e29b-41d4-a716-446655440021'::uuid, NULL, 1, 8, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440012'::uuid, '550e8400-e29b-41d4-a716-446655440022'::uuid, NULL, 2, 7, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440013'::uuid, '550e8400-e29b-41d4-a716-446655440023'::uuid, NULL, 3, 9, true, NOW(), NOW());

-- Insert sample classes
INSERT INTO classes ("Id", "CourseId", "Name", "TeacherId", "SchoolId", "StudentIds", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440031'::uuid, '550e8400-e29b-41d4-a716-446655440001'::uuid, 'Math 8A', '550e8400-e29b-41d4-a716-446655440011'::uuid, NULL, '[]', true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440032'::uuid, '550e8400-e29b-41d4-a716-446655440002'::uuid, 'English 7B', '550e8400-e29b-41d4-a716-446655440012'::uuid, NULL, '[]', true, NOW(), NOW());

-- Insert sample students (matching actual schema - no EnrollmentDate)
INSERT INTO students ("Id", "UserId", "SchoolId", "ClassIds", "GradeLevel", "DateOfBirth", "ParentalConsentGranted", "ParentEmail", "SubscriptionTier", "SubscriptionExpiresAt", "Level", "XpPoints", "DailyStreak", "LastActivityDate", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440041'::uuid, '550e8400-e29b-41d4-a716-446655440051'::uuid, NULL, '["550e8400-e29b-41d4-a716-446655440031"]', 8, '2010-03-15', true, 'parent1@example.com', 1, '2025-12-31 23:59:59+00', 1, 150, 5, '2024-01-15', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440042'::uuid, '550e8400-e29b-41d4-a716-446655440052'::uuid, NULL, '["550e8400-e29b-41d4-a716-446655440032"]', 7, '2011-07-22', true, 'parent2@example.com', 1, '2025-12-31 23:59:59+00', 1, 89, 3, '2024-01-14', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440043'::uuid, '550e8400-e29b-41d4-a716-446655440053'::uuid, NULL, '["550e8400-e29b-41d4-a716-446655440031"]', 8, '2010-11-08', true, 'parent3@example.com', 2, '2025-12-31 23:59:59+00', 2, 230, 7, '2024-01-15', NOW(), NOW());

-- Insert sample assessments (matching actual schema - no DifficultyLevel)
INSERT INTO assessments ("Id", "CourseId", "SchoolId", "Title", "Description", "AssessmentType", "Subject", "GradeLevel", "Topics", "QuestionIds", "TotalPoints", "TimeLimitMinutes", "PassingScorePercentage", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440061'::uuid, '550e8400-e29b-41d4-a716-446655440001'::uuid, NULL, 'Algebra Fundamentals Quiz', 'Basic algebra concepts including variables, equations, and solving for x', 1, 1, 8, '["algebra", "equations", "variables"]', '[]', 100, 30, 70, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440062'::uuid, '550e8400-e29b-41d4-a716-446655440002'::uuid, NULL, 'Reading Comprehension Test', 'Assessment of reading skills through short passages and questions', 1, 2, 7, '["reading", "comprehension", "analysis"]', '[]', 80, 45, 75, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440063'::uuid, '550e8400-e29b-41d4-a716-446655440003'::uuid, NULL, 'Basic Chemistry Quiz', 'Introduction to atoms, molecules, and chemical reactions', 1, 3, 9, '["chemistry", "atoms", "molecules", "reactions"]', '[]', 90, 40, 65, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440064'::uuid, '550e8400-e29b-41d4-a716-446655440001'::uuid, NULL, 'Geometry Basics', 'Understanding shapes, angles, and basic geometric principles', 1, 1, 8, '["geometry", "shapes", "angles"]', '[]', 75, 35, 70, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440065'::uuid, '550e8400-e29b-41d4-a716-446655440002'::uuid, NULL, 'Grammar and Writing', 'Assessment of grammar rules and basic writing skills', 1, 2, 7, '["grammar", "writing", "punctuation"]', '[]', 60, 25, 80, true, NOW(), NOW());

-- Insert sample questions
INSERT INTO questions ("Id", "AssessmentId", "QuestionType", "QuestionText", "CorrectAnswer", "IncorrectAnswers", "Explanation", "Points", "TimeEstimateSeconds", "DifficultyLevel", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440071'::uuid, '550e8400-e29b-41d4-a716-446655440061'::uuid, 1, 'Solve for x: 2x + 5 = 13', 'x = 4', '["x = 3", "x = 5", "x = 6"]', 'Subtract 5 from both sides to get 2x = 8, then divide by 2 to get x = 4', 10, 120, 1, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440072'::uuid, '550e8400-e29b-41d4-a716-446655440061'::uuid, 1, 'What is the value of y when y - 7 = 12?', 'y = 19', '["y = 5", "y = 17", "y = 20"]', 'Add 7 to both sides to get y = 19', 10, 90, 1, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440073'::uuid, '550e8400-e29b-41d4-a716-446655440062'::uuid, 1, 'What is the main idea of the passage about renewable energy?', 'Renewable energy sources are important for environmental sustainability', '["Solar panels are expensive", "Wind energy is unreliable", "Coal is still the best option"]', 'The passage emphasizes the environmental benefits and long-term importance of renewable energy', 15, 180, 2, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440074'::uuid, '550e8400-e29b-41d4-a716-446655440063'::uuid, 1, 'How many protons does a carbon atom have?', '6', '["4", "8", "12"]', 'Carbon has an atomic number of 6, which equals the number of protons', 10, 60, 1, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440075'::uuid, '550e8400-e29b-41d4-a716-446655440064'::uuid, 1, 'What is the sum of angles in a triangle?', '180 degrees', '["90 degrees", "270 degrees", "360 degrees"]', 'The sum of all interior angles in any triangle is always 180 degrees', 10, 90, 1, NOW(), NOW());

-- Update assessments to include the question IDs
UPDATE assessments SET "QuestionIds" = '["550e8400-e29b-41d4-a716-446655440071", "550e8400-e29b-41d4-a716-446655440072"]' WHERE "Id" = '550e8400-e29b-41d4-a716-446655440061'::uuid;
UPDATE assessments SET "QuestionIds" = '["550e8400-e29b-41d4-a716-446655440073"]' WHERE "Id" = '550e8400-e29b-41d4-a716-446655440062'::uuid;
UPDATE assessments SET "QuestionIds" = '["550e8400-e29b-41d4-a716-446655440074"]' WHERE "Id" = '550e8400-e29b-41d4-a716-446655440063'::uuid;
UPDATE assessments SET "QuestionIds" = '["550e8400-e29b-41d4-a716-446655440075"]' WHERE "Id" = '550e8400-e29b-41d4-a716-446655440064'::uuid;
UPDATE assessments SET "QuestionIds" = '[]' WHERE "Id" = '550e8400-e29b-41d4-a716-446655440065'::uuid;

COMMIT;