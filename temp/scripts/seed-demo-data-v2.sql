-- ============================================================================
-- EduMind.AI Demo Data Seed Script (Updated for actual schema)
-- ============================================================================
-- This script populates the database with realistic test data for demonstration
-- Includes: Schools, Users, Classes, Students, Courses, Assessments
-- Note: Questions and StudentAssessments will be added via the shell script
-- ============================================================================

-- Clean existing data (for re-running script)
TRUNCATE TABLE student_responses CASCADE;
TRUNCATE TABLE student_assessments CASCADE;
TRUNCATE TABLE questions CASCADE;
TRUNCATE TABLE assessments CASCADE;
TRUNCATE TABLE students CASCADE;
TRUNCATE TABLE classes CASCADE;
TRUNCATE TABLE courses CASCADE;
TRUNCATE TABLE users CASCADE;
TRUNCATE TABLE schools CASCADE;

-- ============================================================================
-- SCHOOLS
-- ============================================================================
INSERT INTO schools ("Id", "Name", "Code", "Address", "ContactEmail", "ContactPhone", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('10000000-0000-0000-0000-000000000001', 'Lincoln High School', 'LHS', '123 Main St, Springfield, IL 62701', 'contact@lincoln.edu', '217-555-0101', true, NOW(), NOW()),
('10000000-0000-0000-0000-000000000002', 'Washington Academy', 'WA', '456 Oak Ave, Springfield, IL 62702', 'contact@washington.edu', '217-555-0102', true, NOW(), NOW()),
('10000000-0000-0000-0000-000000000003', 'Roosevelt Institute', 'RI', '789 Elm Dr, Springfield, IL 62703', 'contact@roosevelt.edu', '217-555-0103', true, NOW(), NOW());

-- ============================================================================
-- USERS
-- ============================================================================
-- System Admin
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "IsActive", "CreatedAt", "UpdatedAt", "ExternalId") VALUES
('20000000-0000-0000-0000-000000000001', 'admin@edumind.ai', 'System Administrator', 6, NULL, true, NOW(), NOW(), 'ext-sysadmin-001');

