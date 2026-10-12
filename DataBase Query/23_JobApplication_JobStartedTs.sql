/* ============================================================================
   23_JobApplication_JobStartedTs.sql
   ============================================================================
   PURPOSE
   -------
   Supports the new "Today's Worker" section on the Employer dashboard,
   which needs an actual check-in TIME per worker. Previously the only
   timestamp on JOB_APPLICATION was the generic UPDATED_TS, which gets
   overwritten again when the application later moves to COMPLETED — so it
   can't reliably answer "what time did this worker check in?".

   Adds JOB_STARTED_TS, set once (and only once) the moment
   EmployeeJobDao.AdvanceStatusAsync transitions an application's
   STATUS_CD to 'JOB_STARTED' (i.e. the worker entered the Job Starting OTP).

   Idempotent — safe to re-run.
   ============================================================================ */

SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.JOB_APPLICATION') AND name = 'JOB_STARTED_TS'
)
BEGIN
    ALTER TABLE dbo.JOB_APPLICATION ADD JOB_STARTED_TS DATETIME2 NULL;
END;
GO

-- Validation
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'JOB_APPLICATION' AND COLUMN_NAME = 'JOB_STARTED_TS';
