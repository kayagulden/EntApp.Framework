-- State Flow Engine — Initial Schema
-- Schema: sf (state_flow)
-- Tarih: 2026-04-22

CREATE SCHEMA IF NOT EXISTS sf;

-- ═══════════════════════════════════════════════════════════════
--  state_flow_definitions — Akış tanımları
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS sf.state_flow_definitions (
    "Id"                uuid            NOT NULL PRIMARY KEY,
    "EntityType"        varchar(100)    NOT NULL,
    "Key"               varchar(200)    NOT NULL,
    "Name"              varchar(300)    NOT NULL,
    "Description"       varchar(1000),
    "Version"           integer         NOT NULL DEFAULT 1,
    "Status"            varchar(20)     NOT NULL DEFAULT 'Draft',
    "PublishedAt"       timestamptz,
    "IsGlobalTemplate"  boolean         NOT NULL DEFAULT false,
    "SourceTemplateId"  uuid,
    "TenantId"          uuid            NOT NULL,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "UpdatedAt"         timestamptz,
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedBy"         text,
    "ModifiedBy"        text,
    "RowVersion"        xid             NOT NULL DEFAULT '0'::xid
);

-- EntityType + Key + Version benzersiz
CREATE UNIQUE INDEX IF NOT EXISTS ix_flow_definitions_entity_key_version
    ON sf.state_flow_definitions ("EntityType", "Key", "Version")
    WHERE "IsDeleted" = false;

-- Her EntityType için en fazla 1 Published
CREATE UNIQUE INDEX IF NOT EXISTS ix_flow_definitions_entity_type_published
    ON sf.state_flow_definitions ("EntityType", "Status")
    WHERE "Status" = 'Published' AND "IsDeleted" = false;

-- TenantId lookup
CREATE INDEX IF NOT EXISTS ix_flow_definitions_tenant_id
    ON sf.state_flow_definitions ("TenantId");

-- Global template lookup
CREATE INDEX IF NOT EXISTS ix_flow_definitions_global_template
    ON sf.state_flow_definitions ("IsGlobalTemplate")
    WHERE "IsGlobalTemplate" = true AND "IsDeleted" = false;


-- ═══════════════════════════════════════════════════════════════
--  state_definitions — Durum tanımları
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS sf.state_definitions (
    "Id"                uuid            NOT NULL PRIMARY KEY,
    "FlowDefinitionId"  uuid            NOT NULL REFERENCES sf.state_flow_definitions("Id") ON DELETE CASCADE,
    "Name"              varchar(100)    NOT NULL,
    "Label"             varchar(200)    NOT NULL,
    "Color"             varchar(20)     DEFAULT '#6b7280',
    "Icon"              varchar(50),
    "IsInitial"         boolean         NOT NULL DEFAULT false,
    "IsTerminal"        boolean         NOT NULL DEFAULT false,
    "IsPaused"          boolean         NOT NULL DEFAULT false,
    "Category"          varchar(50)     DEFAULT 'Active',
    "PositionX"         double precision NOT NULL DEFAULT 0,
    "PositionY"         double precision NOT NULL DEFAULT 0,
    "SortOrder"         integer         NOT NULL DEFAULT 0,
    "OnEntryActions"    jsonb,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "UpdatedAt"         timestamptz,
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedBy"         text,
    "ModifiedBy"        text,
    "RowVersion"        xid             NOT NULL DEFAULT '0'::xid
);

-- Aynı flow içinde aynı isimde state olamaz
CREATE UNIQUE INDEX IF NOT EXISTS ix_state_definitions_flow_name
    ON sf.state_definitions ("FlowDefinitionId", "Name")
    WHERE "IsDeleted" = false;


-- ═══════════════════════════════════════════════════════════════
--  transition_definitions — Geçiş tanımları
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS sf.transition_definitions (
    "Id"                uuid            NOT NULL PRIMARY KEY,
    "FlowDefinitionId"  uuid            NOT NULL REFERENCES sf.state_flow_definitions("Id") ON DELETE CASCADE,
    "FromStateName"     varchar(100)    NOT NULL,
    "ToStateName"       varchar(100)    NOT NULL,
    "TriggerName"       varchar(100)    NOT NULL,
    "Label"             varchar(200)    NOT NULL,
    "RequiredRole"      varchar(100),
    "GuardExpression"   varchar(500),
    "SortOrder"         integer         NOT NULL DEFAULT 0,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "UpdatedAt"         timestamptz,
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedBy"         text,
    "ModifiedBy"        text,
    "RowVersion"        xid             NOT NULL DEFAULT '0'::xid
);

-- Flow + From + Trigger benzersiz (aynı state'den aynı trigger ile iki farklı geçiş olamaz)
CREATE UNIQUE INDEX IF NOT EXISTS ix_transition_definitions_flow_from_trigger
    ON sf.transition_definitions ("FlowDefinitionId", "FromStateName", "TriggerName")
    WHERE "IsDeleted" = false;
