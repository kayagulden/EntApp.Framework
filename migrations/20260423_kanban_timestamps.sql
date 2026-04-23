-- Migration: Kanban metrics timestamps
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "StartedAt" timestamp with time zone;
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "CompletedAt" timestamp with time zone;

-- Mevcut Done/Cancelled items için CompletedAt = UpdatedAt olarak ayarla
UPDATE pm.work_items SET "CompletedAt" = "UpdatedAt"
    WHERE "CompletedAt" IS NULL AND "Status" IN (4, 5);

-- Mevcut InProgress items için StartedAt = UpdatedAt olarak ayarla
UPDATE pm.work_items SET "StartedAt" = "UpdatedAt"
    WHERE "StartedAt" IS NULL AND "Status" = 1;
