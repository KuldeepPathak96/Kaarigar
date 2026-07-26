/* ============================================================================
   03_Employee_Document_Table.sql
   Screen W-02 (Employee Profile) needs to store uploaded ID Proof and CV/
   Resume files. The EMPLOYEE_DOCUMENT table below already exists in
   01_Database_Schema.sql — this script ONLY adds the additional unique
   index the application code relies on (one row per document type per
   employee, so uploading again is an update-in-place, not a duplicate row).

   Safe to re-run: checks sys.indexes before creating.
   ============================================================================ */

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_EMPLOYEE_DOCUMENT_USER_ACCOUNT_TYPE'
      AND object_id = OBJECT_ID('EMPLOYEE_DOCUMENT')
)
BEGIN
    CREATE UNIQUE INDEX UQ_EMPLOYEE_DOCUMENT_USER_ACCOUNT_TYPE
        ON EMPLOYEE_DOCUMENT (USER_ACCOUNT_ID, DOCUMENT_TYPE_CD);
END
GO
