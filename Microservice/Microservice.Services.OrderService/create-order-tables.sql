-- Script to create OrderItems and OrderStatusHistory tables
-- Run this script on PostgreSQL database: orderservice_db

-- Add missing columns to Orders table
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Orders' AND column_name = 'PaymentMethod'
    ) THEN
        ALTER TABLE "Orders" ADD COLUMN "PaymentMethod" VARCHAR(50);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Orders' AND column_name = 'PaymentStatus'
    ) THEN
        ALTER TABLE "Orders" ADD COLUMN "PaymentStatus" VARCHAR(50);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Orders' AND column_name = 'PaymentTransactionId'
    ) THEN
        ALTER TABLE "Orders" ADD COLUMN "PaymentTransactionId" VARCHAR(200);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Orders' AND column_name = 'PaymentDate'
    ) THEN
        ALTER TABLE "Orders" ADD COLUMN "PaymentDate" TIMESTAMP;
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Orders' AND column_name = 'ShippingCarrier'
    ) THEN
        ALTER TABLE "Orders" ADD COLUMN "ShippingCarrier" VARCHAR(100);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Orders' AND column_name = 'TrackingNumber'
    ) THEN
        ALTER TABLE "Orders" ADD COLUMN "TrackingNumber" VARCHAR(100);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Orders' AND column_name = 'ShippedDate'
    ) THEN
        ALTER TABLE "Orders" ADD COLUMN "ShippedDate" TIMESTAMP;
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Orders' AND column_name = 'DeliveredDate'
    ) THEN
        ALTER TABLE "Orders" ADD COLUMN "DeliveredDate" TIMESTAMP;
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Orders' AND column_name = 'Notes'
    ) THEN
        ALTER TABLE "Orders" ADD COLUMN "Notes" VARCHAR(1000);
    END IF;
END $$;

-- Create OrderItems table
CREATE TABLE IF NOT EXISTS "OrderItems" (
    "Id" SERIAL PRIMARY KEY,
    "OrderId" INTEGER NOT NULL,
    "ProductId" INTEGER NOT NULL,
    "ProductName" VARCHAR(200) NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "UnitPrice" NUMERIC(18,2) NOT NULL,
    "SubTotal" NUMERIC(18,2) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_OrderItems_Orders_OrderId" 
        FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");

-- Create OrderStatusHistory table
CREATE TABLE IF NOT EXISTS "OrderStatusHistory" (
    "Id" SERIAL PRIMARY KEY,
    "OrderId" INTEGER NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "Notes" VARCHAR(500),
    "ChangedBy" VARCHAR(100),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_OrderStatusHistory_Orders_OrderId" 
        FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_OrderStatusHistory_OrderId" ON "OrderStatusHistory" ("OrderId");

