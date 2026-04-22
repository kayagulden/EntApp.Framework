-- Ticket — FlowDefinitionId alanı ekleme
-- Tarih: 2026-04-22

ALTER TABLE req.tickets ADD COLUMN IF NOT EXISTS "FlowDefinitionId" uuid NULL;

CREATE INDEX IF NOT EXISTS ix_tickets_flow_definition
    ON req.tickets ("FlowDefinitionId")
    WHERE "FlowDefinitionId" IS NOT NULL;

-- Mevcut ticket'lara seed flow'u ata (varsa)
UPDATE req.tickets
SET "FlowDefinitionId" = '00000000-0000-0000-0000-000000000001'
WHERE "FlowDefinitionId" IS NULL
  AND EXISTS (SELECT 1 FROM sf.state_flow_definitions WHERE "Id" = '00000000-0000-0000-0000-000000000001');
