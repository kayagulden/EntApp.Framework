-- =============================================================
-- Unified Work Item Model — DB Migration
-- Tarih: 2026-04-21
-- Hedef: pm schema (TaskManagement modülü)
-- =============================================================

-- 0. Schema oluştur (yoksa)
CREATE SCHEMA IF NOT EXISTS pm;

-- 1. Tablo rename: tasks → work_items
ALTER TABLE IF EXISTS pm.tasks RENAME TO work_items;

-- 2. Yeni kolonlar (backward compatible — tümü nullable veya default'lu)
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "StoryPoints" integer NULL;
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "AcceptanceCriteria" text NULL;
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "SprintId" uuid NULL;
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "HierarchyLevel" integer NOT NULL DEFAULT 0;

-- 3. Kolon rename: TaskNumber → WorkItemNumber
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'pm' AND table_name = 'work_items' AND column_name = 'TaskNumber'
    ) THEN
        ALTER TABLE pm.work_items RENAME COLUMN "TaskNumber" TO "WorkItemNumber";
    END IF;
END $$;

-- 4. Type kolonu genişletme (yeni enum değerleri: UserStory, TechDebt, Spike)
ALTER TABLE pm.work_items ALTER COLUMN "Type" TYPE varchar(30);

-- 5. ProjectBase: TaskSequence → WorkItemSequence
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'pm' AND table_name = 'projects' AND column_name = 'TaskSequence'
    ) THEN
        ALTER TABLE pm.projects RENAME COLUMN "TaskSequence" TO "WorkItemSequence";
    END IF;
END $$;

-- 6. Sprint tablosu
CREATE TABLE IF NOT EXISTS pm.sprints (
    "Id" uuid PRIMARY KEY,
    "ProjectId" uuid NOT NULL REFERENCES pm.projects("Id"),
    "Name" varchar(200) NOT NULL,
    "Goal" varchar(2000),
    "Status" varchar(20) NOT NULL DEFAULT 'Planning',
    "StartDate" timestamp NOT NULL,
    "EndDate" timestamp NOT NULL,
    "CapacityPoints" integer,
    "TenantId" uuid NOT NULL,
    "CreatedAt" timestamp NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp,
    "CreatedBy" varchar(256),
    "ModifiedBy" varchar(256),
    "IsDeleted" boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_sprints_project ON pm.sprints("ProjectId");
CREATE INDEX IF NOT EXISTS ix_sprints_status ON pm.sprints("Status");

-- 7. SprintId FK + index (work_items → sprints)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_work_items_sprint'
    ) THEN
        ALTER TABLE pm.work_items ADD CONSTRAINT fk_work_items_sprint 
            FOREIGN KEY ("SprintId") REFERENCES pm.sprints("Id");
    END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_work_items_sprint ON pm.work_items("SprintId");

-- Doğrulama
DO $$
BEGIN
    RAISE NOTICE '✅ Migration tamamlandı!';
    RAISE NOTICE '  - pm.work_items tablosu güncellendi';
    RAISE NOTICE '  - pm.sprints tablosu oluşturuldu';
    RAISE NOTICE '  - Yeni kolonlar: StoryPoints, AcceptanceCriteria, SprintId, HierarchyLevel';
END $$;
