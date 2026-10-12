/* ============================================================================
   24_JobPost_NoteForWorker.sql
   ============================================================================
   Adds JOB_POST.NOTE_FOR_WORKER_TXT — an optional employer note shown to
   workers (e.g. "Wear black shoes, full-sleeve uniform required, no
   jewellery on the floor"). Shown on the Post Job form, the Browse Jobs
   list, and the worker's My Applications page.

   Idempotent — safe to re-run.
   ============================================================================ */

SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.JOB_POST') AND name = 'NOTE_FOR_WORKER_TXT'
)
BEGIN
    ALTER TABLE dbo.JOB_POST ADD NOTE_FOR_WORKER_TXT NVARCHAR(1000) NULL;
END;
GO

-- Validation
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'JOB_POST' AND COLUMN_NAME = 'NOTE_FOR_WORKER_TXT';
