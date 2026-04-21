ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "MilestoneId" uuid REFERENCES pm.milestones("Id");
CREATE INDEX IF NOT EXISTS ix_tasks_milestone ON pm.work_items ("MilestoneId");
ALTER TABLE pm.sprints ADD COLUMN IF NOT EXISTS "MilestoneId" uuid REFERENCES pm.milestones("Id");
CREATE INDEX IF NOT EXISTS ix_sprints_milestone ON pm.sprints ("MilestoneId");
