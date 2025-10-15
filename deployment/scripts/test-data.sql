-- Test data seeding script for integration tests
-- This script creates minimal test data for faster test execution

-- Create test courses
INSERT INTO courses (id, title, description, subject, board_name, module_name, metadata, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'Test Mathematics Course', 'Math course for integration tests', 0, 'CBSE', 'Algebra', '{"difficulty": "intermediate"}', NOW(), NOW()),
    (gen_random_uuid(), 'Test Physics Course', 'Physics course for integration tests', 1, 'ICSE', 'Mechanics', '{"difficulty": "advanced"}', NOW(), NOW()),
    (gen_random_uuid(), 'Test Chemistry Course', 'Chemistry course for integration tests', 2, 'CBSE', 'Organic', '{"difficulty": "basic"}', NOW(), NOW());

-- Create test students
INSERT INTO students (id, first_name, last_name, email, grade, created_at, updated_at)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'Test', 'Student1', 'test1@example.com', 10, NOW(), NOW()),
    ('22222222-2222-2222-2222-222222222222', 'Test', 'Student2', 'test2@example.com', 11, NOW(), NOW()),
    ('33333333-3333-3333-3333-333333333333', 'Test', 'Student3', 'test3@example.com', 12, NOW(), NOW());

-- Create test teachers
INSERT INTO teachers (id, first_name, last_name, email, subject_specialization, created_at, updated_at)
VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Test', 'Teacher1', 'teacher1@example.com', 'Mathematics', NOW(), NOW()),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Test', 'Teacher2', 'teacher2@example.com', 'Physics', NOW(), NOW());

-- Note: Additional test data can be created in test setup methods
-- This provides only the minimal baseline data
