-- Add missing ProjectBase columns
ALTER TABLE pm.projects ADD COLUMN IF NOT EXISTS "Methodology" varchar(20) NOT NULL DEFAULT 'Kanban';
ALTER TABLE pm.projects ADD COLUMN IF NOT EXISTS "Category" varchar(30) NOT NULL DEFAULT 'General';
ALTER TABLE pm.projects ADD COLUMN IF NOT EXISTS "TargetEndDate" timestamptz NULL;
ALTER TABLE pm.projects ADD COLUMN IF NOT EXISTS "OwnerUserId" uuid NULL;
ALTER TABLE pm.projects ADD COLUMN IF NOT EXISTS "PortfolioId" uuid NULL;
