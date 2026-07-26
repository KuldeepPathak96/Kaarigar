/* ============================================================================
   TEMPORARY MANPOWER HIRING PLATFORM (MVP)
   Database: SQL Server
   Script:   01_Database_Schema.sql   (v2 - Enterprise Naming Convention)
   ----------------------------------------------------------------------------
   NAMING CONVENTION (Product/Enterprise Standard):
     - Tables          : SINGULAR, ALL CAPS, underscore separated   -> EMPLOYEE
     - Columns          : ALL CAPS, underscore separated             -> FIRST_NAME
     - Primary Key      : <TABLE>_ID                                  -> EMPLOYEE_ID
     - Foreign Key      : <REFERENCED_TABLE>_ID                       -> EMPLOYEE_ID (in child table)
     - Number/Phone     : suffix _NBR                                 -> CONTACT_NBR, ACCOUNT_NBR
     - Identifier/Code  : suffix _ID (system) or _CD (lookup code)    -> EMAIL_ID, STATUS_CD
     - Date only        : suffix _DT                                  -> START_DT
     - Date + Time       : suffix _TS (timestamp)                      -> CREATED_TS
     - Flag (true/false) : suffix _FL                                  -> IS_ACTIVE_FL
     - Free text/desc   : suffix _TXT or _DESC                        -> DESCRIPTION_TXT
     - Constraints       : PK_<TABLE>, FK_<TABLE>_<REF>, UQ_<TABLE>_<COL>, CK_<TABLE>_<COL>, IX_<TABLE>_<COL>

   STANDARD AUDIT COLUMNS (present on every table):
     CREATED_BY          NVARCHAR(100)   -- user/system that created the row
     CREATED_TS          DATETIME2       -- timestamp of creation (UTC)
     CREATED_IP_ADDR     VARCHAR(45)     -- IP address of creator (IPv4/IPv6)
     UPDATED_BY          NVARCHAR(100)   -- user/system that last updated the row
     UPDATED_TS          DATETIME2       -- timestamp of last update (UTC)
     UPDATED_IP_ADDR     VARCHAR(45)     -- IP address of last updater
   ============================================================================ */

CREATE DATABASE MANPOWER_HIRING_DB;
GO

USE MANPOWER_HIRING_DB;
GO

/* ============================================================================
   1. USER_ACCOUNT  [WEEK 1]
   Central table for all logins: EMPLOYER, EMPLOYEE, ADMIN.
   ============================================================================ */
CREATE TABLE USER_ACCOUNT (
    USER_ACCOUNT_ID       INT IDENTITY(1,1)      NOT NULL,
    FIRST_NAME             NVARCHAR(75)            NOT NULL,
    LAST_NAME              NVARCHAR(75)            NULL,
    CONTACT_NBR            VARCHAR(15)             NOT NULL,   -- mobile number
    EMAIL_ID               NVARCHAR(150)           NULL,
    PASSWORD_HASH_TXT      NVARCHAR(255)           NOT NULL,   -- store hashed password only
    ROLE_CD                VARCHAR(20)             NOT NULL,   -- 'EMPLOYER' | 'EMPLOYEE' | 'ADMIN'
    IS_MOBILE_VERIFIED_FL  BIT                     NOT NULL CONSTRAINT DF_USER_ACCOUNT_MOBILE_VERIFIED_FL DEFAULT (0),
    IS_ACTIVE_FL           BIT                     NOT NULL CONSTRAINT DF_USER_ACCOUNT_ACTIVE_FL DEFAULT (1),
    IS_APPROVED_FL         BIT                     NOT NULL CONSTRAINT DF_USER_ACCOUNT_APPROVED_FL DEFAULT (0),
    LAST_LOGIN_TS          DATETIME2               NULL,

    CREATED_BY             NVARCHAR(100)           NOT NULL CONSTRAINT DF_USER_ACCOUNT_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS             DATETIME2               NOT NULL CONSTRAINT DF_USER_ACCOUNT_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR        VARCHAR(45)              NULL,
    UPDATED_BY             NVARCHAR(100)           NULL,
    UPDATED_TS             DATETIME2               NULL,
    UPDATED_IP_ADDR        VARCHAR(45)              NULL,

    CONSTRAINT PK_USER_ACCOUNT PRIMARY KEY (USER_ACCOUNT_ID),
    CONSTRAINT UQ_USER_ACCOUNT_CONTACT_NBR UNIQUE (CONTACT_NBR),
    CONSTRAINT CK_USER_ACCOUNT_ROLE_CD CHECK (ROLE_CD IN ('EMPLOYER', 'EMPLOYEE', 'ADMIN'))
);
GO

