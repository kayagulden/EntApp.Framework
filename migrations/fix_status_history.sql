UPDATE req."ticket_status_history" SET "OldStatus" = 'AllChildrenDone' WHERE "OldStatus" = 'AllTasksDone';
UPDATE req."ticket_status_history" SET "NewStatus" = 'AllChildrenDone' WHERE "NewStatus" = 'AllTasksDone';
