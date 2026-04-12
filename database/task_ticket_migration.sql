-- TaskManagement: Source alanları ve nullable ProjectId
ALTER TABLE pm.tasks ADD COLUMN IF NOT EXISTS "SourceModule" varchar(50);
ALTER TABLE pm.tasks ADD COLUMN IF NOT EXISTS "SourceType" varchar(50);
ALTER TABLE pm.tasks ADD COLUMN IF NOT EXISTS "SourceId" uuid;
ALTER TABLE pm.tasks ALTER COLUMN "ProjectId" DROP NOT NULL;
CREATE INDEX IF NOT EXISTS ix_tasks_source ON pm.tasks ("SourceModule", "SourceType", "SourceId") WHERE "SourceId" IS NOT NULL;

-- RequestManagement: Ticket görev sayaçları
ALTER TABLE req.tickets ADD COLUMN IF NOT EXISTS "LinkedTaskCount" int NOT NULL DEFAULT 0;
ALTER TABLE req.tickets ADD COLUMN IF NOT EXISTS "CompletedTaskCount" int NOT NULL DEFAULT 0;
