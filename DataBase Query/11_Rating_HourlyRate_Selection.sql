/* ============================================================================
   10. RATING + HOURLY RATE + SKILL-BASED JOB TITLE + SELECTION WORKFLOW
   ============================================================================
   Covers:
   1. KAARIGAR_RATING       - employer rates the Kaarigar once JOB_APPLICATION
                               is COMPLETED and salary is released.
   2. JOB_POST.PRIMARY_SKILL_ID + HOURLY_WAGE_AMT
                             - Job Title is now driven by a chosen SKILL
                               (JOB_TITLE column keeps storing the skill name
                               for backward compatibility / display), and the
                               wage is per-hour instead of per-day.
   3. JOB_APPLICATION.STATUS_CD gets two new values: SELECTED and CANCELLED,
      plus a SALARY_RELEASED_FL/TS pair, to drive the
      "5 selected -> job CLOSED, cancel one -> job reopens" workflow.
   ============================================================================ */

/* ---- 1. JOB_POST: hourly rate + skill-driven title ---------------------- */
ALTER TABLE JOB_POST ADD HOURLY_WAGE_AMT DECIMAL(10,2) NULL;
GO

-- Backfill from the old daily wage as a rough starting point (assumes 8-hr day);
-- review/adjust manually, then drop DAILY_WAGE_AMT once confirmed unused elsewhere.
UPDATE JOB_POST SET HOURLY_WAGE_AMT = DAILY_WAGE_AMT / 8.0 WHERE DAILY_WAGE_AMT IS NOT NULL;
GO

ALTER TABLE JOB_POST ADD PRIMARY_SKILL_ID INT NULL;
GO
ALTER TABLE JOB_POST ADD CONSTRAINT FK_JOB_POST_PRIMARY_SKILL
    FOREIGN KEY (PRIMARY_SKILL_ID) REFERENCES SKILL(SKILL_ID);
GO

-- Once existing rows are backfilled (JOB_TITLE matched to a SKILL_ID) and the
-- app is confirmed working end-to-end, these can be dropped:
-- ALTER TABLE JOB_POST DROP COLUMN DAILY_WAGE_AMT;

/* ---- 2. JOB_APPLICATION: selection workflow + salary release ------------ */
ALTER TABLE JOB_APPLICATION DROP CONSTRAINT CK_JOB_APPLICATION_STATUS_CD;
GO
ALTER TABLE JOB_APPLICATION ADD CONSTRAINT CK_JOB_APPLICATION_STATUS_CD
    CHECK (STATUS_CD IN (
        'PENDING','EMPLOYER_VIEWED','EMPLOYER_CONTACTED',
        'SELECTED','JOB_STARTED','COMPLETED','CANCELLED'
    ));
GO

ALTER TABLE JOB_APPLICATION ADD SELECTED_TS DATETIME2 NULL;
GO
ALTER TABLE JOB_APPLICATION ADD SALARY_RELEASED_FL BIT NOT NULL
    CONSTRAINT DF_JOB_APPLICATION_SALARY_RELEASED_FL DEFAULT (0);
GO
ALTER TABLE JOB_APPLICATION ADD SALARY_RELEASED_TS DATETIME2 NULL;
GO

/* ---- 3. KAARIGAR_RATING --------------------------------------------------
   One row per JOB_APPLICATION, created only after the application is
   COMPLETED and SALARY_RELEASED_FL = 1. Employer -> Kaarigar direction only.
   ---------------------------------------------------------------------- */
CREATE TABLE KAARIGAR_RATING (
    KAARIGAR_RATING_ID        INT IDENTITY(1,1)   NOT NULL,
    JOB_APPLICATION_ID        INT                 NOT NULL,
    JOB_POST_ID                INT                 NOT NULL,
    EMPLOYEE_USER_ACCOUNT_ID   INT                 NOT NULL,
    EMPLOYER_USER_ACCOUNT_ID   INT                 NOT NULL,
    RATING_NBR                  TINYINT             NOT NULL,
    REVIEW_TXT                   NVARCHAR(1000)      NULL,
    RATED_TS                      DATETIME2           NOT NULL CONSTRAINT DF_KAARIGAR_RATING_RATED_TS DEFAULT (SYSUTCDATETIME()),

    CREATED_BY                    NVARCHAR(100)       NOT NULL CONSTRAINT DF_KAARIGAR_RATING_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS                    DATETIME2           NOT NULL CONSTRAINT DF_KAARIGAR_RATING_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR                VARCHAR(45)         NULL,
    UPDATED_BY                     NVARCHAR(100)       NULL,
    UPDATED_TS                      DATETIME2          NULL,
    UPDATED_IP_ADDR                 VARCHAR(45)        NULL,

    CONSTRAINT PK_KAARIGAR_RATING PRIMARY KEY (KAARIGAR_RATING_ID),
    CONSTRAINT UQ_KAARIGAR_RATING_APPLICATION UNIQUE (JOB_APPLICATION_ID),
    CONSTRAINT CK_KAARIGAR_RATING_NBR CHECK (RATING_NBR BETWEEN 1 AND 5),
    CONSTRAINT FK_KAARIGAR_RATING_APPLICATION FOREIGN KEY (JOB_APPLICATION_ID) REFERENCES JOB_APPLICATION(JOB_APPLICATION_ID),
    CONSTRAINT FK_KAARIGAR_RATING_JOB_POST FOREIGN KEY (JOB_POST_ID) REFERENCES JOB_POST(JOB_POST_ID),
    CONSTRAINT FK_KAARIGAR_RATING_EMPLOYEE FOREIGN KEY (EMPLOYEE_USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID),
    CONSTRAINT FK_KAARIGAR_RATING_EMPLOYER FOREIGN KEY (EMPLOYER_USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID)
);
GO

CREATE INDEX IX_KAARIGAR_RATING_EMPLOYEE ON KAARIGAR_RATING(EMPLOYEE_USER_ACCOUNT_ID);
GO
