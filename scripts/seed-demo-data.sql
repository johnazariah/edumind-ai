-- ============================================================================
-- EduMind.AI Demo Data Seed Script
-- ============================================================================
-- This script populates the database with realistic test data for demonstration
-- Includes: Schools, Classes, Students, Courses, Assessments, Questions, and Responses
-- ============================================================================

-- Clean existing data (for re-running script)
TRUNCATE TABLE student_responses CASCADE;
TRUNCATE TABLE student_assessments CASCADE;
TRUNCATE TABLE questions CASCADE;
TRUNCATE TABLE assessments CASCADE;
TRUNCATE TABLE courses CASCADE;
TRUNCATE TABLE students CASCADE;
TRUNCATE TABLE classes CASCADE;
TRUNCATE TABLE schools CASCADE;
TRUNCATE TABLE users CASCADE;

-- ============================================================================
-- SCHOOLS
-- ============================================================================
INSERT INTO schools (id, name, district, created_at, updated_at) VALUES
('10000000-0000-0000-0000-000000000001', 'Lincoln High School', 'Central District', NOW(), NOW()),
('10000000-0000-0000-0000-000000000002', 'Washington Academy', 'North District', NOW(), NOW()),
('10000000-0000-0000-0000-000000000003', 'Roosevelt Institute', 'South District', NOW(), NOW());

-- ============================================================================
-- USERS
-- ============================================================================
-- System Admin
INSERT INTO users ("Id", "Email", "FullName", "Role", "CreatedAt", "UpdatedAt") VALUES
('20000000-0000-0000-0000-000000000001', 'admin@edumind.ai', 'System Administrator', 6, NOW(), NOW());

-- School Admins (one per school)
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "CreatedAt", "UpdatedAt") VALUES
('20000000-0000-0000-0000-000000000011', 'admin@lincoln.edu', 'Sarah Johnson', 2, '10000000-0000-0000-0000-000000000001', NOW(), NOW()),
('20000000-0000-0000-0000-000000000012', 'admin@washington.edu', 'Michael Chen', 2, '10000000-0000-0000-0000-000000000002', NOW(), NOW()),
('20000000-0000-0000-0000-000000000013', 'admin@roosevelt.edu', 'Emily Martinez', 2, '10000000-0000-0000-0000-000000000003', NOW(), NOW());

-- Teachers (2 per school - Math and Science)
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "CreatedAt", "UpdatedAt") VALUES
-- Lincoln High
('20000000-0000-0000-0000-000000000021', 'math.teacher@lincoln.edu', 'Dr. Robert Williams', 1, '10000000-0000-0000-0000-000000000001', NOW(), NOW()),
('20000000-0000-0000-0000-000000000022', 'science.teacher@lincoln.edu', 'Dr. Jennifer Davis', 1, '10000000-0000-0000-0000-000000000001', NOW(), NOW()),
-- Washington Academy
('20000000-0000-0000-0000-000000000023', 'math.teacher@washington.edu', 'Prof. James Anderson', 1, '10000000-0000-0000-0000-000000000002', NOW(), NOW()),
('20000000-0000-0000-0000-000000000024', 'science.teacher@washington.edu', 'Dr. Lisa Thompson', 1, '10000000-0000-0000-0000-000000000002', NOW(), NOW()),
-- Roosevelt Institute
('20000000-0000-0000-0000-000000000025', 'math.teacher@roosevelt.edu', 'Prof. David Garcia', 1, '10000000-0000-0000-0000-000000000003', NOW(), NOW()),
('20000000-0000-0000-0000-000000000026', 'science.teacher@roosevelt.edu', 'Dr. Maria Rodriguez', 1, '10000000-0000-0000-0000-000000000003', NOW(), NOW());

-- ============================================================================
-- CLASSES
-- ============================================================================
-- Lincoln High - Grade 10 Math and Science
INSERT INTO classes ("Id", "Name", "GradeLevel", "SchoolId", "TeacherId", "AcademicYear", "CreatedAt", "UpdatedAt") VALUES
('30000000-0000-0000-0000-000000000001', 'Grade 10 Math - Period 1', 10, '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000021', '2024-2025', NOW(), NOW()),
('30000000-0000-0000-0000-000000000002', 'Grade 10 Physics - Period 2', 10, '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000022', NOW(), NOW());

-- Washington Academy - Grade 11 Math and Science
INSERT INTO classes ("Id", "Name", "GradeLevel", "SchoolId", "TeacherId", "AcademicYear", "CreatedAt", "UpdatedAt") VALUES
('30000000-0000-0000-0000-000000000003', 'Grade 11 Advanced Math', 11, '10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000023', '2024-2025', NOW(), NOW()),
('30000000-0000-0000-0000-000000000004', 'Grade 11 Chemistry', 11, '10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000024', '2024-2025', NOW(), NOW());

