-- EduMind.AI Database Initialization Script
-- This script sets up the initial database structure for development

-- Create schemas
CREATE SCHEMA IF NOT EXISTS academic;
CREATE SCHEMA IF NOT EXISTS analytics;
CREATE SCHEMA IF NOT EXISTS agents;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- For text search
CREATE EXTENSION IF NOT EXISTS "btree_gin"; -- For performance

-- Grant permissions
GRANT ALL PRIVILEGES ON SCHEMA academic TO edumind_user;
GRANT ALL PRIVILEGES ON SCHEMA analytics TO edumind_user;
GRANT ALL PRIVILEGES ON SCHEMA agents TO edumind_user;

-- Create basic audit trigger function
CREATE OR REPLACE FUNCTION academic.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Informational messages
DO $$
BEGIN
    RAISE NOTICE 'EduMind.AI Database initialized successfully!';
    RAISE NOTICE 'Schemas created: academic, analytics, agents';
    RAISE NOTICE 'Extensions enabled: uuid-ossp, pg_trgm, btree_gin';
END $$;