CREATE UNIQUE INDEX UQ_USER_ACCOUNT_EMAIL_ID ON USER_ACCOUNT(EMAIL_ID) WHERE EMAIL_ID IS NOT NULL;
GO

CREATE INDEX IX_USER_ACCOUNT_ROLE_CD ON USER_ACCOUNT(ROLE_CD);
GO


/* ============================================================================
   2. EMPLOYER_PROFILE  [WEEK 1 structure / data entry starts Week 2]
   ============================================================================ */
CREATE TABLE EMPLOYER_PROFILE (
    EMPLOYER_PROFILE_ID    INT IDENTITY(1,1)      NOT NULL,
    USER_ACCOUNT_ID         INT                     NOT NULL,
    COMPANY_NAME            NVARCHAR(200)           NULL,
    LOGO_FILE_PATH_TXT      NVARCHAR(400)           NULL,
    CITY_NAME               NVARCHAR(100)           NULL,
    AREA_ADDRESS_TXT        NVARCHAR(300)           NULL,
    LATITUDE_NBR            DECIMAL(9,6)            NULL,
    LONGITUDE_NBR           DECIMAL(9,6)            NULL,

    CREATED_BY              NVARCHAR(100)           NOT NULL CONSTRAINT DF_EMPLOYER_PROFILE_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS              DATETIME2               NOT NULL CONSTRAINT DF_EMPLOYER_PROFILE_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR         VARCHAR(45)              NULL,
    UPDATED_BY              NVARCHAR(100)           NULL,
    UPDATED_TS              DATETIME2               NULL,
    UPDATED_IP_ADDR         VARCHAR(45)              NULL,

    CONSTRAINT PK_EMPLOYER_PROFILE PRIMARY KEY (EMPLOYER_PROFILE_ID),
    CONSTRAINT FK_EMPLOYER_PROFILE_USER_ACCOUNT FOREIGN KEY (USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID) ON DELETE CASCADE,
    CONSTRAINT UQ_EMPLOYER_PROFILE_USER_ACCOUNT_ID UNIQUE (USER_ACCOUNT_ID)
);
GO


/* ============================================================================
   3. EMPLOYEE_PROFILE  [WEEK 1 structure / data entry starts Week 3]
   ============================================================================ */
CREATE TABLE EMPLOYEE_PROFILE (
    EMPLOYEE_PROFILE_ID     INT IDENTITY(1,1)      NOT NULL,
    USER_ACCOUNT_ID          INT                     NOT NULL,
    CITY_NAME                NVARCHAR(100)           NULL,
    AREA_ADDRESS_TXT         NVARCHAR(300)           NULL,
    LATITUDE_NBR             DECIMAL(9,6)            NULL,
    LONGITUDE_NBR            DECIMAL(9,6)            NULL,
    PREFERRED_RADIUS_NBR     INT                      NULL,   -- 5 / 10 / 25 / 50 / NULL = Any (km)

    CREATED_BY               NVARCHAR(100)           NOT NULL CONSTRAINT DF_EMPLOYEE_PROFILE_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS               DATETIME2               NOT NULL CONSTRAINT DF_EMPLOYEE_PROFILE_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR          VARCHAR(45)              NULL,
    UPDATED_BY                NVARCHAR(100)           NULL,
    UPDATED_TS                DATETIME2               NULL,
    UPDATED_IP_ADDR           VARCHAR(45)              NULL,

    CONSTRAINT PK_EMPLOYEE_PROFILE PRIMARY KEY (EMPLOYEE_PROFILE_ID),
    CONSTRAINT FK_EMPLOYEE_PROFILE_USER_ACCOUNT FOREIGN KEY (USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID) ON DELETE CASCADE,
    CONSTRAINT UQ_EMPLOYEE_PROFILE_USER_ACCOUNT_ID UNIQUE (USER_ACCOUNT_ID)
);
GO


