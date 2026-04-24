-- Release Management MVP — Migration (Fixed)
-- Schema: pm
-- Tarih: 2026-04-24

-- 1. releases tablosu
CREATE TABLE IF NOT EXISTS pm.releases (
    "Id"                uuid PRIMARY KEY,
    "ProjectId"         uuid NOT NULL REFERENCES pm.projects("Id") ON DELETE CASCADE,
    "Key"               varchar(30) NOT NULL,
    "Version"           varchar(50) NOT NULL,
    "Title"             varchar(500) NOT NULL,
    "Description"       text,
    "Status"            varchar(20) NOT NULL DEFAULT 'Planning',
    "Type"              varchar(20) NOT NULL DEFAULT 'Minor',
    "SprintId"          uuid,
    "MilestoneId"       uuid,
    "PlannedDate"       date,
    "ActualDate"        date,
    "CodeFreezeDate"    date,
    "ReleaseManagerId"  varchar(100),
    "TargetEnvironment" varchar(100),
    "Tags"              varchar(500),
    "SortOrder"         integer NOT NULL DEFAULT 0,
    "TenantId"          uuid NOT NULL,
    "CreatedAt"         timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"         text,
    "UpdatedAt"         timestamp with time zone,
    "ModifiedBy"        text,
    "IsDeleted"         boolean NOT NULL DEFAULT false,
    "RowVersion"        bigint NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_releases_project_key ON pm.releases ("ProjectId", "Key");
CREATE INDEX IF NOT EXISTS ix_releases_status ON pm.releases ("Status");

-- 2. release_items tablosu
CREATE TABLE IF NOT EXISTS pm.release_items (
    "Id"            uuid PRIMARY KEY,
    "ReleaseId"     uuid NOT NULL REFERENCES pm.releases("Id") ON DELETE CASCADE,
    "WorkItemId"    uuid NOT NULL REFERENCES pm.work_items("Id") ON DELETE CASCADE,
    "IncludedAt"    timestamp with time zone NOT NULL DEFAULT now(),
    "IncludedBy"    varchar(100) NOT NULL,
    "Notes"         varchar(500),
    "SortOrder"     integer NOT NULL DEFAULT 0,
    "CreatedAt"     timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"     text,
    "UpdatedAt"     timestamp with time zone,
    "ModifiedBy"    text,
    "IsDeleted"     boolean NOT NULL DEFAULT false,
    "RowVersion"    bigint NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_release_items_unique ON pm.release_items ("ReleaseId", "WorkItemId");

-- 3. go_no_go_checklists tablosu
CREATE TABLE IF NOT EXISTS pm.go_no_go_checklists (
    "Id"            uuid PRIMARY KEY,
    "ReleaseId"     uuid NOT NULL REFERENCES pm.releases("Id") ON DELETE CASCADE,
    "Status"        varchar(20) NOT NULL DEFAULT 'Pending',
    "DecisionAt"    timestamp with time zone,
    "DecisionBy"    varchar(100),
    "DecisionNotes" text,
    "TenantId"      uuid NOT NULL,
    "CreatedAt"     timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"     text,
    "UpdatedAt"     timestamp with time zone,
    "ModifiedBy"    text,
    "IsDeleted"     boolean NOT NULL DEFAULT false,
    "RowVersion"    bigint NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_go_no_go_checklists_release ON pm.go_no_go_checklists ("ReleaseId");

-- 4. go_no_go_items tablosu
CREATE TABLE IF NOT EXISTS pm.go_no_go_items (
    "Id"            uuid PRIMARY KEY,
    "ChecklistId"   uuid NOT NULL REFERENCES pm.go_no_go_checklists("Id") ON DELETE CASCADE,
    "Category"      varchar(20) NOT NULL DEFAULT 'Development',
    "Title"         varchar(500) NOT NULL,
    "Description"   text,
    "Status"        varchar(20) NOT NULL DEFAULT 'Pending',
    "ReviewedBy"    varchar(100),
    "ReviewedAt"    timestamp with time zone,
    "Notes"         text,
    "SortOrder"     integer NOT NULL DEFAULT 0,
    "IsRequired"    boolean NOT NULL DEFAULT true,
    "CreatedAt"     timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt"     timestamp with time zone,
    "IsDeleted"     boolean NOT NULL DEFAULT false,
    "RowVersion"    bigint NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_go_no_go_items_checklist ON pm.go_no_go_items ("ChecklistId");

-- 5. release_notes tablosu
CREATE TABLE IF NOT EXISTS pm.release_notes (
    "Id"                uuid PRIMARY KEY,
    "ReleaseId"         uuid NOT NULL REFERENCES pm.releases("Id") ON DELETE CASCADE,
    "Content"           text NOT NULL,
    "GeneratedAt"       timestamp with time zone NOT NULL DEFAULT now(),
    "IsManuallyEdited"  boolean NOT NULL DEFAULT false,
    "PublishedAt"       timestamp with time zone,
    "TenantId"          uuid NOT NULL,
    "CreatedAt"         timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"         text,
    "UpdatedAt"         timestamp with time zone,
    "ModifiedBy"        text,
    "IsDeleted"         boolean NOT NULL DEFAULT false,
    "RowVersion"        bigint NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_release_notes_release ON pm.release_notes ("ReleaseId");

-- 6. projects tablosuna ReleaseSequence kolonu (idempotent)
ALTER TABLE pm.projects ADD COLUMN IF NOT EXISTS "ReleaseSequence" integer NOT NULL DEFAULT 0;
