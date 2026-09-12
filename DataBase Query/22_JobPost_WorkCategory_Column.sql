/* ============================================================================
   JOB_POST — Work Category (Post Job "Worker Category" now uses the real
   Business/Work Category system instead of a flat skill-name dropdown)
   ============================================================================
   Adds a nullable WORK_CATEGORY_ID column to JOB_POST, mirroring
   EMPLOYEE_PROFILE.WORK_CATEGORY_ID. This lets the Post Job page reuse the
   same Work Category -> Skills cascade already used on Registration and the
   Worker's own Profile page, instead of the old flat "pick any skill name as
   the job title" dropdown.

   Depends on: the WORK_CATEGORY table already existing (created by the
   16_WorkCategory... script). Safe to re-run.
*/

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JOB_POST') AND name = 'WORK_CATEGORY_ID')
BEGIN
    ALTER TABLE dbo.JOB_POST ADD WORK_CATEGORY_ID INT NULL;

    ALTER TABLE dbo.JOB_POST
        ADD CONSTRAINT FK_JOB_POST_WORK_CATEGORY
        FOREIGN KEY (WORK_CATEGORY_ID)
        REFERENCES dbo.WORK_CATEGORY(WORK_CATEGORY_ID);
END;
GO