-- Roosevelt Institute - Grade 9 Math and Science
INSERT INTO classes ("Id", "Name", "GradeLevel", "SchoolId", "TeacherId", "AcademicYear", "CreatedAt", "UpdatedAt") VALUES
('30000000-0000-0000-0000-000000000005', 'Grade 9 Algebra', 9, '10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000025', '2024-2025', NOW(), NOW()),
('30000000-0000-0000-0000-000000000006', 'Grade 9 Biology', 9, '10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000026', '2024-2025', NOW(), NOW());

-- ============================================================================
-- STUDENTS (8 per school = 24 total)
-- ============================================================================
-- Lincoln High Students (Class 1 & 2)
INSERT INTO students ("Id", "UserId", "SchoolId", "GradeLevel", "EnrollmentDate", "CreatedAt", "UpdatedAt") VALUES
('40000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000101', '10000000-0000-0000-0000-000000000001', 10, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000102', '10000000-0000-0000-0000-000000000001', 10, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000103', '10000000-0000-0000-0000-000000000001', 10, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000104', '10000000-0000-0000-0000-000000000001', 10, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000105', '10000000-0000-0000-0000-000000000001', 10, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000006', '20000000-0000-0000-0000-000000000106', '10000000-0000-0000-0000-000000000001', 10, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000007', '20000000-0000-0000-0000-000000000107', '10000000-0000-0000-0000-000000000001', 10, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000008', '20000000-0000-0000-0000-000000000108', '10000000-0000-0000-0000-000000000001', 10, '2024-09-01', NOW(), NOW());

-- Student Users for Lincoln High
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "CreatedAt", "UpdatedAt") VALUES
('20000000-0000-0000-0000-000000000101', 'emma.wilson@lincoln.edu', 'Emma Wilson', 0, '10000000-0000-0000-0000-000000000001', NOW(), NOW()),
('20000000-0000-0000-0000-000000000102', 'liam.brown@lincoln.edu', 'Liam Brown', 0, '10000000-0000-0000-0000-000000000001', NOW(), NOW()),
('20000000-0000-0000-0000-000000000103', 'olivia.jones@lincoln.edu', 'Olivia Jones', 0, '10000000-0000-0000-0000-000000000001', NOW(), NOW()),
('20000000-0000-0000-0000-000000000104', 'noah.garcia@lincoln.edu', 'Noah Garcia', 0, '10000000-0000-0000-0000-000000000001', NOW(), NOW()),
('20000000-0000-0000-0000-000000000105', 'ava.miller@lincoln.edu', 'Ava Miller', 0, '10000000-0000-0000-0000-000000000001', NOW(), NOW()),
('20000000-0000-0000-0000-000000000106', 'ethan.davis@lincoln.edu', 'Ethan Davis', 0, '10000000-0000-0000-0000-000000000001', NOW(), NOW()),
('20000000-0000-0000-0000-000000000107', 'sophia.rodriguez@lincoln.edu', 'Sophia Rodriguez', 0, '10000000-0000-0000-0000-000000000001', NOW(), NOW()),
('20000000-0000-0000-0000-000000000108', 'mason.martinez@lincoln.edu', 'Mason Martinez', 0, '10000000-0000-0000-0000-000000000001', NOW(), NOW());