-- School Admins (one per school)
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "IsActive", "CreatedAt", "UpdatedAt", "ExternalId") VALUES
('20000000-0000-0000-0000-000000000011', 'admin@lincoln.edu', 'Sarah Johnson', 2, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-admin-lincoln'),
('20000000-0000-0000-0000-000000000012', 'admin@washington.edu', 'Michael Chen', 2, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-admin-washington'),
('20000000-0000-0000-0000-000000000013', 'admin@roosevelt.edu', 'Emily Martinez', 2, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-admin-roosevelt');

-- Teachers (2 per school - Math and Science)
-- Lincoln High School Teachers
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "IsActive", "CreatedAt", "UpdatedAt", "ExternalId") VALUES
('20000000-0000-0000-0000-000000000021', 'math.teacher@lincoln.edu', 'Dr. Robert Williams', 1, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-teacher-lincoln-math'),
('20000000-0000-0000-0000-000000000022', 'science.teacher@lincoln.edu', 'Dr. Lisa Anderson', 1, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-teacher-lincoln-science');

-- Washington Academy Teachers
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "IsActive", "CreatedAt", "UpdatedAt", "ExternalId") VALUES
('20000000-0000-0000-0000-000000000023', 'math.teacher@washington.edu', 'Prof. David Kim', 1, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-teacher-washington-math'),
('20000000-0000-0000-0000-000000000024', 'science.teacher@washington.edu', 'Dr. Jennifer Lee', 1, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-teacher-washington-science');

-- Roosevelt Institute Teachers
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "IsActive", "CreatedAt", "UpdatedAt", "ExternalId") VALUES
('20000000-0000-0000-0000-000000000025', 'math.teacher@roosevelt.edu', 'Prof. James Taylor', 1, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-teacher-roosevelt-math'),
('20000000-0000-0000-0000-000000000026', 'science.teacher@roosevelt.edu', 'Dr. Maria Garcia', 1, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-teacher-roosevelt-science');

-- ============================================================================
-- COURSES
-- ============================================================================
-- Subject Enum: Mathematics=0, Physics=1, Chemistry=2, Biology=3, English=4
INSERT INTO courses ("Id", "Name", "Code", "Subject", "GradeLevel", "Description", "LearningObjectives", "Topics", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('30000000-0000-0000-0000-000000000001', 'Algebra I', 'MATH-ALG1', 0, 9, 'Foundational algebra course covering linear equations, polynomials, and functions', '["Solve linear equations", "Graph functions", "Factor polynomials"]', '["Linear Equations", "Graphing", "Polynomials", "Functions"]', true, NOW(), NOW()),
('30000000-0000-0000-0000-000000000002', 'Algebra II', 'MATH-ALG2', 0, 10, 'Advanced algebra including quadratics, exponentials, and logarithms', '["Solve quadratic equations", "Work with exponential functions", "Apply logarithms"]', '["Quadratics", "Exponentials", "Logarithms", "Sequences"]', true, NOW(), NOW()),
('30000000-0000-0000-0000-000000000003', 'Pre-Calculus', 'MATH-PRECALC', 0, 11, 'Preparation for calculus with trigonometry and analytical geometry', '["Master trigonometry", "Understand limits", "Analyze functions"]', '["Trigonometry", "Limits", "Vectors", "Conic Sections"]', true, NOW(), NOW()),
('30000000-0000-0000-0000-000000000004', 'Physics I', 'PHYS-1', 1, 10, 'Introduction to mechanics, energy, and motion', '["Apply Newton''s laws", "Calculate energy", "Analyze motion"]', '["Kinematics", "Forces", "Energy", "Momentum"]', true, NOW(), NOW()),
('30000000-0000-0000-0000-000000000005', 'Chemistry I', 'CHEM-1', 2, 10, 'Basic chemistry covering atomic structure, bonding, and reactions', '["Understand atomic structure", "Balance equations", "Calculate stoichiometry"]', '["Atoms", "Bonding", "Reactions", "Stoichiometry"]', true, NOW(), NOW()),
('30000000-0000-0000-0000-000000000006', 'Biology I', 'BIO-1', 3, 9, 'Introduction to life sciences and cellular biology', '["Understand cell structure", "Explain photosynthesis", "Describe genetics"]', '["Cells", "Genetics", "Evolution", "Ecology"]', true, NOW(), NOW());

-- ============================================================================
-- CLASSES
-- ============================================================================
-- Lincoln High School Classes
-- TeacherIds and StudentIds are stored as JSON arrays (will be filled after students are created)
INSERT INTO classes ("Id", "SchoolId", "Name", "Code", "GradeLevel", "Subject", "TeacherIds", "StudentIds", "AcademicYear", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('50000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'Algebra I - Period 1', 'LHS-ALG1-P1', 9, 0, '["20000000-0000-0000-0000-000000000021"]', '[]', '2024-2025', true, NOW(), NOW()),
('50000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 'Biology I - Period 2', 'LHS-BIO1-P2', 9, 3, '["20000000-0000-0000-0000-000000000022"]', '[]', '2024-2025', true, NOW(), NOW());

-- Washington Academy Classes
INSERT INTO classes ("Id", "SchoolId", "Name", "Code", "GradeLevel", "Subject", "TeacherIds", "StudentIds", "AcademicYear", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('50000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000002', 'Algebra II - Advanced', 'WA-ALG2-ADV', 10, 0, '["20000000-0000-0000-0000-000000000023"]', '[]', '2024-2025', true, NOW(), NOW()),
('50000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000002', 'Physics I - Honors', 'WA-PHYS1-HON', 10, 1, '["20000000-0000-0000-0000-000000000024"]', '[]', '2024-2025', true, NOW(), NOW());

-- Roosevelt Institute Classes
INSERT INTO classes ("Id", "SchoolId", "Name", "Code", "GradeLevel", "Subject", "TeacherIds", "StudentIds", "AcademicYear", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('50000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000003', 'Pre-Calculus', 'RI-PRECALC', 11, 0, '["20000000-0000-0000-0000-000000000025"]', '[]', '2024-2025', true, NOW(), NOW()),
('50000000-0000-0000-0000-000000000006', '10000000-0000-0000-0000-000000000003', 'Chemistry I', 'RI-CHEM1', 10, 2, '["20000000-0000-0000-0000-000000000026"]', '[]', '2024-2025', true, NOW(), NOW());

-- ============================================================================
-- STUDENTS
-- ============================================================================
-- Lincoln High School Students (8 students - Grade 9)
INSERT INTO students ("Id", "UserId", "SchoolId", "GradeLevel", "EnrollmentDate", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('40000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000101', '10000000-0000-0000-0000-000000000001', 9, '2024-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000102', '10000000-0000-0000-0000-000000000001', 9, '2024-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000103', '10000000-0000-0000-0000-000000000001', 9, '2024-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000104', '10000000-0000-0000-0000-000000000001', 9, '2024-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000105', '10000000-0000-0000-0000-000000000001', 9, '2024-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000006', '20000000-0000-0000-0000-000000000106', '10000000-0000-0000-0000-000000000001', 9, '2024-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000007', '20000000-0000-0000-0000-000000000107', '10000000-0000-0000-0000-000000000001', 9, '2024-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000008', '20000000-0000-0000-0000-000000000108', '10000000-0000-0000-0000-000000000001', 9, '2024-09-01', true, NOW(), NOW());

-- Student User Accounts for Lincoln High School
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "IsActive", "CreatedAt", "UpdatedAt", "ExternalId") VALUES
('20000000-0000-0000-0000-000000000101', 'emma.wilson@lincoln.edu', 'Emma Wilson', 0, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-student-lincoln-001'),
('20000000-0000-0000-0000-000000000102', 'noah.brown@lincoln.edu', 'Noah Brown', 0, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-student-lincoln-002'),
('20000000-0000-0000-0000-000000000103', 'olivia.davis@lincoln.edu', 'Olivia Davis', 0, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-student-lincoln-003'),
('20000000-0000-0000-0000-000000000104', 'liam.miller@lincoln.edu', 'Liam Miller', 0, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-student-lincoln-004'),
('20000000-0000-0000-0000-000000000105', 'ava.garcia@lincoln.edu', 'Ava Garcia', 0, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-student-lincoln-005'),
('20000000-0000-0000-0000-000000000106', 'ethan.rodriguez@lincoln.edu', 'Ethan Rodriguez', 0, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-student-lincoln-006'),
('20000000-0000-0000-0000-000000000107', 'sophia.martinez@lincoln.edu', 'Sophia Martinez', 0, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-student-lincoln-007'),
('20000000-0000-0000-0000-000000000108', 'mason.lopez@lincoln.edu', 'Mason Lopez', 0, '10000000-0000-0000-0000-000000000001', true, NOW(), NOW(), 'ext-student-lincoln-008');

-- Washington Academy Students (8 students - Grade 10)
INSERT INTO students ("Id", "UserId", "SchoolId", "GradeLevel", "EnrollmentDate", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('40000000-0000-0000-0000-000000000009', '20000000-0000-0000-0000-000000000201', '10000000-0000-0000-0000-000000000002', 10, '2023-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000010', '20000000-0000-0000-0000-000000000202', '10000000-0000-0000-0000-000000000002', 10, '2023-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000011', '20000000-0000-0000-0000-000000000203', '10000000-0000-0000-0000-000000000002', 10, '2023-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000012', '20000000-0000-0000-0000-000000000204', '10000000-0000-0000-0000-000000000002', 10, '2023-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000013', '20000000-0000-0000-0000-000000000205', '10000000-0000-0000-0000-000000000002', 10, '2023-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000014', '20000000-0000-0000-0000-000000000206', '10000000-0000-0000-0000-000000000002', 10, '2023-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000015', '20000000-0000-0000-0000-000000000207', '10000000-0000-0000-0000-000000000002', 10, '2023-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000016', '20000000-0000-0000-0000-000000000208', '10000000-0000-0000-0000-000000000002', 10, '2023-09-01', true, NOW(), NOW());

-- Student User Accounts for Washington Academy
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "IsActive", "CreatedAt", "UpdatedAt", "ExternalId") VALUES
('20000000-0000-0000-0000-000000000201', 'isabella.hernandez@washington.edu', 'Isabella Hernandez', 0, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-student-washington-001'),
('20000000-0000-0000-0000-000000000202', 'james.gonzalez@washington.edu', 'James Gonzalez', 0, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-student-washington-002'),
('20000000-0000-0000-0000-000000000203', 'mia.wilson@washington.edu', 'Mia Wilson', 0, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-student-washington-003'),
('20000000-0000-0000-0000-000000000204', 'benjamin.anderson@washington.edu', 'Benjamin Anderson', 0, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-student-washington-004'),
('20000000-0000-0000-0000-000000000205', 'charlotte.thomas@washington.edu', 'Charlotte Thomas', 0, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-student-washington-005'),
('20000000-0000-0000-0000-000000000206', 'lucas.taylor@washington.edu', 'Lucas Taylor', 0, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-student-washington-006'),
('20000000-0000-0000-0000-000000000207', 'amelia.moore@washington.edu', 'Amelia Moore', 0, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-student-washington-007'),
('20000000-0000-0000-0000-000000000208', 'henry.jackson@washington.edu', 'Henry Jackson', 0, '10000000-0000-0000-0000-000000000002', true, NOW(), NOW(), 'ext-student-washington-008');

-- Roosevelt Institute Students (8 students - Grade 11)
INSERT INTO students ("Id", "UserId", "SchoolId", "GradeLevel", "EnrollmentDate", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('40000000-0000-0000-0000-000000000017', '20000000-0000-0000-0000-000000000301', '10000000-0000-0000-0000-000000000003', 11, '2022-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000018', '20000000-0000-0000-0000-000000000302', '10000000-0000-0000-0000-000000000003', 11, '2022-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000019', '20000000-0000-0000-0000-000000000303', '10000000-0000-0000-0000-000000000003', 11, '2022-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000020', '20000000-0000-0000-0000-000000000304', '10000000-0000-0000-0000-000000000003', 11, '2022-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000021', '20000000-0000-0000-0000-000000000305', '10000000-0000-0000-0000-000000000003', 11, '2022-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000022', '20000000-0000-0000-0000-000000000306', '10000000-0000-0000-0000-000000000003', 11, '2022-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000023', '20000000-0000-0000-0000-000000000307', '10000000-0000-0000-0000-000000000003', 11, '2022-09-01', true, NOW(), NOW()),
('40000000-0000-0000-0000-000000000024', '20000000-0000-0000-0000-000000000308', '10000000-0000-0000-0000-000000000003', 11, '2022-09-01', true, NOW(), NOW());

-- Student User Accounts for Roosevelt Institute
INSERT INTO users ("Id", "Email", "FullName", "Role", "SchoolId", "IsActive", "CreatedAt", "UpdatedAt", "ExternalId") VALUES
('20000000-0000-0000-0000-000000000301', 'harper.white@roosevelt.edu', 'Harper White', 0, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-student-roosevelt-001'),
('20000000-0000-0000-0000-000000000302', 'alexander.harris@roosevelt.edu', 'Alexander Harris', 0, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-student-roosevelt-002'),
('20000000-0000-0000-0000-000000000303', 'evelyn.martin@roosevelt.edu', 'Evelyn Martin', 0, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-student-roosevelt-003'),
('20000000-0000-0000-0000-000000000304', 'sebastian.thompson@roosevelt.edu', 'Sebastian Thompson', 0, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-student-roosevelt-004'),
('20000000-0000-0000-0000-000000000305', 'avery.garcia@roosevelt.edu', 'Avery Garcia', 0, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-student-roosevelt-005'),
('20000000-0000-0000-0000-000000000306', 'daniel.martinez@roosevelt.edu', 'Daniel Martinez', 0, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-student-roosevelt-006'),
('20000000-0000-0000-0000-000000000307', 'ella.robinson@roosevelt.edu', 'Ella Robinson', 0, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-student-roosevelt-007'),
('20000000-0000-0000-0000-000000000308', 'matthew.clark@roosevelt.edu', 'Matthew Clark', 0, '10000000-0000-0000-0000-000000000003', true, NOW(), NOW(), 'ext-student-roosevelt-008');

-- ============================================================================
-- ASSESSMENTS
-- ============================================================================
-- DifficultyLevel: VeryEasy=0, Easy=1, Medium=2, Hard=3, VeryHard=4
-- AssessmentType: Practice=0, Quiz=1, Test=2, Exam=3

-- Algebra I Assessments
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "AssessmentType", "DifficultyLevel", "EstimatedDurationMinutes", "IsAdaptive", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'Linear Equations Quiz', 'Basic linear equations practice', 1, 1, 25, false, true, NOW(), NOW()),
('60000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000001', 'Polynomials Test', 'Comprehensive polynomials test', 2, 2, 45, false, true, NOW(), NOW()),
('60000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000001', 'Functions Mid-term', 'Mid-term exam on functions', 3, 3, 60, false, true, NOW(), NOW());

-- Algebra II Assessments  
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "AssessmentType", "DifficultyLevel", "EstimatedDurationMinutes", "IsAdaptive", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000002', 'Quadratics Quiz', 'Solving quadratic equations', 1, 2, 30, false, true, NOW(), NOW()),
('60000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000002', 'Exponentials Test', 'Exponential and logarithmic functions', 2, 3, 50, false, true, NOW(), NOW());

-- Pre-Calculus Assessments
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "AssessmentType", "DifficultyLevel", "EstimatedDurationMinutes", "IsAdaptive", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000003', 'Trigonometry Quiz', 'Trigonometric identities and equations', 1, 3, 35, false, true, NOW(), NOW()),
('60000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000003', 'Limits Practice', 'Understanding limits and continuity', 0, 2, 30, false, true, NOW(), NOW());

-- Physics I Assessments
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "AssessmentType", "DifficultyLevel", "EstimatedDurationMinutes", "IsAdaptive", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000008', '30000000-0000-0000-0000-000000000004', 'Kinematics Quiz', 'Motion and velocity problems', 1, 2, 30, false, true, NOW(), NOW()),
('60000000-0000-0000-0000-000000000009', '30000000-0000-0000-0000-000000000004', 'Forces Test', 'Newton''s laws and applications', 2, 3, 45, false, true, NOW(), NOW()),
('60000000-0000-0000-0000-000000000010', '30000000-0000-0000-0000-000000000004', 'Energy & Momentum', 'Conservation laws', 2, 4, 50, false, true, NOW(), NOW());

-- Chemistry I Assessments
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "AssessmentType", "DifficultyLevel", "EstimatedDurationMinutes", "IsAdaptive", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000011', '30000000-0000-0000-0000-000000000005', 'Atomic Structure Quiz', 'Atoms, electrons, and periodic table', 1, 1, 25, false, true, NOW(), NOW()),
('60000000-0000-0000-0000-000000000012', '30000000-0000-0000-0000-000000000005', 'Chemical Reactions Test', 'Balancing equations and stoichiometry', 2, 2, 40, false, true, NOW(), NOW());

-- Biology I Assessment
INSERT INTO assessments ("Id", "CourseId", "Title", "Description", "AssessmentType", "DifficultyLevel", "EstimatedDurationMinutes", "IsAdaptive", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('60000000-0000-0000-0000-000000000013', '30000000-0000-0000-0000-000000000006', 'Cell Biology Quiz', 'Cell structure and function', 1, 1, 30, false, true, NOW(), NOW());

-- ============================================================================
-- UPDATE CLASS STUDENT IDS
-- ============================================================================
-- Now update the classes with student IDs
UPDATE classes SET "StudentIds" = '["40000000-0000-0000-0000-000000000001", "40000000-0000-0000-0000-000000000002", "40000000-0000-0000-0000-000000000003", "40000000-0000-0000-0000-000000000004", "40000000-0000-0000-0000-000000000005", "40000000-0000-0000-0000-000000000006", "40000000-0000-0000-0000-000000000007", "40000000-0000-0000-0000-000000000008"]'
WHERE "Id" = '50000000-0000-0000-0000-000000000001';

UPDATE classes SET "StudentIds" = '["40000000-0000-0000-0000-000000000001", "40000000-0000-0000-0000-000000000002", "40000000-0000-0000-0000-000000000003", "40000000-0000-0000-0000-000000000004", "40000000-0000-0000-0000-000000000005", "40000000-0000-0000-0000-000000000006", "40000000-0000-0000-0000-000000000007", "40000000-0000-0000-0000-000000000008"]'
WHERE "Id" = '50000000-0000-0000-0000-000000000002';

UPDATE classes SET "StudentIds" = '["40000000-0000-0000-0000-000000000009", "40000000-0000-0000-0000-000000000010", "40000000-0000-0000-0000-000000000011", "40000000-0000-0000-0000-000000000012", "40000000-0000-0000-0000-000000000013", "40000000-0000-0000-0000-000000000014", "40000000-0000-0000-0000-000000000015", "40000000-0000-0000-0000-000000000016"]'
WHERE "Id" = '50000000-0000-0000-0000-000000000003';

UPDATE classes SET "StudentIds" = '["40000000-0000-0000-0000-000000000009", "40000000-0000-0000-0000-000000000010", "40000000-0000-0000-0000-000000000011", "40000000-0000-0000-0000-000000000012", "40000000-0000-0000-0000-000000000013", "40000000-0000-0000-0000-000000000014", "40000000-0000-0000-0000-000000000015", "40000000-0000-0000-0000-000000000016"]'
WHERE "Id" = '50000000-0000-0000-0000-000000000004';

UPDATE classes SET "StudentIds" = '["40000000-0000-0000-0000-000000000017", "40000000-0000-0000-0000-000000000018", "40000000-0000-0000-0000-000000000019", "40000000-0000-0000-0000-000000000020", "40000000-0000-0000-0000-000000000021", "40000000-0000-0000-0000-000000000022", "40000000-0000-0000-0000-000000000023", "40000000-0000-0000-0000-000000000024"]'
WHERE "Id" = '50000000-0000-0000-0000-000000000005';

UPDATE classes SET "StudentIds" = '["40000000-0000-0000-0000-000000000017", "40000000-0000-0000-0000-000000000018", "40000000-0000-0000-0000-000000000019", "40000000-0000-0000-0000-000000000020", "40000000-0000-0000-0000-000000000021", "40000000-0000-0000-0000-000000000022", "40000000-0000-0000-0000-000000000023", "40000000-0000-0000-0000-000000000024"]'
WHERE "Id" = '50000000-0000-0000-0000-000000000006';

-- ============================================================================
-- DONE - Base data loaded
-- ============================================================================
-- Questions and StudentAssessments will be added via seed-demo-data.sh script
