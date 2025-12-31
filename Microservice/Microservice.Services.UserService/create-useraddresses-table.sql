-- Script to create UserAddresses table if it doesn't exist
-- Run this script on PostgreSQL database: userservice_db

CREATE TABLE IF NOT EXISTS "UserAddresses" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "FullName" VARCHAR(100) NOT NULL,
    "PhoneNumber" VARCHAR(20) NOT NULL,
    "Street" VARCHAR(200) NOT NULL,
    "City" VARCHAR(100) NOT NULL,
    "State" VARCHAR(100) NOT NULL,
    "PostalCode" VARCHAR(20) NOT NULL,
    "Country" VARCHAR(100) NOT NULL,
    "IsDefault" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_UserAddresses_Users_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_UserAddresses_UserId" ON "UserAddresses" ("UserId");

