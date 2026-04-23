-- board_columns tablosundaki eksik kolonları ekle
ALTER TABLE pm.board_columns ADD COLUMN IF NOT EXISTS "RowVersion" bigint NOT NULL DEFAULT 0;
ALTER TABLE pm.board_columns ADD COLUMN IF NOT EXISTS "CreatedBy" text;
ALTER TABLE pm.board_columns ADD COLUMN IF NOT EXISTS "ModifiedBy" text;
ALTER TABLE pm.board_columns ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;
