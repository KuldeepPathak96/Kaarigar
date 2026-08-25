/* ============================================================================
   BILLING — Kaarigar Invoice + Website Invoice
   ============================================================================
   Per completed job (i.e. per JOB_APPLICATION reaching STATUS_CD =
   'COMPLETED' via the Satisfaction OTP), the system generates TWO invoices:

     1. KAARIGAR_INVOICE   — Kaarigar's own commission (Service Charge + GST).
                              Payable immediately.
     2. WEBSITE_INVOICE    — the amount that funds the worker (wage + PF
                              Employer Contribution + ESIC Employer
                              Contribution + GST). Employer pays this via a
                              QR-code payment page (payment integration is a
                              separate, later piece — not part of this script).

   Both invoices are 1:1 with a single JOB_APPLICATION — no line-item table
   needed, since each completed shift is already a single worker / single
   job / single amount.

   Also adds two fields EmployerProfile is missing but the invoice "Bill To"
   block requires: GSTIN and a per-employer Service Charge %.

   Safe to re-run: every ALTER/CREATE is guarded with IF NOT EXISTS.
*/

-- ────────────────────────────────────────────────────────────────────────
-- EMPLOYER_PROFILE additions
-- ────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EMPLOYER_PROFILE') AND name = 'GSTIN_TXT')
    ALTER TABLE dbo.EMPLOYER_PROFILE ADD GSTIN_TXT NVARCHAR(15) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EMPLOYER_PROFILE') AND name = 'SERVICE_CHARGE_PCT')
    ALTER TABLE dbo.EMPLOYER_PROFILE ADD SERVICE_CHARGE_PCT DECIMAL(5,2) NULL;
GO
-- NULL means "use the platform default" (currently 15.00%) — see
-- InvoiceCalculationService.DefaultServiceChargePct. Admin can override per
-- employer once an Admin billing-settings screen exists.


-- ────────────────────────────────────────────────────────────────────────
-- KAARIGAR_INVOICE — Kaarigar's own commission invoice, payable immediately
-- ────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'KAARIGAR_INVOICE')
BEGIN
    CREATE TABLE dbo.KAARIGAR_INVOICE (
        KAARIGAR_INVOICE_ID   INT IDENTITY(1,1) PRIMARY KEY,
        INVOICE_NBR           NVARCHAR(40) NOT NULL,
        JOB_APPLICATION_ID    INT NOT NULL,
        JOB_POST_ID           INT NOT NULL,
        EMPLOYER_USER_ACCOUNT_ID INT NOT NULL,
        WORKER_USER_ACCOUNT_ID   INT NOT NULL,

        INVOICE_DT            DATE NOT NULL,
        DUE_DT                DATE NOT NULL,
        PLACE_OF_SUPPLY_TXT   NVARCHAR(100) NULL,
        GST_TYPE_CD           NVARCHAR(10) NOT NULL,   -- 'INTRA' | 'INTER'

        SERVICE_CHARGE_AMT    DECIMAL(12,2) NOT NULL,
        TAXABLE_AMT           DECIMAL(12,2) NOT NULL,
        GST_RATE_PCT          DECIMAL(5,2) NOT NULL DEFAULT 18.00,
        CGST_AMT              DECIMAL(12,2) NOT NULL DEFAULT 0,
        SGST_AMT              DECIMAL(12,2) NOT NULL DEFAULT 0,
        IGST_AMT              DECIMAL(12,2) NOT NULL DEFAULT 0,
        TOTAL_AMT             DECIMAL(12,2) NOT NULL,

        STATUS_CD             NVARCHAR(20) NOT NULL DEFAULT 'DUE',  -- DUE | PAID | OVERDUE | CANCELLED
        PDF_FILE_PATH_TXT     NVARCHAR(400) NULL,
        WHATS_APP_SENT_TS     DATETIME2 NULL,
        PAID_TS               DATETIME2 NULL,

        CREATED_BY            NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        CREATED_TS            DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UPDATED_BY            NVARCHAR(100) NULL,
        UPDATED_TS            DATETIME2 NULL,

        CONSTRAINT UQ_KAARIGAR_INVOICE_JOB_APPLICATION UNIQUE (JOB_APPLICATION_ID),
        CONSTRAINT FK_KAARIGAR_INVOICE_JOB_APPLICATION FOREIGN KEY (JOB_APPLICATION_ID) REFERENCES dbo.JOB_APPLICATION(JOB_APPLICATION_ID),
        CONSTRAINT FK_KAARIGAR_INVOICE_JOB_POST FOREIGN KEY (JOB_POST_ID) REFERENCES dbo.JOB_POST(JOB_POST_ID),
        CONSTRAINT FK_KAARIGAR_INVOICE_EMPLOYER FOREIGN KEY (EMPLOYER_USER_ACCOUNT_ID) REFERENCES dbo.USER_ACCOUNT(USER_ACCOUNT_ID),
        CONSTRAINT FK_KAARIGAR_INVOICE_WORKER FOREIGN KEY (WORKER_USER_ACCOUNT_ID) REFERENCES dbo.USER_ACCOUNT(USER_ACCOUNT_ID)
    );
