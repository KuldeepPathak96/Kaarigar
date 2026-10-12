/* ============================================================================
   25_EmployerProfile_BillingInformation.sql
   ============================================================================
   PURPOSE
   -------
   Adds dedicated "Billing Information" fields to EMPLOYER_PROFILE, separate
   from the operational company details already captured (CompanyName,
   CityName/AreaAddressTxt/AddressTxt, BusinessProofNumberTxt). These are
   deliberately distinct because a business's invoicing details (legal
   entity name, registered/billing address, GSTIN, finance contact) often
   differ from its trading name, site address, or the GST/Gumasthadhara
   number captured during onboarding for verification purposes.

   GSTIN_TXT already existed (added in an earlier migration) but was never
   actually surfaced on the Profile page or wired into the invoice PDF's
   "Bill To" block until now — see InvoicePdfService.cs.

   New columns:
     INVOICE_NAME_TXT     - name to print on invoices (falls back to
                             COMPANY_NAME if not set)
     BILLING_ADDRESS_TXT  - falls back to AREA_ADDRESS_TXT + CITY_NAME if
                             not set
     FINANCE_CONTACT_TXT  - name of the person who handles payment/invoices
     ACCOUNTS_EMAIL_TXT   - email for invoice/accounts correspondence

   Idempotent — safe to re-run.
   ============================================================================ */

SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.EMPLOYER_PROFILE') AND name = 'INVOICE_NAME_TXT'
)
BEGIN
    ALTER TABLE dbo.EMPLOYER_PROFILE ADD INVOICE_NAME_TXT NVARCHAR(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.EMPLOYER_PROFILE') AND name = 'BILLING_ADDRESS_TXT'
)
BEGIN
    ALTER TABLE dbo.EMPLOYER_PROFILE ADD BILLING_ADDRESS_TXT NVARCHAR(400) NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.EMPLOYER_PROFILE') AND name = 'FINANCE_CONTACT_TXT'
)
BEGIN
    ALTER TABLE dbo.EMPLOYER_PROFILE ADD FINANCE_CONTACT_TXT NVARCHAR(150) NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.EMPLOYER_PROFILE') AND name = 'ACCOUNTS_EMAIL_TXT'
)
BEGIN
    ALTER TABLE dbo.EMPLOYER_PROFILE ADD ACCOUNTS_EMAIL_TXT NVARCHAR(150) NULL;
END;
GO

-- Validation
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'EMPLOYER_PROFILE'
  AND COLUMN_NAME IN ('INVOICE_NAME_TXT', 'BILLING_ADDRESS_TXT', 'FINANCE_CONTACT_TXT', 'ACCOUNTS_EMAIL_TXT', 'GSTIN_TXT');
