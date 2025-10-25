-- Final corrected seed data for EduMind.AI demo
-- Matches actual database schema exactly

-- Clean existing data
TRUNCATE TABLE student_responses CASCADE;
TRUNCATE TABLE student_assessments CASCADE;
TRUNCATE TABLE questions CASCADE;
TRUNCATE TABLE assessments CASCADE;
TRUNCATE TABLE students CASCADE;
TRUNCATE TABLE classes CASCADE;
TRUNCATE TABLE courses CASCADE;

-- Insert sample courses (matching actual schema)
INSERT INTO courses ("Id", "Name", "Code", "Subject", "GradeLevel", "Description", "LearningObjectives", "Topics", "IsActive", "CreatedAt", "UpdatedAt", "CourseAdminId", "BoardName", "Metadata", "ModuleName") VALUES
('550e8400-e29b-41d4-a716-446655440001'::uuid, 'Mathematics Grade 8', 'MATH8-001', 1, 8, 'Comprehensive mathematics course covering algebra, geometry, and statistics', '["solve_linear_equations", "understand_geometric_principles", "analyze_data"]', '["algebra", "geometry", "statistics"]', true, NOW(), NOW(), NULL, 'Common Core', '{}', 'Core Mathematics'),
('550e8400-e29b-41d4-a716-446655440002'::uuid, 'English Grade 7', 'ENG7-001', 2, 7, 'English language arts focusing on reading comprehension and writing skills', '["improve_reading_comprehension", "develop_writing_skills", "analyze_literature"]', '["reading", "writing", "literature", "grammar"]', true, NOW(), NOW(), NULL, 'Common Core', '{}', 'Language Arts'),
('550e8400-e29b-41d4-a716-446655440003'::uuid, 'Science Grade 9', 'SCI9-001', 3, 9, 'Introduction to physical science, chemistry, and biology concepts', '["understand_scientific_method", "explore_chemistry_basics", "study_biology_fundamentals"]', '["chemistry", "biology", "physics", "scientific_method"]', true, NOW(), NOW(), NULL, 'NGSS', '{}', 'General Science');

-- Insert sample classes (matching actual schema)
INSERT INTO classes ("Id", "SchoolId", "Name", "Code", "GradeLevel", "Subject", "TeacherIds", "StudentIds", "AcademicYear", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440031'::uuid, '550e8400-e29b-41d4-a716-446655440201'::uuid, 'Math 8A Morning', 'MATH8A-M', 8, 1, '["550e8400-e29b-41d4-a716-446655440011"]', '["550e8400-e29b-41d4-a716-446655440041", "550e8400-e29b-41d4-a716-446655440043"]', '2024-2025', true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440032'::uuid, '550e8400-e29b-41d4-a716-446655440201'::uuid, 'English 7B Afternoon', 'ENG7B-A', 7, 2, '["550e8400-e29b-41d4-a716-446655440012"]', '["550e8400-e29b-41d4-a716-446655440042"]', '2024-2025', true, NOW(), NOW());