/* ============================================================================
   4. EMPLOYEE_DOCUMENT  [WEEK 1]
   Stores uploaded ID proof + Resume.
   Only the LAST 4 DIGITS of the ID number are stored as text (for Admin's
   quick reference) — the actual document file is what Admin opens to verify
   the full ID. SERVER_FILE_PATH_TXT holds the server-side storage location.
   ============================================================================ */
CREATE TABLE EMPLOYEE_DOCUMENT (
    EMPLOYEE_DOCUMENT_ID         INT IDENTITY(1,1)      NOT NULL,
    USER_ACCOUNT_ID               INT                     NOT NULL,
    DOCUMENT_TYPE_CD               VARCHAR(30)             NOT NULL,   -- 'ID_PROOF' | 'RESUME'
    DOCUMENT_SUBTYPE_CD            VARCHAR(30)              NULL,       -- 'AADHAAR','PAN','VOTER_ID','DRIVING_LICENSE'
    ID_LAST_FOUR_DIGIT_TXT         VARCHAR(4)               NULL,       -- last 4 digits/chars of ID number (NULL for RESUME)
    ORIGINAL_FILE_NAME_TXT         NVARCHAR(255)           NOT NULL,   -- name as uploaded by user
    STORED_FILE_NAME_TXT           NVARCHAR(255)           NOT NULL,   -- system-generated unique name saved on disk
    SERVER_FILE_PATH_TXT           NVARCHAR(500)           NOT NULL,   -- full path where file is stored
    FILE_SIZE_KB_NBR                INT                      NULL,
    MIME_TYPE_TXT                   VARCHAR(100)             NULL,
    UPLOADED_TS                     DATETIME2                NOT NULL CONSTRAINT DF_EMPLOYEE_DOCUMENT_UPLOADED_TS DEFAULT (SYSUTCDATETIME()),

    -- ADMIN REVIEW FIELDS
    REVIEW_STATUS_CD                VARCHAR(20)              NOT NULL CONSTRAINT DF_EMPLOYEE_DOCUMENT_REVIEW_STATUS_CD DEFAULT ('PENDING'),
    REVIEWED_BY_USER_ACCOUNT_ID     INT                      NULL,       -- FK to USER_ACCOUNT (the ADMIN who reviewed)
    REVIEWED_TS                     DATETIME2                NULL,
    REJECTION_REASON_TXT             NVARCHAR(300)            NULL,

    CREATED_BY                       NVARCHAR(100)           NOT NULL CONSTRAINT DF_EMPLOYEE_DOCUMENT_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS                       DATETIME2               NOT NULL CONSTRAINT DF_EMPLOYEE_DOCUMENT_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR                  VARCHAR(45)              NULL,
    UPDATED_BY                        NVARCHAR(100)           NULL,
    UPDATED_TS                        DATETIME2               NULL,
    UPDATED_IP_ADDR                   VARCHAR(45)              NULL,

    CONSTRAINT PK_EMPLOYEE_DOCUMENT PRIMARY KEY (EMPLOYEE_DOCUMENT_ID),
    CONSTRAINT FK_EMPLOYEE_DOCUMENT_USER_ACCOUNT FOREIGN KEY (USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID) ON DELETE CASCADE,
    CONSTRAINT FK_EMPLOYEE_DOCUMENT_REVIEWED_BY FOREIGN KEY (REVIEWED_BY_USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID),
    CONSTRAINT CK_EMPLOYEE_DOCUMENT_TYPE_CD CHECK (DOCUMENT_TYPE_CD IN ('ID_PROOF', 'RESUME')),
    CONSTRAINT CK_EMPLOYEE_DOCUMENT_REVIEW_STATUS_CD CHECK (REVIEW_STATUS_CD IN ('PENDING', 'APPROVED', 'REJECTED'))
);
GO

