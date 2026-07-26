/* ============================================================================
   JOB_POST — hourly-rate / hours-based duration / category removal
   ============================================================================
   Changes requested by the client:
     - Job Title is now chosen from SKILL (no schema change needed — JOB_TITLE
       stays a text column, just populated from the selected skill's name).
     - Job Category is removed from the UI everywhere. JOB_CATEGORY_CD is
       kept (nullable) for old rows rather than dropped, but new jobs no
       longer set it.
     - "Daily Wage" becomes "Hourly Rate", picked from an admin-editable
       dropdown (HOURLY_RATE_OPTION) rather than typed freely.
     - "Duration (days)" + separate Start/End time-of-day becomes a single
       Start Date+Time plus a Duration in HOURS; end is calculated
       (StartDateTime + DurationHourNbr hours) rather than stored.
   DAILY_WAGE_AMT, DURATION_DAY_NBR and END_TIME are left in place (nullable,
   unused going forward) rather than dropped, so existing historical job
   posts don't lose data.

   This script is safe to re-run: every step checks whether it already
   applied before doing anything, so running it twice (or resuming after a
   partial failure) won't error out.
*/

-- ── Step 1: drop objects that block making JOB_CATEGORY_CD nullable ────────
-- (default constraint, check constraint, index — SQL Server won't let you
-- ALTER COLUMN while any of these still reference it)

IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_JOB_POST_JOB_CATEGORY_CD')
    ALTER TABLE JOB_POST DROP CONSTRAINT DF_JOB_POST_JOB_CATEGORY_CD;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_JOB_POST_JOB_CATEGORY_CD')
    ALTER TABLE JOB_POST DROP CONSTRAINT CK_JOB_POST_JOB_CATEGORY_CD;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JOB_POST_JOB_CATEGORY_CD' AND object_id = OBJECT_ID('JOB_POST'))
    DROP INDEX IX_JOB_POST_JOB_CATEGORY_CD ON JOB_POST;
GO

-- ── Step 2: make Job Category optional going forward ────────────────────────
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('JOB_POST') AND name = 'JOB_CATEGORY_CD' AND is_nullable = 0
)
    ALTER TABLE JOB_POST ALTER COLUMN JOB_CATEGORY_CD NVARCHAR(50) NULL;
GO

-- ── Step 3: new columns for hourly rate + hours-based duration ──────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JOB_POST') AND name = 'HOURLY_WAGE_AMT')
    ALTER TABLE JOB_POST ADD HOURLY_WAGE_AMT DECIMAL(10,2) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JOB_POST') AND name = 'DURATION_HOUR_NBR')
    ALTER TABLE JOB_POST ADD DURATION_HOUR_NBR INT NULL;
GO

-- ── Step 4: HOURLY_RATE_OPTION — admin-editable preset list for the Hourly Rate dropdown ──
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HOURLY_RATE_OPTION')
BEGIN
    CREATE TABLE HOURLY_RATE_OPTION (
        RATE_OPTION_ID      INT IDENTITY(1,1) PRIMARY KEY,
        RATE_LABEL_TXT      NVARCHAR(100)   NOT NULL,
        HOURLY_RATE_AMT     DECIMAL(10,2)   NOT NULL,
        DISPLAY_ORDER_NBR   INT             NOT NULL DEFAULT 0,
        IS_ACTIVE_FL        BIT             NOT NULL DEFAULT 1,
        CREATED_BY          NVARCHAR(50)    NULL,
        CREATED_TS          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UPDATED_BY          NVARCHAR(50)    NULL,
        UPDATED_TS          DATETIME2       NULL
    );
END
GO

-- Placeholder seed values — Admin can edit/add/remove from Admin > Hourly Rates.
-- Only seeds if the table is empty, so re-running this script won't duplicate rows.
IF NOT EXISTS (SELECT 1 FROM HOURLY_RATE_OPTION)
BEGIN
    INSERT INTO HOURLY_RATE_OPTION (RATE_LABEL_TXT, HOURLY_RATE_AMT, DISPLAY_ORDER_NBR, CREATED_BY) VALUES
        (N'₹50 / hour',  50.00, 1, 'SEED_SCRIPT'),
        (N'₹80 / hour',  80.00, 2, 'SEED_SCRIPT'),
        (N'₹100 / hour', 100.00, 3, 'SEED_SCRIPT'),
        (N'₹150 / hour', 150.00, 4, 'SEED_SCRIPT');
END
GO