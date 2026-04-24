-- Test Management MVP Migration
-- pm schema altına 6 tablo + pm.projects'e 2 sequence kolonu

-- 1. pm.projects tablosuna sequence kolonları
ALTER TABLE pm.projects ADD COLUMN IF NOT EXISTS "TestScenarioSequence" integer NOT NULL DEFAULT 0;
ALTER TABLE pm.projects ADD COLUMN IF NOT EXISTS "TestPlanSequence" integer NOT NULL DEFAULT 0;

-- 2. pm.test_scenarios
CREATE TABLE IF NOT EXISTS pm.test_scenarios (
    "Id" uuid NOT NULL,
    "ProjectId" uuid NOT NULL,
    "Key" character varying(30) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Description" text,
    "Preconditions" text,
    "Type" character varying(20) NOT NULL DEFAULT 'Functional',
    "Priority" character varying(20) NOT NULL DEFAULT 'Medium',
    "Status" character varying(20) NOT NULL DEFAULT 'Draft',
    "RequirementId" uuid,
    "EstimatedDuration" interval,
    "Tags" character varying(500),
    "SortOrder" integer NOT NULL DEFAULT 0,
    "TenantId" uuid NOT NULL,
    "RowVersion" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone,
    "ModifiedBy" character varying(100),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "PK_test_scenarios" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_test_scenarios_projects" FOREIGN KEY ("ProjectId") REFERENCES pm.projects("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_test_scenarios_requirements" FOREIGN KEY ("RequirementId") REFERENCES pm.requirements("Id") ON DELETE SET NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_test_scenarios_project_key ON pm.test_scenarios ("ProjectId", "Key");
CREATE INDEX IF NOT EXISTS ix_test_scenarios_requirement ON pm.test_scenarios ("RequirementId");

-- 3. pm.test_steps
CREATE TABLE IF NOT EXISTS pm.test_steps (
    "Id" uuid NOT NULL,
    "TestScenarioId" uuid NOT NULL,
    "StepNumber" integer NOT NULL,
    "Action" character varying(1000) NOT NULL,
    "ExpectedResult" character varying(1000) NOT NULL,
    "TestData" character varying(2000),
    "Notes" character varying(1000),
    "RowVersion" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone,
    "ModifiedBy" character varying(100),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "PK_test_steps" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_test_steps_scenarios" FOREIGN KEY ("TestScenarioId") REFERENCES pm.test_scenarios("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_test_steps_scenario_number ON pm.test_steps ("TestScenarioId", "StepNumber");

-- 4. pm.test_plans
CREATE TABLE IF NOT EXISTS pm.test_plans (
    "Id" uuid NOT NULL,
    "ProjectId" uuid NOT NULL,
    "Key" character varying(30) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Description" text,
    "Status" character varying(20) NOT NULL DEFAULT 'Draft',
    "SprintId" uuid,
    "MilestoneId" uuid,
    "StartDate" date,
    "EndDate" date,
    "AssignedTesterId" character varying(100),
    "TenantId" uuid NOT NULL,
    "RowVersion" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone,
    "ModifiedBy" character varying(100),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "PK_test_plans" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_test_plans_projects" FOREIGN KEY ("ProjectId") REFERENCES pm.projects("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_test_plans_project_key ON pm.test_plans ("ProjectId", "Key");

-- 5. pm.test_plan_scenarios
CREATE TABLE IF NOT EXISTS pm.test_plan_scenarios (
    "Id" uuid NOT NULL,
    "TestPlanId" uuid NOT NULL,
    "TestScenarioId" uuid NOT NULL,
    "AssignedTesterId" character varying(100),
    "SortOrder" integer NOT NULL DEFAULT 0,
    "RowVersion" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "PK_test_plan_scenarios" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_test_plan_scenarios_plans" FOREIGN KEY ("TestPlanId") REFERENCES pm.test_plans("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_test_plan_scenarios_scenarios" FOREIGN KEY ("TestScenarioId") REFERENCES pm.test_scenarios("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_test_plan_scenarios_unique ON pm.test_plan_scenarios ("TestPlanId", "TestScenarioId");

-- 6. pm.test_executions
CREATE TABLE IF NOT EXISTS pm.test_executions (
    "Id" uuid NOT NULL,
    "TestPlanScenarioId" uuid NOT NULL,
    "ExecutedBy" character varying(100) NOT NULL,
    "ExecutedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "Result" character varying(20) NOT NULL DEFAULT 'NotRun',
    "Duration" interval,
    "Notes" text,
    "Environment" character varying(500),
    "LinkedBugId" uuid,
    "TenantId" uuid NOT NULL,
    "RowVersion" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone,
    "ModifiedBy" character varying(100),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "PK_test_executions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_test_executions_plan_scenarios" FOREIGN KEY ("TestPlanScenarioId") REFERENCES pm.test_plan_scenarios("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_test_executions_plan_scenario ON pm.test_executions ("TestPlanScenarioId");

-- 7. pm.test_step_results
CREATE TABLE IF NOT EXISTS pm.test_step_results (
    "Id" uuid NOT NULL,
    "TestExecutionId" uuid NOT NULL,
    "TestStepId" uuid NOT NULL,
    "Result" character varying(20) NOT NULL DEFAULT 'NotRun',
    "ActualResult" character varying(2000),
    "Notes" character varying(1000),
    "RowVersion" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "PK_test_step_results" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_test_step_results_executions" FOREIGN KEY ("TestExecutionId") REFERENCES pm.test_executions("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_test_step_results_steps" FOREIGN KEY ("TestStepId") REFERENCES pm.test_steps("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_test_step_results_unique ON pm.test_step_results ("TestExecutionId", "TestStepId");