-- Insert sample students (matching actual schema)
INSERT INTO students ("Id", "UserId", "SchoolId", "ClassIds", "GradeLevel", "DateOfBirth", "ParentalConsentGranted", "ParentEmail", "SubscriptionTier", "SubscriptionExpiresAt", "Level", "XpPoints", "DailyStreak", "LastActivityDate", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440041'::uuid, '550e8400-e29b-41d4-a716-446655440051'::uuid, '550e8400-e29b-41d4-a716-446655440201'::uuid, '["550e8400-e29b-41d4-a716-446655440031"]', 8, '2010-03-15', true, 'parent1@example.com', 1, '2025-12-31 23:59:59+00', 1, 150, 5, '2024-01-15', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440042'::uuid, '550e8400-e29b-41d4-a716-446655440052'::uuid, '550e8400-e29b-41d4-a716-446655440201'::uuid, '["550e8400-e29b-41d4-a716-446655440032"]', 7, '2011-07-22', true, 'parent2@example.com', 1, '2025-12-31 23:59:59+00', 1, 89, 3, '2024-01-14', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440043'::uuid, '550e8400-e29b-41d4-a716-446655440053'::uuid, '550e8400-e29b-41d4-a716-446655440201'::uuid, '["550e8400-e29b-41d4-a716-446655440031"]', 8, '2010-11-08', true, 'parent3@example.com', 2, '2025-12-31 23:59:59+00', 2, 230, 7, '2024-01-15', NOW(), NOW());

-- Insert sample assessments (matching actual schema)
INSERT INTO assessments ("Id", "CourseId", "SchoolId", "Title", "Description", "AssessmentType", "Subject", "GradeLevel", "Topics", "QuestionIds", "TotalPoints", "TimeLimitMinutes", "PassingScorePercentage", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440061'::uuid, '550e8400-e29b-41d4-a716-446655440001'::uuid, '550e8400-e29b-41d4-a716-446655440201'::uuid, 'Algebra Fundamentals Quiz', 'Basic algebra concepts including variables, equations, and solving for x', 1, 1, 8, '["algebra", "equations", "variables"]', '[]', 100, 30, 70, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440062'::uuid, '550e8400-e29b-41d4-a716-446655440002'::uuid, '550e8400-e29b-41d4-a716-446655440201'::uuid, 'Reading Comprehension Test', 'Assessment of reading skills through short passages and questions', 1, 2, 7, '["reading", "comprehension", "analysis"]', '[]', 80, 45, 75, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440063'::uuid, '550e8400-e29b-41d4-a716-446655440003'::uuid, '550e8400-e29b-41d4-a716-446655440201'::uuid, 'Basic Chemistry Quiz', 'Introduction to atoms, molecules, and chemical reactions', 1, 3, 9, '["chemistry", "atoms", "molecules", "reactions"]', '[]', 90, 40, 65, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440064'::uuid, '550e8400-e29b-41d4-a716-446655440001'::uuid, '550e8400-e29b-41d4-a716-446655440201'::uuid, 'Geometry Basics', 'Understanding shapes, angles, and basic geometric principles', 1, 1, 8, '["geometry", "shapes", "angles"]', '[]', 75, 35, 70, true, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440065'::uuid, '550e8400-e29b-41d4-a716-446655440002'::uuid, '550e8400-e29b-41d4-a716-446655440201'::uuid, 'Grammar and Writing', 'Assessment of grammar rules and basic writing skills', 1, 2, 7, '["grammar", "writing", "punctuation"]', '[]', 60, 25, 80, true, NOW(), NOW());

-- Insert sample questions (matching actual schema)
INSERT INTO questions ("Id", "CourseId", "QuestionText", "QuestionType", "Subject", "GradeLevel", "DifficultyLevel", "Topics", "LearningObjectives", "AnswerOptions", "CorrectAnswer", "Explanation", "Points", "IrtDiscrimination", "IrtDifficulty", "IrtGuessing", "IsActive", "TimesAnswered", "TimesCorrect", "CreatedAt", "UpdatedAt", "IsAiGenerated", "ContentHash", "BoardName", "Metadata", "ModuleName") VALUES
('550e8400-e29b-41d4-a716-446655440071'::uuid, '550e8400-e29b-41d4-a716-446655440001'::uuid, 'Solve for x: 2x + 5 = 13', 1, 1, 8, 1, '["algebra", "equations"]', '["solve_linear_equations"]', '["x = 3", "x = 4", "x = 5", "x = 6"]', 'x = 4', 'Subtract 5 from both sides to get 2x = 8, then divide by 2 to get x = 4', 10, 1.2, 0.5, 0.25, true, 0, 0, NOW(), NOW(), false, 'hash1', 'Common Core', '{}', 'Core Mathematics'),
('550e8400-e29b-41d4-a716-446655440072'::uuid, '550e8400-e29b-41d4-a716-446655440001'::uuid, 'What is the value of y when y - 7 = 12?', 1, 1, 8, 1, '["algebra", "equations"]', '["solve_linear_equations"]', '["y = 5", "y = 17", "y = 19", "y = 20"]', 'y = 19', 'Add 7 to both sides to get y = 19', 10, 1.1, 0.3, 0.25, true, 0, 0, NOW(), NOW(), false, 'hash2', 'Common Core', '{}', 'Core Mathematics'),
('550e8400-e29b-41d4-a716-446655440073'::uuid, '550e8400-e29b-41d4-a716-446655440002'::uuid, 'What is the main idea of a passage about renewable energy?', 1, 2, 7, 2, '["reading", "comprehension"]', '["improve_reading_comprehension"]', '["Solar panels are expensive", "Renewable energy sources are important for environmental sustainability", "Wind energy is unreliable", "Coal is still the best option"]', 'Renewable energy sources are important for environmental sustainability', 'The passage emphasizes the environmental benefits and long-term importance of renewable energy', 15, 1.3, 0.7, 0.25, true, 0, 0, NOW(), NOW(), false, 'hash3', 'Common Core', '{}', 'Language Arts'),
('550e8400-e29b-41d4-a716-446655440074'::uuid, '550e8400-e29b-41d4-a716-446655440003'::uuid, 'How many protons does a carbon atom have?', 1, 3, 9, 1, '["chemistry", "atoms"]', '["understand_atomic_structure"]', '["4", "6", "8", "12"]', '6', 'Carbon has an atomic number of 6, which equals the number of protons', 10, 1.4, 0.2, 0.25, true, 0, 0, NOW(), NOW(), false, 'hash4', 'NGSS', '{}', 'General Science'),
('550e8400-e29b-41d4-a716-446655440075'::uuid, '550e8400-e29b-41d4-a716-446655440001'::uuid, 'What is the sum of angles in a triangle?', 1, 1, 8, 1, '["geometry", "angles"]', '["understand_geometric_principles"]', '["90 degrees", "180 degrees", "270 degrees", "360 degrees"]', '180 degrees', 'The sum of all interior angles in any triangle is always 180 degrees', 10, 1.0, 0.4, 0.25, true, 0, 0, NOW(), NOW(), false, 'hash5', 'Common Core', '{}', 'Core Mathematics');

-- Update assessments to include the question IDs
UPDATE assessments SET "QuestionIds" = '["550e8400-e29b-41d4-a716-446655440071", "550e8400-e29b-41d4-a716-446655440072"]' WHERE "Id" = '550e8400-e29b-41d4-a716-446655440061'::uuid;
UPDATE assessments SET "QuestionIds" = '["550e8400-e29b-41d4-a716-446655440073"]' WHERE "Id" = '550e8400-e29b-41d4-a716-446655440062'::uuid;
UPDATE assessments SET "QuestionIds" = '["550e8400-e29b-41d4-a716-446655440074"]' WHERE "Id" = '550e8400-e29b-41d4-a716-446655440063'::uuid;
UPDATE assessments SET "QuestionIds" = '["550e8400-e29b-41d4-a716-446655440075"]' WHERE "Id" = '550e8400-e29b-41d4-a716-446655440064'::uuid;
UPDATE assessments SET "QuestionIds" = '[]' WHERE "Id" = '550e8400-e29b-41d4-a716-446655440065'::uuid;

COMMIT;