END;
GO


-- ────────────────────────────────────────────────────────────────────────
-- WEBSITE_INVOICE — funds the worker's payout (wage + PF + ESIC + GST),
-- paid by the Employer via the QR payment page (later feature)
-- ────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WEBSITE_INVOICE')
BEGIN
    CREATE TABLE dbo.WEBSITE_INVOICE (
        WEBSITE_INVOICE_ID    INT IDENTITY(1,1) PRIMARY KEY,
        INVOICE_NBR           NVARCHAR(40) NOT NULL,
        JOB_APPLICATION_ID    INT NOT NULL,
        JOB_POST_ID           INT NOT NULL,
        EMPLOYER_USER_ACCOUNT_ID INT NOT NULL,
        WORKER_USER_ACCOUNT_ID   INT NOT NULL,

        INVOICE_DT            DATE NOT NULL,
        DUE_DT                DATE NOT NULL,
        PLACE_OF_SUPPLY_TXT   NVARCHAR(100) NULL,
        GST_TYPE_CD           NVARCHAR(10) NOT NULL,   -- 'INTRA' | 'INTER'

        HOURS_WORKED_NBR      DECIMAL(6,2) NOT NULL,
        HOURLY_RATE_AMT       DECIMAL(10,2) NOT NULL,
        MANPOWER_AMT          DECIMAL(12,2) NOT NULL,   -- hours * rate (the worker's gross wage for this shift)
        PF_EMPLOYER_AMT       DECIMAL(12,2) NOT NULL DEFAULT 0,
        ESIC_EMPLOYER_AMT     DECIMAL(12,2) NOT NULL DEFAULT 0,
        TAXABLE_AMT           DECIMAL(12,2) NOT NULL,   -- MANPOWER + PF + ESIC
        GST_RATE_PCT          DECIMAL(5,2) NOT NULL DEFAULT 18.00,
        CGST_AMT              DECIMAL(12,2) NOT NULL DEFAULT 0,
        SGST_AMT              DECIMAL(12,2) NOT NULL DEFAULT 0,
        IGST_AMT              DECIMAL(12,2) NOT NULL DEFAULT 0,
        TOTAL_AMT             DECIMAL(12,2) NOT NULL,

        STATUS_CD             NVARCHAR(20) NOT NULL DEFAULT 'PENDING_PAYMENT',  -- PENDING_PAYMENT | PAID | OVERDUE | CANCELLED
        PDF_FILE_PATH_TXT     NVARCHAR(400) NULL,
        WHATS_APP_SENT_TS     DATETIME2 NULL,
        PAID_TS               DATETIME2 NULL,

        CREATED_BY            NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        CREATED_TS            DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UPDATED_BY            NVARCHAR(100) NULL,
        UPDATED_TS            DATETIME2 NULL,

        CONSTRAINT UQ_WEBSITE_INVOICE_JOB_APPLICATION UNIQUE (JOB_APPLICATION_ID),
        CONSTRAINT FK_WEBSITE_INVOICE_JOB_APPLICATION FOREIGN KEY (JOB_APPLICATION_ID) REFERENCES dbo.JOB_APPLICATION(JOB_APPLICATION_ID),
        CONSTRAINT FK_WEBSITE_INVOICE_JOB_POST FOREIGN KEY (JOB_POST_ID) REFERENCES dbo.JOB_POST(JOB_POST_ID),
        CONSTRAINT FK_WEBSITE_INVOICE_EMPLOYER FOREIGN KEY (EMPLOYER_USER_ACCOUNT_ID) REFERENCES dbo.USER_ACCOUNT(USER_ACCOUNT_ID),
        CONSTRAINT FK_WEBSITE_INVOICE_WORKER FOREIGN KEY (WORKER_USER_ACCOUNT_ID) REFERENCES dbo.USER_ACCOUNT(USER_ACCOUNT_ID)
    );
END;
GO


-- ────────────────────────────────────────────────────────────────────────
-- Sequence table for gap-free-ish invoice numbering (KT/26-27/0001 style),
-- separate counters for each invoice type so they don't share a series.
-- ────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'INVOICE_NUMBER_SEQUENCE')
BEGIN
    CREATE TABLE dbo.INVOICE_NUMBER_SEQUENCE (
        SEQUENCE_CD       NVARCHAR(30) NOT NULL PRIMARY KEY,  -- 'KAARIGAR_INVOICE' | 'WEBSITE_INVOICE'
        FINANCIAL_YEAR_TXT NVARCHAR(10) NOT NULL,             -- e.g. '26-27'
        LAST_NUMBER_NBR   INT NOT NULL DEFAULT 0
    );

    INSERT INTO dbo.INVOICE_NUMBER_SEQUENCE (SEQUENCE_CD, FINANCIAL_YEAR_TXT, LAST_NUMBER_NBR)
    VALUES ('KAARIGAR_INVOICE', '26-27', 0), ('WEBSITE_INVOICE', '26-27', 0);
END;
GO
