-- Risk Management modülü tabloları (pm schema'sı)
-- Bağımlılık: pm.projects tablosu

-- ═══════════════════════════════════════════════════════════
-- 1. Risks tablosu
-- ═══════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS pm.risks (
    "Id"              uuid        NOT NULL,
    "ProjectId"       uuid        NOT NULL,
    "Title"           varchar(500) NOT NULL,
    "Description"     text,
    "Category"        varchar(30) NOT NULL DEFAULT 'Technical',
    "Status"          varchar(20) NOT NULL DEFAULT 'Open',
    "Probability"     integer     NOT NULL DEFAULT 1,
    "Impact"          integer     NOT NULL DEFAULT 1,
    "RiskScore"       integer     NOT NULL DEFAULT 1,
    "MitigationPlan"  text,
    "OwnerUserId"     uuid,
    "TenantId"        uuid        NOT NULL,
    "CreatedAt"       timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"       timestamptz,
    "CreatedBy"       uuid,
    "UpdatedBy"       uuid,
    "RowVersion"      bytea,
    CONSTRAINT pk_risks PRIMARY KEY ("Id"),
    CONSTRAINT fk_risks_project FOREIGN KEY ("ProjectId")
        REFERENCES pm.projects("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_risks_project ON pm.risks ("ProjectId");
CREATE INDEX IF NOT EXISTS ix_risks_status  ON pm.risks ("Status");

-- ═══════════════════════════════════════════════════════════
-- 2. MitigationActions tablosu
-- ═══════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS pm.mitigation_actions (
    "Id"              uuid        NOT NULL,
    "RiskId"          uuid        NOT NULL,
    "Title"           varchar(500) NOT NULL,
    "Description"     text,
    "Status"          varchar(20) NOT NULL DEFAULT 'Planned',
    "AssigneeUserId"  uuid,
    "DueDate"         timestamptz,
    "CompletedAt"     timestamptz,
    "TenantId"        uuid        NOT NULL,
    "CreatedAt"       timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"       timestamptz,
    "CreatedBy"       uuid,
    "UpdatedBy"       uuid,
    "RowVersion"      bytea,
    CONSTRAINT pk_mitigation_actions PRIMARY KEY ("Id"),
    CONSTRAINT fk_mitigation_actions_risk FOREIGN KEY ("RiskId")
        REFERENCES pm.risks("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_mitigation_actions_risk ON pm.mitigation_actions ("RiskId");
