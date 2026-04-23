-- Child Ticket Pattern Migration
-- Ticket tablosuna parent-child ilişki desteği ekleme

-- 1. Eski görev entegrasyonu sütunlarını kaldır
ALTER TABLE req."tickets" DROP COLUMN IF EXISTS "LinkedTaskCount";
ALTER TABLE req."tickets" DROP COLUMN IF EXISTS "CompletedTaskCount";

-- 2. Yeni child ticket sütunlarını ekle
ALTER TABLE req."tickets" ADD COLUMN IF NOT EXISTS "ParentTicketId" uuid NULL;
ALTER TABLE req."tickets" ADD COLUMN IF NOT EXISTS "ChildTicketCount" integer NOT NULL DEFAULT 0;
ALTER TABLE req."tickets" ADD COLUMN IF NOT EXISTS "CompletedChildTicketCount" integer NOT NULL DEFAULT 0;

-- 3. Self-referencing FK
ALTER TABLE req."tickets"
    ADD CONSTRAINT "FK_tickets_tickets_ParentTicketId"
    FOREIGN KEY ("ParentTicketId") REFERENCES req."tickets"("Id")
    ON DELETE RESTRICT;

-- 4. Index
CREATE INDEX IF NOT EXISTS "IX_tickets_ParentTicketId"
    ON req."tickets" ("ParentTicketId");

-- 5. AllTasksDone → AllChildrenDone güncelleme (mevcut veride)
UPDATE req."tickets" SET "Status" = 'AllChildrenDone' WHERE "Status" = 'AllTasksDone';
UPDATE req."ticket_status_history" SET "FromStatus" = 'AllChildrenDone' WHERE "FromStatus" = 'AllTasksDone';
UPDATE req."ticket_status_history" SET "ToStatus" = 'AllChildrenDone' WHERE "ToStatus" = 'AllTasksDone';
