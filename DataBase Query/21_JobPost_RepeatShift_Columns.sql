/* ============================================================================
   21_JobPost_RepeatShift_Columns.sql
   ============================================================================
   PURPOSE
   -------
   Supports the "Repeat Shift" quick action: an employer can re-post a past
   job with all details prefilled except date/time, and optionally ask the
   system to invite the SAME workers who completed the original shift first
   (giving them a priority window) before broadcasting to everyone else.

   Adds two nullable/defaulted columns to JOB_POST:
     - REPEATED_FROM_JOB_POST_ID : self-referencing FK to the original job
       this one was repeated from. NULL for normal (non-repeat) job posts.
     - IS_REPEAT_STAFF_PRIORITY_FL : 1 if the employer checked "Repeat Staff"
       — i.e. notify the previous shift's completed workers first, then
       broadcast to everyone else if the shift isn't fully filled after the
       priority window.

   Idempotent — safe to re-run.
   ============================================================================ */

SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.JOB_POST') AND name = 'REPEATED_FROM_JOB_POST_ID'
)
BEGIN
    ALTER TABLE dbo.JOB_POST ADD REPEATED_FROM_JOB_POST_ID INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_JOB_POST_REPEATED_FROM'
)
BEGIN
    ALTER TABLE dbo.JOB_POST
        ADD CONSTRAINT FK_JOB_POST_REPEATED_FROM
        FOREIGN KEY (REPEATED_FROM_JOB_POST_ID) REFERENCES dbo.JOB_POST(JOB_POST_ID);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.JOB_POST') AND name = 'IS_REPEAT_STAFF_PRIORITY_FL'
)
BEGIN
    ALTER TABLE dbo.JOB_POST ADD IS_REPEAT_STAFF_PRIORITY_FL BIT NOT NULL CONSTRAINT DF_JOB_POST_IS_REPEAT_STAFF_PRIORITY_FL DEFAULT (0);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.JOB_POST') AND name = 'IX_JOB_POST_REPEATED_FROM_JOB_POST_ID'
)
BEGIN
    CREATE INDEX IX_JOB_POST_REPEATED_FROM_JOB_POST_ID ON dbo.JOB_POST(REPEATED_FROM_JOB_POST_ID);
END;
GO

-- Validation
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'JOB_POST' AND COLUMN_NAME IN ('REPEATED_FROM_JOB_POST_ID', 'IS_REPEAT_STAFF_PRIORITY_FL');
