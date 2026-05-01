-- Automation Rules (StateFlow Actions) — sf schema'sı
-- Bağımlılık: sf.state_flow_definitions, sf.transition_definitions

-- ═══════════════════════════════════════════════════════════
-- 1. OnTransitionActions kolonu (transition_definitions tablosuna)
-- ═══════════════════════════════════════════════════════════
ALTER TABLE sf.transition_definitions
    ADD COLUMN IF NOT EXISTS "OnTransitionActions" jsonb;

-- ═══════════════════════════════════════════════════════════
-- 2. rule_execution_logs tablosu
-- ═══════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS sf.rule_execution_logs (
    "Id"                uuid         NOT NULL,
    "FlowDefinitionId"  uuid         NOT NULL,
    "EntityType"        varchar(100) NOT NULL,
    "TargetEntityId"    uuid         NOT NULL,
    "Source"            varchar(30)  NOT NULL,     -- 'OnEntry' | 'OnTransition'
    "StateName"         varchar(100) NOT NULL,
    "TriggerName"       varchar(100),
    "ActionType"        varchar(50)  NOT NULL,     -- 'SendNotification', 'ChangeStatus' vb.
    "ActionParamsJson"  jsonb        NOT NULL DEFAULT '{}',
    "Success"           boolean      NOT NULL DEFAULT true,
    "ErrorMessage"      text,
    "DurationMs"        integer      NOT NULL DEFAULT 0,
    "TenantId"          uuid         NOT NULL,
    "IsDeleted"         boolean      NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz  NOT NULL DEFAULT now(),
    "UpdatedAt"         timestamptz,
    "CreatedBy"         varchar(200),
    "ModifiedBy"        varchar(200),
    "RowVersion"        bytea,
    CONSTRAINT pk_rule_execution_logs PRIMARY KEY ("Id"),
    CONSTRAINT fk_rule_execution_logs_flow FOREIGN KEY ("FlowDefinitionId")
        REFERENCES sf.state_flow_definitions("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_rule_execution_logs_flow
    ON sf.rule_execution_logs ("FlowDefinitionId");

CREATE INDEX IF NOT EXISTS ix_rule_execution_logs_entity
    ON sf.rule_execution_logs ("TargetEntityId");

CREATE INDEX IF NOT EXISTS ix_rule_execution_logs_created
    ON sf.rule_execution_logs ("CreatedAt" DESC);

CREATE INDEX IF NOT EXISTS ix_rule_execution_logs_tenant
    ON sf.rule_execution_logs ("TenantId");

-- ═══════════════════════════════════════════════════════════
-- 3. 90 gün saklama politikası (opsiyonel — cron job ile temizlenir)
-- ═══════════════════════════════════════════════════════════
-- Bu sorgu bir cron job veya Hangfire background job ile günlük çalıştırılabilir:
-- DELETE FROM sf.rule_execution_logs WHERE "CreatedAt" < NOW() - INTERVAL '90 days';
