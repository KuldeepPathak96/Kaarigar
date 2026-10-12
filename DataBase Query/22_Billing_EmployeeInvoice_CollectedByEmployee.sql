/* ============================================================================
   22_Billing_EmployeeInvoice_CollectedByEmployee.sql
   ============================================================================
   PURPOSE
   -------
   Business terminology change on the billing side (see InvoiceGenerationService.cs
   for the full mapping comment):

     - The invoice that used to fund the worker's payout via a QR-code
       platform payment (table WEBSITE_INVOICE, class WebsiteInvoice) is now
       the "Employee Invoice": Hours x Rate ONLY -- no PF Employer
       Contribution, no ESIC Employer Contribution, no GST. The Employer
       pays the worker this amount directly (cash/UPI/etc, outside the
       platform), and the worker marks it collected themselves.

     - Kaarigar's own commission invoice (table KAARIGAR_INVOICE, class
       KaarigarInvoice) is now the "Website Invoice": 19% Service Fee on
       the Employee Invoice amount, plus 18% GST computed on (Employee
       Invoice Amount + Service Fee Amount). This is the only amount the
       platform itself bills the Employer for.

   The table/class names themselves are NOT renamed in this pass (would
   require touching a much larger surface -- DbSet names, routes,
   Views/AdminBilling, Views/Billing -- for a purely cosmetic rename, with
   real risk of breaking something). Only the calculation logic and
   UI-facing labels changed; this script only needs to add ONE new column.

   This script is idempotent -- safe to re-run.
   ============================================================================ */

SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.WEBSITE_INVOICE') AND name = 'COLLECTED_BY_EMPLOYEE_TS'
)
BEGIN
    ALTER TABLE dbo.WEBSITE_INVOICE ADD COLLECTED_BY_EMPLOYEE_TS DATETIME2 NULL;
END;
GO

-- No CHECK constraint exists on WEBSITE_INVOICE.STATUS_CD (confirmed in
-- 21_Billing_KaarigarInvoice_WebsiteInvoice.sql), so the new 'COLLECTED'
-- status value used by the app needs no schema change here.

-- Validation
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WEBSITE_INVOICE' AND COLUMN_NAME = 'COLLECTED_BY_EMPLOYEE_TS';
