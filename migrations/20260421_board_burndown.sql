-- ============================================================
-- Migration: Faz 5-6 — BoardColumn, BurndownSnapshot, Sprint Metrics
-- Tarih: 2026-04-21
-- ============================================================

CREATE SCHEMA IF NOT EXISTS pm;

-- ── BoardColumn tablosu ─────────────────────────────────────
CREATE TABLE IF NOT EXISTS pm.board_columns (
    "Id" uuid PRIMARY KEY,
    "ProjectId" uuid NOT NULL REFERENCES pm.projects("Id"),
    "Name" varchar(100) NOT NULL,
    "Order" integer NOT NULL DEFAULT 0,
    "WipLimit" integer NULL,
    "MappedStatus" varchar(20) NOT NULL,
    "TenantId" uuid NOT NULL,
    "CreatedAt" timestamp NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp,
    "CreatedBy" varchar(256),
    "ModifiedBy" varchar(256),
    "IsDeleted" boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_board_columns_project ON pm.board_columns("ProjectId");

-- ── Sprint metrik alanları ──────────────────────────────────
ALTER TABLE pm.sprints ADD COLUMN IF NOT EXISTS "PlannedPoints" integer NOT NULL DEFAULT 0;
ALTER TABLE pm.sprints ADD COLUMN IF NOT EXISTS "CompletedPoints" integer NOT NULL DEFAULT 0;

-- ── BurndownSnapshot tablosu ────────────────────────────────
CREATE TABLE IF NOT EXISTS pm.burndown_snapshots (
    "Id" uuid PRIMARY KEY,
    "SprintId" uuid NOT NULL REFERENCES pm.sprints("Id"),
    "Date" date NOT NULL,
    "RemainingPoints" integer NOT NULL DEFAULT 0,
    "CompletedPoints" integer NOT NULL DEFAULT 0,
    "TotalItems" integer NOT NULL DEFAULT 0,
    "CompletedItems" integer NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS ix_burndown_sprint ON pm.burndown_snapshots("SprintId");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'ix_burndown_sprint_date') THEN
        CREATE UNIQUE INDEX ix_burndown_sprint_date ON pm.burndown_snapshots("SprintId", "Date");
    END IF;
END $$;

-- ── Mevcut projeler için varsayılan board kolonları ─────────
DO $$
DECLARE
    proj RECORD;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pm.board_columns LIMIT 1) THEN
        FOR proj IN SELECT "Id", "TenantId" FROM pm.projects WHERE "IsDeleted" = false LOOP
            INSERT INTO pm.board_columns ("Id", "ProjectId", "Name", "Order", "MappedStatus", "TenantId", "CreatedAt", "IsDeleted")
            VALUES
                (gen_random_uuid(), proj."Id", 'Bekleyenler', 0, 'Backlog', proj."TenantId", now(), false),
                (gen_random_uuid(), proj."Id", 'Yapılacak', 1, 'Todo', proj."TenantId", now(), false),
                (gen_random_uuid(), proj."Id", 'İşlemde', 2, 'InProgress', proj."TenantId", now(), false),
                (gen_random_uuid(), proj."Id", 'İnceleme', 3, 'InReview', proj."TenantId", now(), false),
                (gen_random_uuid(), proj."Id", 'Tamamlandı', 4, 'Done', proj."TenantId", now(), false),
                (gen_random_uuid(), proj."Id", 'İptal', 5, 'Cancelled', proj."TenantId", now(), false);
        END LOOP;
    END IF;
END $$;

DO $$ BEGIN RAISE NOTICE 'Faz 5-6 Migration tamamlandi'; END $$;