CREATE INDEX IX_EMPLOYEE_DOCUMENT_USER_ACCOUNT_ID ON EMPLOYEE_DOCUMENT(USER_ACCOUNT_ID);
CREATE INDEX IX_EMPLOYEE_DOCUMENT_REVIEW_STATUS_CD ON EMPLOYEE_DOCUMENT(REVIEW_STATUS_CD);
GO


/* ============================================================================
   5. SKILL  [FUTURE WEEK — Week 3, created now to keep schema complete]
   ============================================================================ */
CREATE TABLE SKILL (
    SKILL_ID                INT IDENTITY(1,1)      NOT NULL,
    SKILL_NAME               NVARCHAR(100)           NOT NULL,
    CATEGORY_NAME            NVARCHAR(100)           NULL,
    IS_ACTIVE_FL              BIT                     NOT NULL CONSTRAINT DF_SKILL_ACTIVE_FL DEFAULT (1),

    CREATED_BY                NVARCHAR(100)           NOT NULL CONSTRAINT DF_SKILL_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS                DATETIME2               NOT NULL CONSTRAINT DF_SKILL_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR           VARCHAR(45)              NULL,
    UPDATED_BY                 NVARCHAR(100)           NULL,
    UPDATED_TS                 DATETIME2               NULL,
    UPDATED_IP_ADDR            VARCHAR(45)              NULL,

    CONSTRAINT PK_SKILL PRIMARY KEY (SKILL_ID),
    CONSTRAINT UQ_SKILL_NAME UNIQUE (SKILL_NAME)
);
GO

CREATE TABLE EMPLOYEE_SKILL (
    EMPLOYEE_SKILL_ID         INT IDENTITY(1,1)      NOT NULL,
    USER_ACCOUNT_ID             INT                     NOT NULL,   -- references USER_ACCOUNT (EMPLOYEE)
    SKILL_ID                    INT                     NOT NULL,

    CREATED_BY                  NVARCHAR(100)           NOT NULL CONSTRAINT DF_EMPLOYEE_SKILL_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS                  DATETIME2               NOT NULL CONSTRAINT DF_EMPLOYEE_SKILL_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR             VARCHAR(45)              NULL,
    UPDATED_BY                   NVARCHAR(100)           NULL,
    UPDATED_TS                   DATETIME2               NULL,
    UPDATED_IP_ADDR              VARCHAR(45)              NULL,

    CONSTRAINT PK_EMPLOYEE_SKILL PRIMARY KEY (EMPLOYEE_SKILL_ID),
    CONSTRAINT FK_EMPLOYEE_SKILL_USER_ACCOUNT FOREIGN KEY (USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID) ON DELETE CASCADE,
    CONSTRAINT FK_EMPLOYEE_SKILL_SKILL FOREIGN KEY (SKILL_ID) REFERENCES SKILL(SKILL_ID),
    CONSTRAINT UQ_EMPLOYEE_SKILL UNIQUE (USER_ACCOUNT_ID, SKILL_ID)
);
GO


/* ============================================================================
   6. JOB_POST  [FUTURE WEEK — Week 2]
   ============================================================================ */
CREATE TABLE JOB_POST (
    JOB_POST_ID                  INT IDENTITY(1,1)      NOT NULL,
    EMPLOYER_USER_ACCOUNT_ID       INT                     NOT NULL,
    JOB_TITLE                       NVARCHAR(200)           NOT NULL,
    DESCRIPTION_TXT                  NVARCHAR(MAX)           NULL,
    REQUIRED_WORKER_NBR              INT                     NOT NULL CONSTRAINT DF_JOB_POST_REQUIRED_WORKER_NBR DEFAULT (1),
    DAILY_WAGE_AMT                   DECIMAL(10,2)           NULL,
    START_DT                          DATE                     NULL,
    DURATION_DAY_NBR                  INT                      NULL,
    LOCATION_ADDRESS_TXT              NVARCHAR(300)            NULL,
    LATITUDE_NBR                      DECIMAL(9,6)            NULL,
    LONGITUDE_NBR                     DECIMAL(9,6)            NULL,
    CONTACT_NBR                       VARCHAR(15)              NULL,
    STATUS_CD                         VARCHAR(20)              NOT NULL CONSTRAINT DF_JOB_POST_STATUS_CD DEFAULT ('ACTIVE'),

    CREATED_BY                        NVARCHAR(100)           NOT NULL CONSTRAINT DF_JOB_POST_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS                        DATETIME2               NOT NULL CONSTRAINT DF_JOB_POST_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR                   VARCHAR(45)              NULL,
    UPDATED_BY                         NVARCHAR(100)           NULL,
    UPDATED_TS                         DATETIME2               NULL,
    UPDATED_IP_ADDR                    VARCHAR(45)              NULL,

    CONSTRAINT PK_JOB_POST PRIMARY KEY (JOB_POST_ID),
    CONSTRAINT FK_JOB_POST_EMPLOYER FOREIGN KEY (EMPLOYER_USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID),
    CONSTRAINT CK_JOB_POST_STATUS_CD CHECK (STATUS_CD IN ('ACTIVE', 'PAUSED', 'CLOSED'))
);
GO