-- Washington Academy Students (Class 3 & 4)
INSERT INTO students ("Id", "UserId", "SchoolId", "GradeLevel", "EnrollmentDate", "CreatedAt", "UpdatedAt") VALUES
('40000000-0000-0000-0000-000000000009', '20000000-0000-0000-0000-000000000201', '10000000-0000-0000-0000-000000000002', 11, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000010', '20000000-0000-0000-0000-000000000202', '10000000-0000-0000-0000-000000000002', 11, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000011', '20000000-0000-0000-0000-000000000203', '10000000-0000-0000-0000-000000000002', 11, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000012', '20000000-0000-0000-0000-000000000204', '10000000-0000-0000-0000-000000000002', 11, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000013', '20000000-0000-0000-0000-000000000205', '10000000-0000-0000-0000-000000000002', 11, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000014', '20000000-0000-0000-0000-000000000206', '10000000-0000-0000-0000-000000000002', 11, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000015', '20000000-0000-0000-0000-000000000207', '10000000-0000-0000-0000-000000000002', 11, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000016', '20000000-0000-0000-0000-000000000208', '10000000-0000-0000-0000-000000000002', 11, '2024-09-01', NOW(), NOW());

-- Student Users for Washington Academy
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "CreatedAt", "UpdatedAt") VALUES
('20000000-0000-0000-0000-000000000201', 'isabella.hernandez@washington.edu', 'Isabella Hernandez', 0, '10000000-0000-0000-0000-000000000002', NOW(), NOW()),
('20000000-0000-0000-0000-000000000202', 'william.lopez@washington.edu', 'William Lopez', 0, '10000000-0000-0000-0000-000000000002', NOW(), NOW()),
('20000000-0000-0000-0000-000000000203', 'mia.gonzalez@washington.edu', 'Mia Gonzalez', 0, '10000000-0000-0000-0000-000000000002', NOW(), NOW()),
('20000000-0000-0000-0000-000000000204', 'james.wilson@washington.edu', 'James Wilson', 0, '10000000-0000-0000-0000-000000000002', NOW(), NOW()),
('20000000-0000-0000-0000-000000000205', 'charlotte.anderson@washington.edu', 'Charlotte Anderson', 0, '10000000-0000-0000-0000-000000000002', NOW(), NOW()),
('20000000-0000-0000-0000-000000000206', 'benjamin.thomas@washington.edu', 'Benjamin Thomas', 0, '10000000-0000-0000-0000-000000000002', NOW(), NOW()),
('20000000-0000-0000-0000-000000000207', 'amelia.taylor@washington.edu', 'Amelia Taylor', 0, '10000000-0000-0000-0000-000000000002', NOW(), NOW()),
('20000000-0000-0000-0000-000000000208', 'lucas.moore@washington.edu', 'Lucas Moore', 0, '10000000-0000-0000-0000-000000000002', NOW(), NOW());

-- Roosevelt Institute Students (Class 5 & 6)
INSERT INTO students ("Id", "UserId", "SchoolId", "GradeLevel", "EnrollmentDate", "CreatedAt", "UpdatedAt") VALUES
('40000000-0000-0000-0000-000000000017', '20000000-0000-0000-0000-000000000301', '10000000-0000-0000-0000-000000000003', 9, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000018', '20000000-0000-0000-0000-000000000302', '10000000-0000-0000-0000-000000000003', 9, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000019', '20000000-0000-0000-0000-000000000303', '10000000-0000-0000-0000-000000000003', 9, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000020', '20000000-0000-0000-0000-000000000304', '10000000-0000-0000-0000-000000000003', 9, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000021', '20000000-0000-0000-0000-000000000305', '10000000-0000-0000-0000-000000000003', 9, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000022', '20000000-0000-0000-0000-000000000306', '10000000-0000-0000-0000-000000000003', 9, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000023', '20000000-0000-0000-0000-000000000307', '10000000-0000-0000-0000-000000000003', 9, '2024-09-01', NOW(), NOW()),
('40000000-0000-0000-0000-000000000024', '20000000-0000-0000-0000-000000000308', '10000000-0000-0000-0000-000000000003', 9, '2024-09-01', NOW(), NOW());

-- Student Users for Roosevelt Institute
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "CreatedAt", "UpdatedAt") VALUES
('20000000-0000-0000-0000-000000000301', 'harper.jackson@roosevelt.edu', 'Harper Jackson', 0, '10000000-0000-0000-0000-000000000003', NOW(), NOW()),
('20000000-0000-0000-0000-000000000302', 'alexander.white@roosevelt.edu', 'Alexander White', 0, '10000000-0000-0000-0000-000000000003', NOW(), NOW()),
('20000000-0000-0000-0000-000000000303', 'evelyn.harris@roosevelt.edu', 'Evelyn Harris', 0, '10000000-0000-0000-0000-000000000003', NOW(), NOW()),
('20000000-0000-0000-0000-000000000304', 'henry.martin@roosevelt.edu', 'Henry Martin', 0, '10000000-0000-0000-0000-000000000003', NOW(), NOW()),
('20000000-0000-0000-0000-000000000305', 'ella.thompson@roosevelt.edu', 'Ella Thompson', 0, '10000000-0000-0000-0000-000000000003', NOW(), NOW()),
('20000000-0000-0000-0000-000000000306', 'sebastian.white@roosevelt.edu', 'Sebastian White', 0, '10000000-0000-0000-0000-000000000003', NOW(), NOW()),
('20000000-0000-0000-0000-000000000307', 'scarlett.lee@roosevelt.edu', 'Scarlett Lee', 0, '10000000-0000-0000-0000-000000000003', NOW(), NOW()),
('20000000-0000-0000-0000-000000000308', 'jack.walker@roosevelt.edu', 'Jack Walker', 0, '10000000-0000-0000-0000-000000000003', NOW(), NOW());

