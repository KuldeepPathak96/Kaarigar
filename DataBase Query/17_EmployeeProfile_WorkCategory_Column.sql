/* ============================================================================
   19_EmployeeProfile_WorkCategory_Column.sql
   ============================================================================
   Adds EMPLOYEE_PROFILE.WORK_CATEGORY_ID, mirroring the existing
   EMPLOYER_PROFILE.BUSINESS_CATEGORY_ID pattern. Needed because the Register
   page now captures a Worker's Work Category and persists it (previously it
   was UI-only / not saved anywhere).

   Idempotent — safe to re-run.
   ============================================================================ */

SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.EMPLOYEE_PROFILE') AND name = 'WORK_CATEGORY_ID'
)
BEGIN
    ALTER TABLE dbo.EMPLOYEE_PROFILE ADD WORK_CATEGORY_ID INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EMPLOYEE_PROFILE_WORK_CATEGORY'
)
BEGIN
    ALTER TABLE dbo.EMPLOYEE_PROFILE
        ADD CONSTRAINT FK_EMPLOYEE_PROFILE_WORK_CATEGORY
        FOREIGN KEY (WORK_CATEGORY_ID) REFERENCES dbo.WORK_CATEGORY(WORK_CATEGORY_ID);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.EMPLOYEE_PROFILE') AND name = 'IX_EMPLOYEE_PROFILE_WORK_CATEGORY_ID'
)
BEGIN
    CREATE INDEX IX_EMPLOYEE_PROFILE_WORK_CATEGORY_ID ON dbo.EMPLOYEE_PROFILE(WORK_CATEGORY_ID);
END;
GO

-- Validation
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'EMPLOYEE_PROFILE' AND COLUMN_NAME = 'WORK_CATEGORY_ID';