CREATE TABLE JOB_SKILL (
    JOB_SKILL_ID                   INT IDENTITY(1,1)      NOT NULL,
    JOB_POST_ID                      INT                     NOT NULL,
    SKILL_ID                         INT                     NOT NULL,

    CREATED_BY                       NVARCHAR(100)           NOT NULL CONSTRAINT DF_JOB_SKILL_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS                       DATETIME2               NOT NULL CONSTRAINT DF_JOB_SKILL_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR                  VARCHAR(45)              NULL,
    UPDATED_BY                        NVARCHAR(100)           NULL,
    UPDATED_TS                        DATETIME2               NULL,
    UPDATED_IP_ADDR                   VARCHAR(45)              NULL,

    CONSTRAINT PK_JOB_SKILL PRIMARY KEY (JOB_SKILL_ID),
    CONSTRAINT FK_JOB_SKILL_JOB_POST FOREIGN KEY (JOB_POST_ID) REFERENCES JOB_POST(JOB_POST_ID) ON DELETE CASCADE,
    CONSTRAINT FK_JOB_SKILL_SKILL FOREIGN KEY (SKILL_ID) REFERENCES SKILL(SKILL_ID),
    CONSTRAINT UQ_JOB_SKILL UNIQUE (JOB_POST_ID, SKILL_ID)
);
GO


/* ============================================================================
   7. JOB_APPLICATION  [FUTURE WEEK — Week 3]
   ============================================================================ */
CREATE TABLE JOB_APPLICATION (
    JOB_APPLICATION_ID             INT IDENTITY(1,1)      NOT NULL,
    JOB_POST_ID                      INT                     NOT NULL,
    EMPLOYEE_USER_ACCOUNT_ID          INT                     NOT NULL,
    STATUS_CD                         VARCHAR(30)             NOT NULL CONSTRAINT DF_JOB_APPLICATION_STATUS_CD DEFAULT ('PENDING'),
    APPLIED_TS                        DATETIME2                NOT NULL CONSTRAINT DF_JOB_APPLICATION_APPLIED_TS DEFAULT (SYSUTCDATETIME()),

    CREATED_BY                        NVARCHAR(100)           NOT NULL CONSTRAINT DF_JOB_APPLICATION_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS                        DATETIME2               NOT NULL CONSTRAINT DF_JOB_APPLICATION_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR                   VARCHAR(45)              NULL,
    UPDATED_BY                         NVARCHAR(100)           NULL,
    UPDATED_TS                         DATETIME2               NULL,
    UPDATED_IP_ADDR                    VARCHAR(45)              NULL,

    CONSTRAINT PK_JOB_APPLICATION PRIMARY KEY (JOB_APPLICATION_ID),
    CONSTRAINT FK_JOB_APPLICATION_JOB_POST FOREIGN KEY (JOB_POST_ID) REFERENCES JOB_POST(JOB_POST_ID),
    CONSTRAINT FK_JOB_APPLICATION_EMPLOYEE FOREIGN KEY (EMPLOYEE_USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID),
    CONSTRAINT UQ_JOB_APPLICATION UNIQUE (JOB_POST_ID, EMPLOYEE_USER_ACCOUNT_ID),
    CONSTRAINT CK_JOB_APPLICATION_STATUS_CD CHECK (STATUS_CD IN ('PENDING','EMPLOYER_VIEWED','EMPLOYER_CONTACTED','OTP_VERIFIED','COMPLETED'))
);
GO


