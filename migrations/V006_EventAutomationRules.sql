-- =============================================
-- V006 — Event Automation Rules
-- Schema: sf
-- =============================================

SET search_path TO sf, public;

-- ── Table: event_automation_rules ─────────────────────────────
CREATE TABLE IF NOT EXISTS sf.event_automation_rules (
    "Id"                uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name"              varchar(200) NOT NULL,
    "Description"       text,
    "TriggerType"       varchar(50)  NOT NULL,
    "TriggerConditions" jsonb        NOT NULL DEFAULT '{}',
    "ActionType"        varchar(50)  NOT NULL,
    "ActionParams"      jsonb        NOT NULL DEFAULT '{}',
    "EntityType"        varchar(100),
    "IsEnabled"         boolean      NOT NULL DEFAULT true,
    "Priority"          integer      NOT NULL DEFAULT 0,
    "SortOrder"         integer      NOT NULL DEFAULT 0,
    "TenantId"          uuid         NOT NULL,
    "CreatedAt"         timestamptz  NOT NULL DEFAULT now(),
    "UpdatedAt"         timestamptz,
    "CreatedBy"         varchar(256),
    "ModifiedBy"        varchar(256),
    "IsDeleted"         boolean      NOT NULL DEFAULT false,
    "RowVersion"        xid          NOT NULL DEFAULT txid_current()::text::xid
);

-- ── Indexes ───────────────────────────────────────────────────
CREATE INDEX IF NOT EXISTS ix_event_automation_rules_trigger
    ON sf.event_automation_rules ("TriggerType");

CREATE INDEX IF NOT EXISTS ix_event_automation_rules_enabled
    ON sf.event_automation_rules ("IsEnabled");

CREATE INDEX IF NOT EXISTS ix_event_automation_rules_tenant
    ON sf.event_automation_rules ("TenantId");
