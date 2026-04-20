-- Faz B: Ticket tablosuna ConfigurationItemId kolonu
ALTER TABLE req.tickets ADD COLUMN IF NOT EXISTS "ConfigurationItemId" uuid NULL;
CREATE INDEX IF NOT EXISTS ix_tickets_ci ON req.tickets ("ConfigurationItemId") WHERE "ConfigurationItemId" IS NOT NULL;

-- Faz C: project_deliverables tablosu
CREATE TABLE IF NOT EXISTS pm.project_deliverables (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ProjectId" uuid NOT NULL REFERENCES pm.projects("Id"),
    "ConfigurationItemId" uuid NOT NULL REFERENCES pm.configuration_items("Id"),
    "Role" varchar(20) NOT NULL DEFAULT 'Primary',
    "Notes" varchar(500) NULL,
    "TenantId" uuid NOT NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_project_deliverables_unique ON pm.project_deliverables ("ProjectId", "ConfigurationItemId");