/* ============================================================================
   8. OTP_RECORD  [FUTURE WEEK — Week 3]  (Job-site App OTP, NOT login OTP)
   ============================================================================ */
CREATE TABLE OTP_RECORD (
    OTP_RECORD_ID                   INT IDENTITY(1,1)      NOT NULL,
    EMPLOYEE_USER_ACCOUNT_ID           INT                     NOT NULL,
    JOB_POST_ID                         INT                     NOT NULL,
    OTP_CD                              VARCHAR(6)              NOT NULL,
    GENERATED_TS                        DATETIME2               NOT NULL CONSTRAINT DF_OTP_RECORD_GENERATED_TS DEFAULT (SYSUTCDATETIME()),
    EXPIRES_TS                          DATETIME2               NOT NULL,
    IS_USED_FL                           BIT                     NOT NULL CONSTRAINT DF_OTP_RECORD_USED_FL DEFAULT (0),

    CREATED_BY                           NVARCHAR(100)           NOT NULL CONSTRAINT DF_OTP_RECORD_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS                           DATETIME2               NOT NULL CONSTRAINT DF_OTP_RECORD_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR                      VARCHAR(45)              NULL,
    UPDATED_BY                            NVARCHAR(100)           NULL,
    UPDATED_TS                            DATETIME2               NULL,
    UPDATED_IP_ADDR                       VARCHAR(45)              NULL,

    CONSTRAINT PK_OTP_RECORD PRIMARY KEY (OTP_RECORD_ID),
    CONSTRAINT FK_OTP_RECORD_EMPLOYEE FOREIGN KEY (EMPLOYEE_USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID),
    CONSTRAINT FK_OTP_RECORD_JOB_POST FOREIGN KEY (JOB_POST_ID) REFERENCES JOB_POST(JOB_POST_ID)
);
GO


/* ============================================================================
   9. NOTIFICATION_LOG  [FUTURE WEEK — Week 3]
   ============================================================================ */
CREATE TABLE NOTIFICATION_LOG (
    NOTIFICATION_LOG_ID                INT IDENTITY(1,1)      NOT NULL,
    EMPLOYEE_USER_ACCOUNT_ID              INT                     NULL,
    EMPLOYER_USER_ACCOUNT_ID              INT                     NULL,
    JOB_POST_ID                            INT                     NULL,
    CHANNEL_CD                             VARCHAR(20)             NOT NULL,   -- 'WHATSAPP' | 'SMS'
    MESSAGE_TXT                            NVARCHAR(MAX)           NULL,
    SENT_TS                                DATETIME2               NOT NULL CONSTRAINT DF_NOTIFICATION_LOG_SENT_TS DEFAULT (SYSUTCDATETIME()),
    STATUS_CD                              VARCHAR(20)             NOT NULL,   -- 'SENT' | 'FAILED'

    CREATED_BY                             NVARCHAR(100)           NOT NULL CONSTRAINT DF_NOTIFICATION_LOG_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS                             DATETIME2               NOT NULL CONSTRAINT DF_NOTIFICATION_LOG_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR                        VARCHAR(45)              NULL,
    UPDATED_BY                              NVARCHAR(100)           NULL,
    UPDATED_TS                              DATETIME2               NULL,
    UPDATED_IP_ADDR                         VARCHAR(45)              NULL,

    CONSTRAINT PK_NOTIFICATION_LOG PRIMARY KEY (NOTIFICATION_LOG_ID),
    CONSTRAINT FK_NOTIFICATION_LOG_EMPLOYEE FOREIGN KEY (EMPLOYEE_USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID),
    CONSTRAINT FK_NOTIFICATION_LOG_EMPLOYER FOREIGN KEY (EMPLOYER_USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID),
    CONSTRAINT FK_NOTIFICATION_LOG_JOB_POST FOREIGN KEY (JOB_POST_ID) REFERENCES JOB_POST(JOB_POST_ID)
);
GO


