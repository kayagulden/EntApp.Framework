-- Migration: WSJF fields + EstimationMode
-- WorkItemBase: WSJF bileşenleri
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "BusinessValue" integer;
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "TimeCriticality" integer;
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "RiskReduction" integer;
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "WsjfScore" numeric;

-- WSJF skoru index (sıralama için)
CREATE INDEX IF NOT EXISTS ix_work_items_wsjf ON pm.work_items ("WsjfScore" DESC NULLS LAST)
    WHERE "WsjfScore" IS NOT NULL;

-- ProjectBase: tahmin gösterim modu
ALTER TABLE pm.projects ADD COLUMN IF NOT EXISTS "EstimationMode" integer DEFAULT 0;