-- ============================================================================
-- COURSES
-- ============================================================================
INSERT INTO courses ("Id", "Name", "Subject", "Description", "GradeLevel", "CreatedAt", "UpdatedAt") VALUES
-- Mathematics Courses
('50000000-0000-0000-0000-000000000001', 'Algebra II', 0, 'Intermediate algebra covering quadratic equations, polynomials, and functions', 10, NOW(), NOW()),
('50000000-0000-0000-0000-000000000002', 'Pre-Calculus', 0, 'Advanced mathematics preparing students for calculus', 11, NOW(), NOW()),
('50000000-0000-0000-0000-000000000003', 'Algebra I', 0, 'Introduction to algebraic concepts and linear equations', 9, NOW(), NOW()),
-- Physics Courses
('50000000-0000-0000-0000-000000000004', 'Physics I', 1, 'Introduction to mechanics, energy, and motion', 10, NOW(), NOW()),
-- Chemistry Courses
('50000000-0000-0000-0000-000000000005', 'Chemistry I', 2, 'Introduction to chemical principles and reactions', 11, NOW(), NOW()),
-- Biology Courses
('50000000-0000-0000-0000-000000000006', 'Biology I', 3, 'Introduction to life sciences and cellular biology', 9, NOW(), NOW());

-- ============================================================================
-- ASSESSMENTS (3-4 per course)
-- ============================================================================
-- Algebra II Assessments (Lincoln High - Grade 10)
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "DifficultyLevel", "EstimatedDurationMinutes", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', 'Quadratic Equations Assessment', 'Test covering quadratic equations, factoring, and the quadratic formula', 2, 45, NOW(), NOW()),
('60000000-0000-0000-0000-000000000002', '50000000-0000-0000-0000-000000000001', 'Polynomial Functions Quiz', 'Quiz on polynomial operations and graphing', 1, 30, NOW(), NOW()),
('60000000-0000-0000-0000-000000000003', '50000000-0000-0000-0000-000000000001', 'Systems of Equations Test', 'Comprehensive test on solving systems using various methods', 3, 60, NOW(), NOW());

-- Pre-Calculus Assessments (Washington Academy - Grade 11)
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "DifficultyLevel", "EstimatedDurationMinutes", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000004', '50000000-0000-0000-0000-000000000002', 'Trigonometry Unit Test', 'Assessment on trigonometric functions and identities', 3, 50, NOW(), NOW()),
('60000000-0000-0000-0000-000000000005', '50000000-0000-0000-0000-000000000002', 'Limits and Continuity Quiz', 'Introduction to calculus concepts', 4, 40, NOW(), NOW());

-- Algebra I Assessments (Roosevelt Institute - Grade 9)
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "DifficultyLevel", "EstimatedDurationMinutes", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000006', '50000000-0000-0000-0000-000000000003', 'Linear Equations Test', 'Test on solving and graphing linear equations', 1, 40, NOW(), NOW()),
('60000000-0000-0000-0000-000000000007', '50000000-0000-0000-0000-000000000003', 'Variables and Expressions Quiz', 'Basic algebra concepts and simplification', 0, 25, NOW(), NOW());

-- Physics Assessments (Lincoln High - Grade 10)
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "DifficultyLevel", "EstimatedDurationMinutes", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000008', '50000000-0000-0000-0000-000000000004', 'Motion and Forces Test', 'Assessment on Newtons laws and kinematics', 2, 50, NOW(), NOW()),
('60000000-0000-0000-0000-000000000009', '50000000-0000-0000-0000-000000000004', 'Energy and Work Quiz', 'Quiz on energy conservation and work-energy theorem', 2, 35, NOW(), NOW());

-- Chemistry Assessments (Washington Academy - Grade 11)
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "DifficultyLevel", "EstimatedDurationMinutes", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000010', '50000000-0000-0000-0000-000000000005', 'Chemical Reactions Test', 'Assessment on balancing equations and reaction types', 2, 45, NOW(), NOW()),
('60000000-0000-0000-0000-000000000011', '50000000-0000-0000-0000-000000000005', 'Stoichiometry Quiz', 'Quiz on mole calculations and stoichiometry', 3, 40, NOW(), NOW());

-- Biology Assessments (Roosevelt Institute - Grade 9)
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "DifficultyLevel", "EstimatedDurationMinutes", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000012', '50000000-0000-0000-0000-000000000006', 'Cell Structure Test', 'Test on cell organelles and their functions', 1, 40, NOW(), NOW()),
('60000000-0000-0000-0000-000000000013', '50000000-0000-0000-0000-000000000006', 'DNA and Genetics Quiz', 'Quiz on DNA structure and basic genetics', 2, 35, NOW(), NOW());

-- Note: Questions and Student Assessments/Responses will be added via the seed shell script
-- to allow for more complex logic and randomization
