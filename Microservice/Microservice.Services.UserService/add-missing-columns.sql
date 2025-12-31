-- Script to add missing columns to Users table
-- Run this script on PostgreSQL database: userservice_db

-- Add Role column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Users' AND column_name = 'Role'
    ) THEN
        ALTER TABLE "Users" ADD COLUMN "Role" VARCHAR(50) NOT NULL DEFAULT 'Customer';
    END IF;
END $$;

-- Add AvatarUrl column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Users' AND column_name = 'AvatarUrl'
    ) THEN
        ALTER TABLE "Users" ADD COLUMN "AvatarUrl" VARCHAR(500);
    END IF;
END $$;

-- Verify columns exist
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Users'
ORDER BY ordinal_position;