/* ============================================================================
   10. MOBILE_VERIFICATION_OTP  [WEEK 1] — used at Registration & Forgot Password
   Separate from OTP_RECORD (which is for job-site identity verification).
   ============================================================================ */
CREATE TABLE MOBILE_VERIFICATION_OTP (
    MOBILE_VERIFICATION_OTP_ID        INT IDENTITY(1,1)      NOT NULL,
    CONTACT_NBR                         VARCHAR(15)             NOT NULL,
    OTP_CD                              VARCHAR(6)              NOT NULL,
    PURPOSE_CD                          VARCHAR(30)             NOT NULL,   -- 'REGISTRATION' | 'FORGOT_PASSWORD'
    GENERATED_TS                        DATETIME2               NOT NULL CONSTRAINT DF_MOBILE_VERIFICATION_OTP_GENERATED_TS DEFAULT (SYSUTCDATETIME()),
    EXPIRES_TS                          DATETIME2               NOT NULL,
    IS_USED_FL                           BIT                     NOT NULL CONSTRAINT DF_MOBILE_VERIFICATION_OTP_USED_FL DEFAULT (0),

    CREATED_BY                           NVARCHAR(100)           NOT NULL CONSTRAINT DF_MOBILE_VERIFICATION_OTP_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS                           DATETIME2               NOT NULL CONSTRAINT DF_MOBILE_VERIFICATION_OTP_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR                      VARCHAR(45)              NULL,
    UPDATED_BY                            NVARCHAR(100)           NULL,
    UPDATED_TS                            DATETIME2               NULL,
    UPDATED_IP_ADDR                       VARCHAR(45)              NULL,

    CONSTRAINT PK_MOBILE_VERIFICATION_OTP PRIMARY KEY (MOBILE_VERIFICATION_OTP_ID),
    CONSTRAINT CK_MOBILE_VERIFICATION_OTP_PURPOSE_CD CHECK (PURPOSE_CD IN ('REGISTRATION', 'FORGOT_PASSWORD'))
);
GO

CREATE INDEX IX_MOBILE_VERIFICATION_OTP_CONTACT_NBR ON MOBILE_VERIFICATION_OTP(CONTACT_NBR);
GO


/* ============================================================================
   SEED DATA — Default ADMIN user + sample skills
   NOTE: Replace PASSWORD_HASH_TXT below with a real hash generated by the
   application (e.g. via ASP.NET Core Identity PasswordHasher / BCrypt) before
   go-live. The value here is a PLACEHOLDER and will NOT work for actual login.
   ============================================================================ */
INSERT INTO USER_ACCOUNT (FIRST_NAME, LAST_NAME, CONTACT_NBR, EMAIL_ID, PASSWORD_HASH_TXT, ROLE_CD, IS_MOBILE_VERIFIED_FL, IS_ACTIVE_FL, IS_APPROVED_FL, CREATED_BY)
VALUES ('System', 'Admin', '9999999999', 'admin@manpowerplatform.com', 'REPLACE_WITH_REAL_HASH', 'ADMIN', 1, 1, 1, 'SYSTEM_SEED');
GO

INSERT INTO SKILL (SKILL_NAME, CATEGORY_NAME, CREATED_BY) VALUES
('Electrician', 'Technical', 'SYSTEM_SEED'),
('Plumber', 'Technical', 'SYSTEM_SEED'),
('Carpenter', 'Technical', 'SYSTEM_SEED'),
('Helper / Labourer', 'General', 'SYSTEM_SEED'),
('Driver', 'General', 'SYSTEM_SEED'),
('Security Guard', 'General', 'SYSTEM_SEED'),
('Mason', 'Construction', 'SYSTEM_SEED'),
('Painter', 'Construction', 'SYSTEM_SEED'),
('Cook / Kitchen Helper', 'Hospitality', 'SYSTEM_SEED'),
('Housekeeping Staff', 'Hospitality', 'SYSTEM_SEED');
GO

PRINT 'MANPOWER_HIRING_DB schema created successfully with enterprise naming convention.';