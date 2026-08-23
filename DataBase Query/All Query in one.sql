/* ============================================================================
   COMBINED DATABASE INITIALIZATION & MIGRATION SCRIPT
   Database: SQL Server
   Target:   MANPOWER_HIRING_DB
   Order:    01 to 15 (Sequential execution)
   ============================================================================ */

/* ============================================================================
   01_Database_Schema.sql
   ============================================================================ */
CREATE DATABASE KAARIGAR_DB;
GO

USE KAARIGAR_DB;
GO

-- 1. USER_ACCOUNT
CREATE TABLE USER_ACCOUNT (
    USER_ACCOUNT_ID       INT IDENTITY(1,1)      NOT NULL,
    FIRST_NAME             NVARCHAR(75)            NOT NULL,
    LAST_NAME              NVARCHAR(75)            NULL,
    CONTACT_NBR            VARCHAR(15)             NOT NULL,
    EMAIL_ID               NVARCHAR(150)           NULL,
    PASSWORD_HASH_TXT      NVARCHAR(255)           NOT NULL,
    ROLE_CD                VARCHAR(20)             NOT NULL,
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

-- 2. EMPLOYER_PROFILE
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

-- 3. EMPLOYEE_PROFILE
CREATE TABLE EMPLOYEE_PROFILE (
    EMPLOYEE_PROFILE_ID     INT IDENTITY(1,1)      NOT NULL,
    USER_ACCOUNT_ID          INT                     NOT NULL,
    CITY_NAME                NVARCHAR(100)           NULL,
    AREA_ADDRESS_TXT         NVARCHAR(300)           NULL,
    LATITUDE_NBR             DECIMAL(9,6)            NULL,
    LONGITUDE_NBR            DECIMAL(9,6)            NULL,
    PREFERRED_RADIUS_NBR     INT                      NULL,

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

-- 4. EMPLOYEE_DOCUMENT
CREATE TABLE EMPLOYEE_DOCUMENT (
    EMPLOYEE_DOCUMENT_ID         INT IDENTITY(1,1)      NOT NULL,
    USER_ACCOUNT_ID               INT                     NOT NULL,
    DOCUMENT_TYPE_CD               VARCHAR(30)             NOT NULL,
    DOCUMENT_SUBTYPE_CD            VARCHAR(30)              NULL,
    ID_LAST_FOUR_DIGIT_TXT         VARCHAR(4)               NULL,
    ORIGINAL_FILE_NAME_TXT         NVARCHAR(255)           NOT NULL,
    STORED_FILE_NAME_TXT           NVARCHAR(255)           NOT NULL,
    SERVER_FILE_PATH_TXT           NVARCHAR(500)           NOT NULL,
    FILE_SIZE_KB_NBR                INT                      NULL,
    MIME_TYPE_TXT                   VARCHAR(100)             NULL,
    UPLOADED_TS                     DATETIME2                NOT NULL CONSTRAINT DF_EMPLOYEE_DOCUMENT_UPLOADED_TS DEFAULT (SYSUTCDATETIME()),

    REVIEW_STATUS_CD                VARCHAR(20)              NOT NULL CONSTRAINT DF_EMPLOYEE_DOCUMENT_REVIEW_STATUS_CD DEFAULT ('PENDING'),
    REVIEWED_BY_USER_ACCOUNT_ID     INT                      NULL,
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

-- 5. SKILL & EMPLOYEE_SKILL
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
    USER_ACCOUNT_ID             INT                     NOT NULL,
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

-- 6. JOB_POST & JOB_SKILL
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

-- 7. JOB_APPLICATION
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

-- 8. OTP_RECORD
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

-- 9. NOTIFICATION_LOG
CREATE TABLE NOTIFICATION_LOG (
    NOTIFICATION_LOG_ID                INT IDENTITY(1,1)      NOT NULL,
    EMPLOYEE_USER_ACCOUNT_ID              INT                     NULL,
    EMPLOYER_USER_ACCOUNT_ID              INT                     NULL,
    JOB_POST_ID                            INT                     NULL,
    CHANNEL_CD                             VARCHAR(20)             NOT NULL,
    MESSAGE_TXT                            NVARCHAR(MAX)           NULL,
    SENT_TS                                DATETIME2               NOT NULL CONSTRAINT DF_NOTIFICATION_LOG_SENT_TS DEFAULT (SYSUTCDATETIME()),
    STATUS_CD                              VARCHAR(20)             NOT NULL,

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

-- 10. MOBILE_VERIFICATION_OTP
CREATE TABLE MOBILE_VERIFICATION_OTP (
    MOBILE_VERIFICATION_OTP_ID        INT IDENTITY(1,1)      NOT NULL,
    CONTACT_NBR                         VARCHAR(15)             NOT NULL,
    OTP_CD                              VARCHAR(6)              NOT NULL,
    PURPOSE_CD                          VARCHAR(30)             NOT NULL,
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

-- 01 Seed Data
l 

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

/* ============================================================================
   02_EmployerProfile_Schema_Additions.sql
   ============================================================================ */
CREATE TABLE BUSINESS_CATEGORY (
    BUSINESS_CATEGORY_ID   INT IDENTITY(1,1)      NOT NULL,
    CATEGORY_NAME           NVARCHAR(150)           NOT NULL,
    IS_ACTIVE_FL             BIT                     NOT NULL CONSTRAINT DF_BUSINESS_CATEGORY_ACTIVE_FL DEFAULT (1),

    CREATED_BY               NVARCHAR(100)           NOT NULL CONSTRAINT DF_BUSINESS_CATEGORY_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS               DATETIME2               NOT NULL CONSTRAINT DF_BUSINESS_CATEGORY_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    CREATED_IP_ADDR          VARCHAR(45)              NULL,
    UPDATED_BY                NVARCHAR(100)           NULL,
    UPDATED_TS                DATETIME2               NULL,
    UPDATED_IP_ADDR           VARCHAR(45)              NULL,

    CONSTRAINT PK_BUSINESS_CATEGORY PRIMARY KEY (BUSINESS_CATEGORY_ID),
    CONSTRAINT UQ_BUSINESS_CATEGORY_NAME UNIQUE (CATEGORY_NAME)
);
GO

INSERT INTO BUSINESS_CATEGORY (CATEGORY_NAME, CREATED_BY) VALUES
('Restaurants', 'SYSTEM_SEED'),
('Hotels', 'SYSTEM_SEED'),
('Manufacturing', 'SYSTEM_SEED'),
('Pan India Retail / Malls', 'SYSTEM_SEED'),
('Events', 'SYSTEM_SEED'),
('Construction', 'SYSTEM_SEED'),
('Financial Services', 'SYSTEM_SEED'),
('Warehouses', 'SYSTEM_SEED'),
('Facility Management', 'SYSTEM_SEED'),
('Maintenance Services', 'SYSTEM_SEED'),
('Worker Insurance', 'SYSTEM_SEED'),
('Hospitals', 'SYSTEM_SEED'),
('Commercial Offices', 'SYSTEM_SEED'),
('Training Marketplace', 'SYSTEM_SEED');
GO

ALTER TABLE EMPLOYER_PROFILE ADD
    CONTACT_PERSON_NAME                  NVARCHAR(150)   NULL,
    BUSINESS_CATEGORY_ID                  INT             NULL,
    BUSINESS_PROOF_TYPE_CD                 VARCHAR(20)     NULL,
    BUSINESS_PROOF_NUMBER_TXT              VARCHAR(30)     NULL,
    BUSINESS_PROOF_ORIGINAL_FILE_NAME_TXT  NVARCHAR(255)   NULL,
    BUSINESS_PROOF_FILE_PATH_TXT           NVARCHAR(500)   NULL,
    BUSINESS_PROOF_REVIEW_STATUS_CD        VARCHAR(20)     NOT NULL
        CONSTRAINT DF_EMPLOYER_PROFILE_PROOF_REVIEW_STATUS_CD DEFAULT ('PENDING');
GO

ALTER TABLE EMPLOYER_PROFILE ADD CONSTRAINT FK_EMPLOYER_PROFILE_BUSINESS_CATEGORY
    FOREIGN KEY (BUSINESS_CATEGORY_ID) REFERENCES BUSINESS_CATEGORY(BUSINESS_CATEGORY_ID);
GO

ALTER TABLE EMPLOYER_PROFILE ADD CONSTRAINT CK_EMPLOYER_PROFILE_PROOF_TYPE_CD
    CHECK (BUSINESS_PROOF_TYPE_CD IS NULL OR BUSINESS_PROOF_TYPE_CD IN ('GST', 'GUMASTHA'));
GO

ALTER TABLE EMPLOYER_PROFILE ADD CONSTRAINT CK_EMPLOYER_PROFILE_PROOF_REVIEW_STATUS_CD
    CHECK (BUSINESS_PROOF_REVIEW_STATUS_CD IN ('PENDING', 'APPROVED', 'REJECTED'));
GO

CREATE INDEX IX_EMPLOYER_PROFILE_BUSINESS_CATEGORY_ID ON EMPLOYER_PROFILE(BUSINESS_CATEGORY_ID);
GO

/* ============================================================================
   03_JobPost_Schema_Additions.sql
   ============================================================================ */
ALTER TABLE JOB_POST ADD
    JOB_CATEGORY_CD   VARCHAR(50)   NOT NULL
        CONSTRAINT DF_JOB_POST_JOB_CATEGORY_CD DEFAULT ('OTHER');
GO

ALTER TABLE JOB_POST ADD CONSTRAINT CK_JOB_POST_JOB_CATEGORY_CD
    CHECK (JOB_CATEGORY_CD IN (
        'HOUSEKEEPING', 'SECURITY_GUARD', 'DELIVERY_LOGISTICS', 'CONSTRUCTION_LABOUR',
        'KITCHEN_STAFF', 'LOADER_UNLOADER', 'DRIVER', 'WAREHOUSE_STAFF',
        'OFFICE_BOY_PEON', 'ELECTRICIAN', 'PLUMBER', 'CARPENTER', 'PAINTER', 'OTHER'
    ));
GO

CREATE INDEX IX_JOB_POST_JOB_CATEGORY_CD ON JOB_POST(JOB_CATEGORY_CD);
GO

/* ============================================================================
   04_Additional_Skills_Seed.sql
   ============================================================================ */
INSERT INTO SKILL (SKILL_NAME, CATEGORY_NAME, CREATED_BY)
SELECT v.SKILL_NAME, v.CATEGORY_NAME, 'SYSTEM_SEED_W03'
FROM (VALUES
    ('Picker',                 'Warehouse & Logistics'),
    ('Packer',                 'Warehouse & Logistics'),
    ('Delivery Boy',           'Warehouse & Logistics'),
    ('Warehouse Associate',    'Warehouse & Logistics'),
    ('Barcode Scanner Staff',  'Warehouse & Logistics'),
    ('Sorting Staff',          'Warehouse & Logistics'),
    ('Packing Labour',         'Warehouse & Logistics'),
    ('Labeling Staff',         'Warehouse & Logistics'),
    ('Quality Checker',        'Warehouse & Logistics'),
    ('Delivery Rider',         'Warehouse & Logistics'),
    ('Store Helper',           'Retail'),
    ('Billing Staff',          'Retail'),
    ('Promoter',               'Retail'),
    ('Steward/Waiter',         'Hospitality'),
    ('Kitchen Helper',         'Hospitality'),
    ('Cleaner',                'Hospitality'),
    ('Banquet Staff',          'Hospitality')
) AS v(SKILL_NAME, CATEGORY_NAME)
WHERE NOT EXISTS (
    SELECT 1 FROM SKILL s WHERE s.SKILL_NAME = v.SKILL_NAME
);
GO

/* ============================================================================
   05_Employee_Document_Table.sql
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

/* ============================================================================
   06_EmployeeProfile_Schema_Additions.sql
   ============================================================================ */
ALTER TABLE EMPLOYEE_PROFILE ADD
    IS_NOTIFICATION_ENABLED_FL   BIT   NOT NULL
        CONSTRAINT DF_EMPLOYEE_PROFILE_IS_NOTIFICATION_ENABLED_FL DEFAULT (1);
GO

/* ============================================================================
   07_JobPost_Timing_Additions.sql
   ============================================================================ */
ALTER TABLE JOB_POST ADD
    START_TIME   TIME(0)   NULL,
    END_TIME     TIME(0)   NULL;
GO

/* ============================================================================
   08_City_Area_Schema_And_Seed.sql
   ============================================================================ */
CREATE TABLE CITY (
    CITY_ID         INT IDENTITY(1,1) PRIMARY KEY,
    CITY_NAME       NVARCHAR(100)   NOT NULL,
    STATE_NAME      NVARCHAR(100)   NULL,
    IS_ACTIVE_FL    BIT             NOT NULL DEFAULT 1,
    CREATED_BY      NVARCHAR(50)    NULL,
    CREATED_TS      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UPDATED_BY      NVARCHAR(50)    NULL,
    UPDATED_TS      DATETIME2       NULL,
    CONSTRAINT UQ_CITY_NAME UNIQUE (CITY_NAME)
);
GO

CREATE TABLE AREA (
    AREA_ID         INT IDENTITY(1,1) PRIMARY KEY,
    CITY_ID         INT             NOT NULL,
    AREA_NAME       NVARCHAR(150)   NOT NULL,
    PINCODE_TXT     NVARCHAR(10)    NULL,
    IS_ACTIVE_FL    BIT             NOT NULL DEFAULT 1,
    CREATED_BY      NVARCHAR(50)    NULL,
    CREATED_TS      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UPDATED_BY      NVARCHAR(50)    NULL,
    UPDATED_TS      DATETIME2       NULL,
    CONSTRAINT FK_AREA_CITY FOREIGN KEY (CITY_ID) REFERENCES CITY(CITY_ID),
    CONSTRAINT UQ_AREA_CITY_NAME UNIQUE (CITY_ID, AREA_NAME)
);
GO

CREATE INDEX IX_AREA_CITY_ID ON AREA(CITY_ID);
GO

INSERT INTO CITY (CITY_NAME, STATE_NAME, CREATED_BY) VALUES
    ('Vadodara', 'Gujarat', 'SEED_SCRIPT'),
    ('Ahmedabad', 'Gujarat', 'SEED_SCRIPT');
GO

INSERT INTO AREA (CITY_ID, AREA_NAME, PINCODE_TXT, CREATED_BY)
SELECT c.CITY_ID, v.AREA_NAME, v.PINCODE_TXT, 'SEED_SCRIPT'
FROM (VALUES
    ('Vadodara', 'Abhol', '391421'),
    ('Vadodara', 'Achisara', '391111'),
    ('Vadodara', 'Adiran', '391510'),
    ('Vadodara', 'Agar', '391150'),
    ('Vadodara', 'Ajod', '391740'),
    ('Vadodara', 'Ajwa Compound', '391510'),
    ('Vadodara', 'Ajwa Road', '390019'),
    ('Vadodara', 'Akakheda', '391145'),
    ('Vadodara', 'Akona', '391150'),
    ('Vadodara', 'Akota', '390020'),
    ('Vadodara', 'Alhadpura', '391135'),
    ('Vadodara', 'Alkapuri', '390007'),
    ('Vadodara', 'Alva', '391760'),
    ('Vadodara', 'Amaliyara', '390022'),
    ('Vadodara', 'Ambada', '391445'),
    ('Vadodara', 'Ambadungar', '391175'),
    ('Vadodara', 'Ambala', '391165'),
    ('Vadodara', 'Ambali', '391760'),
    ('Vadodara', 'Ambapura', '391145'),
    ('Vadodara', 'Amla', '391445'),
    ('Vadodara', 'Amodar', '390019'),
    ('Vadodara', 'Ampad', '391101'),
    ('Vadodara', 'Amreshwar', '391761'),
    ('Vadodara', 'Amroli', '391150'),
    ('Vadodara', 'Anandi', '391111'),
    ('Vadodara', 'Anandpura', '391761'),
    ('Vadodara', 'Anastu', '391240'),
    ('Vadodara', 'Angadh', '391330'),
    ('Vadodara', 'Anguthan', '391107'),
    ('Vadodara', 'Aniyadri', '391160'),
    ('Vadodara', 'Anjesar', '391775'),
    ('Vadodara', 'Ankhol', '390019'),
    ('Vadodara', 'Ankodia', '391330'),
    ('Vadodara', 'Anti', '391445'),
    ('Vadodara', 'Antoli', '391761'),
    ('Vadodara', 'Asar', '391168'),
    ('Vadodara', 'Asoj', '391510'),
    ('Vadodara', 'Atali', '391244'),
    ('Vadodara', 'Athadungri', '391170'),
    ('Vadodara', 'Athavali', '391140'),
    ('Vadodara', 'Atladara', '390012'),
    ('Vadodara', 'Bachar', '391244'),
    ('Vadodara', 'Bahadarpur', '391125'),
    ('Vadodara', 'Bahutha', '391775'),
    ('Vadodara', 'Bajwa', '391310'),
    ('Vadodara', 'Bakrol', '390019'),
    ('Vadodara', 'Baman Gam', '391210'),
    ('Vadodara', 'Bamroli', '391125'),
    ('Vadodara', 'Bangapura', '391145'),
    ('Vadodara', 'Bar', '391160'),
    ('Vadodara', 'Baranpura', '390001'),
    ('Vadodara', 'Baravad', '391160'),
    ('Vadodara', 'Barkal', '391105'),
    ('Vadodara', 'Baroli', '391150'),
    ('Vadodara', 'Bedia', '391165'),
    ('Vadodara', 'Bhadara', '391445'),
    ('Vadodara', 'Bhadarwa', '391780'),
    ('Vadodara', 'Bhaily', '391410'),
    ('Vadodara', 'Bhaka', '391152'),
    ('Vadodara', 'Bhalod', '393105'),
    ('Vadodara', 'Bhaniara', '391510'),
    ('Vadodara', 'Bhanpur', '391160'),
    ('Vadodara', 'Bharthana', '391244'),
    ('Vadodara', 'Bharvada', '391150'),
    ('Vadodara', 'Bhatpur', '391130'),
    ('Vadodara', 'Bhensavali', '391160'),
    ('Vadodara', 'Bhikhapur', '391160'),
    ('Vadodara', 'Bhilapur', '391107'),
    ('Vadodara', 'Bhilodia', '391110'),
    ('Vadodara', 'Bhilpur', '391160'),
    ('Vadodara', 'Bhindol', '391135'),
    ('Vadodara', 'Bhoj', '391445'),
    ('Vadodara', 'Bhorda', '391165'),
    ('Vadodara', 'Bhumaswada', '391165'),
    ('Vadodara', 'Bill', '391410'),
    ('Vadodara', 'Bithli', '391105'),
    ('Vadodara', 'Bodeli', '391135'),
    ('Vadodara', 'Bodka', '391240'),
    ('Vadodara', 'Borbar', '391107'),
    ('Vadodara', 'Bordha', '391135'),
    ('Vadodara', 'Bortalav', '391145'),
    ('Vadodara', 'Brahamanvashi', '391421'),
    ('Vadodara', 'Chalamali', '391140'),
    ('Vadodara', 'Chametha', '391150'),
    ('Vadodara', 'Champaner Road', '391510'),
    ('Vadodara', 'Chandod', '391105'),
    ('Vadodara', 'Chandranagar', '391520'),
    ('Vadodara', 'Chansad', '391440'),
    ('Vadodara', 'Chanvada', '391110'),
    ('Vadodara', 'Chapad', '391410'),
    ('Vadodara', 'Chemical Industries', '390003'),
    ('Vadodara', 'Chhaliar', '391530'),
    ('Vadodara', 'Chhanbhoi', '391111'),
    ('Vadodara', 'Chhanchava', '391244'),
    ('Vadodara', 'Chhani', '391740'),
    ('Vadodara', 'Chhani Rd', '390002'),
    ('Vadodara', 'Chhatral', '391210'),
    ('Vadodara', 'Chhatrali', '391135'),
    ('Vadodara', 'Chhota-udepur', '391165'),
    ('Vadodara', 'Chhuchhapura', '391125'),
    ('Vadodara', 'Chikhodara', '390009'),
    ('Vadodara', 'Chipad', '391760'),
    ('Vadodara', 'Chisadiya', '391165'),
    ('Vadodara', 'Chitral', '391430'),
    ('Vadodara', 'Chokari', '391450'),
    ('Vadodara', 'Choranda', '391244'),
    ('Vadodara', 'Chorbhuj', '391240'),
    ('Vadodara', 'Chorngala', '391130'),
    ('Vadodara', 'Chudel', '391160'),
    ('Vadodara', 'Dabhasa', '391440'),
    ('Vadodara', 'Dabhoi', '391110'),
    ('Vadodara', 'Dabhoi R.s.', '391110'),
    ('Vadodara', 'Dabhoi Vadodari bhagol', '391110'),
    ('Vadodara', 'Dabka', '391440'),
    ('Vadodara', 'Dajipura', '391774'),
    ('Vadodara', 'Dani', '391152'),
    ('Vadodara', 'Danteshwar', '390004'),
    ('Vadodara', 'Darapura', '391440'),
    ('Vadodara', 'Dashrath', '391740'),
    ('Vadodara', 'Dena', '390022'),
    ('Vadodara', 'Deroli', '391130'),
    ('Vadodara', 'Desar', '391774'),
    ('Vadodara', 'Devalia', '391160'),
    ('Vadodara', 'Dhamasiya', '391150'),
    ('Vadodara', 'Dhandhoda', '391165'),
    ('Vadodara', 'Dhaniavi', '390014'),
    ('Vadodara', 'Dhanora', '391510'),
    ('Vadodara', 'Dhantej', '391530'),
    ('Vadodara', 'Dharmpuri', '391110'),
    ('Vadodara', 'Dhavat', '391210'),
    ('Vadodara', 'Dholar', '391107'),
    ('Vadodara', 'Dholi Simal', '391165'),
    ('Vadodara', 'Dodka', '391340'),
    ('Vadodara', 'Dolaria', '391165'),
    ('Vadodara', 'Dormar', '391125'),
    ('Vadodara', 'Dudhwada', '391450'),
    ('Vadodara', 'Dugdha', '391150'),
    ('Vadodara', 'Dumad', '391740'),
    ('Vadodara', 'Dumali', '391160'),
    ('Vadodara', 'Dungargam', '391165'),
    ('Vadodara', 'Dungarvant', '391160'),
    ('Vadodara', 'Ekalbara', '391165'),
    ('Vadodara', 'Ekelbara', '391440'),
    ('Vadodara', 'Eme', '390008'),
    ('Vadodara', 'Falod', '391760'),
    ('Vadodara', 'Fanta', '391125'),
    ('Vadodara', 'Fartikui', '391107'),
    ('Vadodara', 'Fartilizer Nagar', '391750'),
    ('Vadodara', 'Fateganj', '390002'),
    ('Vadodara', 'Fatepura', '390006'),
    ('Vadodara', 'Fazalpur', '391340'),
    ('Vadodara', 'Ferkuva', '391135'),
    ('Vadodara', 'Gabhirpura', '391761'),
    ('Vadodara', 'Gadhboriad', '391152'),
    ('Vadodara', 'Gametha', '391430'),
    ('Vadodara', 'Gandhara', '391210'),
    ('Vadodara', 'Ganeshpura', '391510'),
    ('Vadodara', 'Gangadia', '391520'),
    ('Vadodara', 'Ganpatpura', '391760'),
    ('Vadodara', 'Garda', '391145'),
    ('Vadodara', 'Gardhia', '391520'),
    ('Vadodara', 'Gavasad', '391430'),
    ('Vadodara', 'Geb', '390007'),
    ('Vadodara', 'Ghantiyal', '391510'),
    ('Vadodara', 'Ghantoli', '391150'),
    ('Vadodara', 'Ghayaj', '391440'),
    ('Vadodara', 'Ghelawant', '391165'),
    ('Vadodara', 'Ghodadara', '391760'),
    ('Vadodara', 'Gola Gamdi', '391125'),
    ('Vadodara', 'Goraj', '391761'),
    ('Vadodara', 'Goriyad', '391440'),
    ('Vadodara', 'Gorwa', '390003'),
    ('Vadodara', 'Gosindra', '391240'),
    ('Vadodara', 'Gothda', '391770'),
    ('Vadodara', 'Gotri', '390021'),
    ('Vadodara', 'Goyawant', '391150'),
    ('Vadodara', 'Gundicha', '391145'),
    ('Vadodara', 'Gutal', '391760'),
    ('Vadodara', 'Handod', '391145'),
    ('Vadodara', 'Harakhapur', '391160'),
    ('Vadodara', 'Hareshwar', '391125'),
    ('Vadodara', 'Haripura', '391150'),
    ('Vadodara', 'Harni', '390022'),
    ('Vadodara', 'Harni Colony', '390022'),
    ('Vadodara', 'Himmatpura', '391774'),
    ('Vadodara', 'Husepura', '391445'),
    ('Vadodara', 'Indrad', '391510'),
    ('Vadodara', 'Indral', '391130'),
    ('Vadodara', 'Industrial Estate', '390016'),
    ('Vadodara', 'Itola', '391240'),
    ('Vadodara', 'Itola R.s.', '391240'),
    ('Vadodara', 'Itwad', '391774'),
    ('Vadodara', 'Jabugam', '391155'),
    ('Vadodara', 'Jakshi', '391150'),
    ('Vadodara', 'Jalalpura', '391430'),
    ('Vadodara', 'Jaloda', '391160'),
    ('Vadodara', 'Jamba', '391160'),
    ('Vadodara', 'Jambugoral', '391774'),
    ('Vadodara', 'Jambuva', '390014'),
    ('Vadodara', 'Jamli', '391152'),
    ('Vadodara', 'Jarod', '391510'),
    ('Vadodara', 'Jaspura', '391440'),
    ('Vadodara', 'Jawahar Nagar', '391320'),
    ('Vadodara', 'Jemalgadh', '391150'),
    ('Vadodara', 'Jiyatalavdi', '391220'),
    ('Vadodara', 'Jojwa r.s.', '391125'),
    ('Vadodara', 'Juna Samlaya', '391520'),
    ('Vadodara', 'Junarampura', '391760'),
    ('Vadodara', 'Juni Jathardi', '391240'),
    ('Vadodara', 'Kadachhala', '391140'),
    ('Vadodara', 'Kadadhara', '391107'),
    ('Vadodara', 'Kadchhala', '391774'),
    ('Vadodara', 'Kadipani', '391175'),
    ('Vadodara', 'Kadvad', '391160'),
    ('Vadodara', 'Kahndia', '391761'),
    ('Vadodara', 'Kalali', '390012'),
    ('Vadodara', 'Kalarani', '391135'),
    ('Vadodara', 'Kaledia', '391150'),
    ('Vadodara', 'Kalidoli', '391140'),
    ('Vadodara', 'Kaliyapura', '391150'),
    ('Vadodara', 'Kamalpura', '391520'),
    ('Vadodara', 'Kamrol', '391510'),
    ('Vadodara', 'Kanalva', '391168'),
    ('Vadodara', 'Kanbha', '391240'),
    ('Vadodara', 'Kanbola', '391240'),
    ('Vadodara', 'Kanda', '391160'),
    ('Vadodara', 'Kandari', '391210'),
    ('Vadodara', 'Kanteshwar', '391125'),
    ('Vadodara', 'Kapurai', '390004'),
    ('Vadodara', 'Karali', '391135'),
    ('Vadodara', 'Karalipura', '391110'),
    ('Vadodara', 'Karamdi', '391240'),
    ('Vadodara', 'Karanjwant', '391165'),
    ('Vadodara', 'Karchia', '391520'),
    ('Vadodara', 'Karchiya', '391310'),
    ('Vadodara', 'Karelibaug', '390018'),
    ('Vadodara', 'Karkhadi', '391450'),
    ('Vadodara', 'Karmal', '391210'),
    ('Vadodara', 'Karmaliyapura', '391761'),
    ('Vadodara', 'Karnali', '391105'),
    ('Vadodara', 'Karnet', '391110'),
    ('Vadodara', 'Karodia', '391310'),
    ('Vadodara', 'Kasumbia', '391761'),
    ('Vadodara', 'Kavitha', '391125'),
    ('Vadodara', 'Kawant', '391170'),
    ('Vadodara', 'Kayavahron', '391220'),
    ('Vadodara', 'Kelanpur', '390004'),
    ('Vadodara', 'Khadkhad', '391165'),
    ('Vadodara', 'Khakharia', '391510'),
    ('Vadodara', 'Khanda', '391760'),
    ('Vadodara', 'Khanderao Market', '390001'),
    ('Vadodara', 'Khapariya', '391150'),
    ('Vadodara', 'Kharmada', '391152'),
    ('Vadodara', 'Khatiawant', '391135'),
    ('Vadodara', 'Kheda Karmasiya', '391510'),
    ('Vadodara', 'Khervadi', '391761'),
    ('Vadodara', 'Khunvad', '391130'),
    ('Vadodara', 'Kikavada', '391160'),
    ('Vadodara', 'Kiya', '392310'),
    ('Vadodara', 'Kohivav', '391160'),
    ('Vadodara', 'Kolamba', '391152'),
    ('Vadodara', 'Kosindra', '391140'),
    ('Vadodara', 'Kotambi', '391510'),
    ('Vadodara', 'Kotna', '391330'),
    ('Vadodara', 'Koyali', '391330'),
    ('Vadodara', 'Kukad', '391110'),
    ('Vadodara', 'Kukarda', '391150'),
    ('Vadodara', 'Kumetha', '390019'),
    ('Vadodara', 'Kundal', '391165'),
    ('Vadodara', 'Kundhela', '391107'),
    ('Vadodara', 'Kurai', '391240'),
    ('Vadodara', 'Kural', '391430'),
    ('Vadodara', 'Kurali', '391240'),
    ('Vadodara', 'Kuvarpura', '391761'),
    ('Vadodara', 'Kuvarvada', '391760'),
    ('Vadodara', 'Lachharas', '391130'),
    ('Vadodara', 'Ladod', '391135'),
    ('Vadodara', 'Lakodara', '391244'),
    ('Vadodara', 'Lamdapura', '391775'),
    ('Vadodara', 'Lanchanpura', '391770'),
    ('Vadodara', 'Latipura', '391440'),
    ('Vadodara', 'Latwa', '391530'),
    ('Vadodara', 'Lavakui', '391150'),
    ('Vadodara', 'Lilora', '391510'),
    ('Vadodara', 'Limda', '391760'),
    ('Vadodara', 'Limdi', '391530'),
    ('Vadodara', 'Lingsthali', '391220'),
    ('Vadodara', 'Lotia', '391125'),
    ('Vadodara', 'Luna', '391440'),
    ('Vadodara', 'M.I. estate', '390010'),
    ('Vadodara', 'Madanzampa', '390001'),
    ('Vadodara', 'Madheli', '391760'),
    ('Vadodara', 'Mahmadpura', '391440'),
    ('Vadodara', 'Mahuvad', '391440'),
    ('Vadodara', 'Majatan', '391450'),
    ('Vadodara', 'Makarpura', '390014'),
    ('Vadodara', 'Makni', '391145'),
    ('Vadodara', 'Malpur', '391145'),
    ('Vadodara', 'Malu', '391761'),
    ('Vadodara', 'Manavant', '391168'),
    ('Vadodara', 'Mandala', '391210'),
    ('Vadodara', 'Mandlava', '391165'),
    ('Vadodara', 'Mandva', '391105'),
    ('Vadodara', 'Mandvi', '390001'),
    ('Vadodara', 'Maneja', '390013'),
    ('Vadodara', 'Manglej', '391210'),
    ('Vadodara', 'Manjalpur', '390011'),
    ('Vadodara', 'Manjrol', '391110'),
    ('Vadodara', 'Manjusar', '391775'),
    ('Vadodara', 'Manka', '391170'),
    ('Vadodara', 'Mankodi', '391170'),
    ('Vadodara', 'Manpur', '391240'),
    ('Vadodara', 'Masar', '391421'),
    ('Vadodara', 'Masar Road', '391421'),
    ('Vadodara', 'Meghakui', '391107'),
    ('Vadodara', 'Menpura', '391220'),
    ('Vadodara', 'Merakuva', '391774'),
    ('Vadodara', 'Methi', '391240'),
    ('Vadodara', 'Mevli', '391770'),
    ('Vadodara', 'Mithibor', '391165'),
    ('Vadodara', 'Miyagam', '391240'),
    ('Vadodara', 'Miyagam Karjan', '391240'),
    ('Vadodara', 'Mobha', '391430'),
    ('Vadodara', 'Mobha Road', '391430'),
    ('Vadodara', 'Modasar', '391135'),
    ('Vadodara', 'Moghela', '391152'),
    ('Vadodara', 'Mokshi', '391780'),
    ('Vadodara', 'Moletha', '391105'),
    ('Vadodara', 'Moradungari', '391140'),
    ('Vadodara', 'Morakhala', '391125'),
    ('Vadodara', 'Morangana', '391168'),
    ('Vadodara', 'Mota habipura', '391111'),
    ('Vadodara', 'Mota Karala', '391105'),
    ('Vadodara', 'Motaghoda', '391165'),
    ('Vadodara', 'Motavanta', '391170'),
    ('Vadodara', 'Moti amroli', '391160'),
    ('Vadodara', 'Moti beg', '391160'),
    ('Vadodara', 'Moti rasli', '391160'),
    ('Vadodara', 'Motibumadi', '391155'),
    ('Vadodara', 'Motimanekpur', '391761'),
    ('Vadodara', 'Motipura', '391520'),
    ('Vadodara', 'Motivarnoli', '391774'),
    ('Vadodara', 'Mudhela', '391510'),
    ('Vadodara', 'Mujpur', '391440'),
    ('Vadodara', 'Muldhar', '391155'),
    ('Vadodara', 'Mundamore', '391135'),
    ('Vadodara', 'Muvada', '391160'),
    ('Vadodara', 'Muval', '391770'),
    ('Vadodara', 'Nada', '391210'),
    ('Vadodara', 'Nahra', '391770'),
    ('Vadodara', 'Nalvant', '391152'),
    ('Vadodara', 'Namisara', '391775'),
    ('Vadodara', 'Nana Amadara', '391135'),
    ('Vadodara', 'Nana Fofalia', '391210'),
    ('Vadodara', 'Nana Karala', '391105'),
    ('Vadodara', 'Nanderia', '391105'),
    ('Vadodara', 'Nandesari', '391340'),
    ('Vadodara', 'Nandesari Ind. estate', '391340'),
    ('Vadodara', 'Nandpur', '391145'),
    ('Vadodara', 'Nani Bhadol', '391520'),
    ('Vadodara', 'Nani Tokri', '391165'),
    ('Vadodara', 'Naria', '391107'),
    ('Vadodara', 'Narpura', '391530'),
    ('Vadodara', 'Narshihpura', '391440'),
    ('Vadodara', 'Naswadi', '391150'),
    ('Vadodara', 'Natwar Nagar', '391780'),
    ('Vadodara', 'Navagam', '391510'),
    ('Vadodara', 'Navalja', '391170'),
    ('Vadodara', 'Navapura', '391125'),
    ('Vadodara', 'Nimetha', '390019'),
    ('Vadodara', 'Ongc Col', '390009'),
    ('Vadodara', 'Oslam', '391244'),
    ('Vadodara', 'Padharwant', '391165'),
    ('Vadodara', 'Padmala', '391350'),
    ('Vadodara', 'Padra', '391440'),
    ('Vadodara', 'Pala', '391150'),
    ('Vadodara', 'Palasani', '391152'),
    ('Vadodara', 'Palaswada', '391107'),
    ('Vadodara', 'Palsanda', '391160'),
    ('Vadodara', 'Panchdevla', '391510'),
    ('Vadodara', 'Pandhara', '391155'),
    ('Vadodara', 'Pandu', '391530'),
    ('Vadodara', 'Panej', '391140'),
    ('Vadodara', 'Pani', '391160'),
    ('Vadodara', 'Pansoli', '391110'),
    ('Vadodara', 'Panvad', '391168'),
    ('Vadodara', 'Parikha', '391210'),
    ('Vadodara', 'Parvata', '391150'),
    ('Vadodara', 'Pasva', '391520'),
    ('Vadodara', 'Patarveni', '391220'),
    ('Vadodara', 'Pavijetpur', '391160'),
    ('Vadodara', 'Petrochemical', '391346'),
    ('Vadodara', 'Petrochemical t ship', '391345'),
    ('Vadodara', 'Picchuwada', '391145'),
    ('Vadodara', 'Pilol', '391745'),
    ('Vadodara', 'Pindappa', '391430'),
    ('Vadodara', 'Pingalvada', '391240'),
    ('Vadodara', 'Pipalchhat', '391530'),
    ('Vadodara', 'Pipaldi', '391170'),
    ('Vadodara', 'Pipalia', '391760'),
    ('Vadodara', 'Pipalsat', '391145'),
    ('Vadodara', 'Pipli', '391445'),
    ('Vadodara', 'Pisai', '391111'),
    ('Vadodara', 'Pochamba', '391150'),
    ('Vadodara', 'Poicha', '391780'),
    ('Vadodara', 'Por', '391243'),
    ('Vadodara', 'Pratapganj', '390002'),
    ('Vadodara', 'Pratapnagar', '390004'),
    ('Vadodara', 'Pratapnagar R.s.', '390004'),
    ('Vadodara', 'Pratappura', '391774'),
    ('Vadodara', 'Prathampura', '391770'),
    ('Vadodara', 'Puniad', '391111'),
    ('Vadodara', 'Puniavant', '391165'),
    ('Vadodara', 'Racecourse', '390007'),
    ('Vadodara', 'Rahkui', '391510'),
    ('Vadodara', 'Raichha', '391168'),
    ('Vadodara', 'Raika', '391340'),
    ('Vadodara', 'Raipur', '391150'),
    ('Vadodara', 'Raipura', '391410'),
    ('Vadodara', 'Raisingpura', '391160'),
    ('Vadodara', 'Rajbodeli', '391140'),
    ('Vadodara', 'Rajpura', '391510'),
    ('Vadodara', 'Rajupura', '391530'),
    ('Vadodara', 'Ramnath', '391220'),
    ('Vadodara', 'Rampura', '391145'),
    ('Vadodara', 'Rangpur (k)', '391135'),
    ('Vadodara', 'Rangpur(z)', '391165'),
    ('Vadodara', 'Rania', '391780'),
    ('Vadodara', 'Raniapura', '391520'),
    ('Vadodara', 'Ranjit Nagar', '391780'),
    ('Vadodara', 'Ranoli', '391350'),
    ('Vadodara', 'Ranu', '391445'),
    ('Vadodara', 'Rasulabad', '391510'),
    ('Vadodara', 'Rasulpur', '391770'),
    ('Vadodara', 'Ratanpur', '391160'),
    ('Vadodara', 'Ratanpura', '391145'),
    ('Vadodara', 'Raval', '391760'),
    ('Vadodara', 'Remeshwarpura', '391761'),
    ('Vadodara', 'Rodadha', '391165'),
    ('Vadodara', 'Rumadia', '391165'),
    ('Vadodara', 'Rustampura', '391761'),
    ('Vadodara', 'Ruvand', '391220'),
    ('Vadodara', 'Srp Group i', '390001'),
    ('Vadodara', 'Sadhli', '391250'),
    ('Vadodara', 'Sadra', '391510'),
    ('Vadodara', 'Sagwa', '391105'),
    ('Vadodara', 'Saidal', '391761'),
    ('Vadodara', 'Saidivasana', '391170'),
    ('Vadodara', 'Saiyed Vasna', '390007'),
    ('Vadodara', 'Saloj', '391135'),
    ('Vadodara', 'Sama', '390008'),
    ('Vadodara', 'Sambhoi', '391240'),
    ('Vadodara', 'Samiyala', '391410'),
    ('Vadodara', 'Samlaya R s', '391520'),
    ('Vadodara', 'Sampa', '391240'),
    ('Vadodara', 'Sampla', '391421'),
    ('Vadodara', 'Samra', '391244'),
    ('Vadodara', 'Samsabad', '391107'),
    ('Vadodara', 'Sanada', '391165'),
    ('Vadodara', 'Sandasal', '391530'),
    ('Vadodara', 'Sandha', '391440'),
    ('Vadodara', 'Sangadol', '391760'),
    ('Vadodara', 'Sangma', '391440'),
    ('Vadodara', 'Saniad', '391244'),
    ('Vadodara', 'Sankarda', '391350'),
    ('Vadodara', 'Sankhandra', '391135'),
    ('Vadodara', 'Sankheda', '391145'),
    ('Vadodara', 'Sanor', '391105'),
    ('Vadodara', 'Sanpur', '391430'),
    ('Vadodara', 'Santkawnar Colony', '390006'),
    ('Vadodara', 'Sarar', '391240'),
    ('Vadodara', 'Sarasavani', '391445'),
    ('Vadodara', 'Sargi', '391140'),
    ('Vadodara', 'Sarnej', '391510'),
    ('Vadodara', 'Sarsinda', '391130'),
    ('Vadodara', 'Sathod', '391111'),
    ('Vadodara', 'Savli', '391770'),
    ('Vadodara', 'Sayajiganj', '390020'),
    ('Vadodara', 'Sayajipura', '390019'),
    ('Vadodara', 'Segwa Simli', '391155'),
    ('Vadodara', 'Sejakuva', '391440'),
    ('Vadodara', 'Sejwa', '391135'),
    ('Vadodara', 'Sengpur', '391152'),
    ('Vadodara', 'Sevasi', '391101'),
    ('Vadodara', 'Shahpura', '391220'),
    ('Vadodara', 'Sharadnagar', '390009'),
    ('Vadodara', 'Sherkhi', '391330'),
    ('Vadodara', 'Shira', '391150'),
    ('Vadodara', 'Sihora', '391530'),
    ('Vadodara', 'Simalia', '391110'),
    ('Vadodara', 'Simli', '391105'),
    ('Vadodara', 'Simri', '391244'),
    ('Vadodara', 'Sindhikuva', '391150'),
    ('Vadodara', 'Sindhrot', '391330'),
    ('Vadodara', 'Singla', '391168'),
    ('Vadodara', 'Sinor', '391115'),
    ('Vadodara', 'Sisva', '391740'),
    ('Vadodara', 'Sitpur', '391110'),
    ('Vadodara', 'Sodhaliya', '391152'),
    ('Vadodara', 'Sokhda', '391745'),
    ('Vadodara', 'Sokhdaradhu', '391240'),
    ('Vadodara', 'Subhanpura', '390023'),
    ('Vadodara', 'Subhelav', '391775'),
    ('Vadodara', 'Sukal', '391155'),
    ('Vadodara', 'Sundarpura', '391240'),
    ('Vadodara', 'Sunderpura', '391145'),
    ('Vadodara', 'Surkheda', '391165'),
    ('Vadodara', 'T B sanatorium', '390021'),
    ('Vadodara', 'Tadkachhla', '391135'),
    ('Vadodara', 'Tajpura', '391440'),
    ('Vadodara', 'Tanakhala', '391151'),
    ('Vadodara', 'Tandalja', '390012'),
    ('Vadodara', 'Targol', '391125'),
    ('Vadodara', 'Tarsali', '390009'),
    ('Vadodara', 'Tatarpura', '390004'),
    ('Vadodara', 'Tejgadh', '391160'),
    ('Vadodara', 'Tentalav', '391105'),
    ('Vadodara', 'Thadgam', '391170'),
    ('Vadodara', 'Thuwavi', '391107'),
    ('Vadodara', 'Timba', '391130'),
    ('Vadodara', 'Tithor', '391450'),
    ('Vadodara', 'Totarmata', '391125'),
    ('Vadodara', 'Tulsipura', '391510'),
    ('Vadodara', 'Tundav', '391775'),
    ('Vadodara', 'Uchapan', '391160'),
    ('Vadodara', 'Umaraya', '391440'),
    ('Vadodara', 'Umarva', '391160'),
    ('Vadodara', 'Umathi', '391170'),
    ('Vadodara', 'Undera', '391330'),
    ('Vadodara', 'Urad', '391244'),
    ('Vadodara', 'Vachhesar', '388710'),
    ('Vadodara', 'Vadadla', '391445'),
    ('Vadodara', 'Vadaj', '391105'),
    ('Vadodara', 'Vadeli', '391145'),
    ('Vadodara', 'Vadia', '391520'),
    ('Vadodara', 'Vadodara', '390001'),
    ('Vadodara', 'Vadsala', '391240'),
    ('Vadodara', 'Vadsar', '390010'),
    ('Vadodara', 'Vadu', '391460'),
    ('Vadodara', 'Vaghodia', '391760'),
    ('Vadodara', 'Vaguma', '391152'),
    ('Vadodara', 'Vakaner', '391780'),
    ('Vadodara', 'Vaktapura', '391530'),
    ('Vadodara', 'Valan', '392310'),
    ('Vadodara', 'Valavav', '391774'),
    ('Vadodara', 'Valothi', '391160'),
    ('Vadodara', 'Valva', '391761'),
    ('Vadodara', 'Vanadara', '391107'),
    ('Vadodara', 'Vanchhara', '391430'),
    ('Vadodara', 'Vaniad', '391105'),
    ('Vadodara', 'Vaniadri', '391135'),
    ('Vadodara', 'Vankaneda', '391774'),
    ('Vadodara', 'Vankla', '391152'),
    ('Vadodara', 'Vanta', '391135'),
    ('Vadodara', 'Varnama', '391240'),
    ('Vadodara', 'Varsada', '391774'),
    ('Vadodara', 'Vasai', '391110'),
    ('Vadodara', 'Vasan Sevada', '391145'),
    ('Vadodara', 'Vasanpura', '391770'),
    ('Vadodara', 'Vasia', '391530'),
    ('Vadodara', 'Vasna', '391140'),
    ('Vadodara', 'Vasna Kotaria', '391745'),
    ('Vadodara', 'Vasna Road', '390007'),
    ('Vadodara', 'Vasvel', '391760'),
    ('Vadodara', 'Vedpur', '391761'),
    ('Vadodara', 'Vejalpura', '391760'),
    ('Vadodara', 'Vejpur', '391535'),
    ('Vadodara', 'Vemar', '391520'),
    ('Vadodara', 'Vemardi', '391210'),
    ('Vadodara', 'Vesania', '391760'),
    ('Vadodara', 'Vidhyut Nagar colony', '390007'),
    ('Vadodara', 'Vijli', '391170'),
    ('Vadodara', 'Vintoz', '391770'),
    ('Vadodara', 'Virjai', '391240'),
    ('Vadodara', 'Virod', '390022'),
    ('Vadodara', 'Virpur', '391445'),
    ('Vadodara', 'Vistar', '391440'),
    ('Vadodara', 'Vyankatpura', '391510'),
    ('Vadodara', 'Vyara Antoli', '391760'),
    ('Vadodara', 'Wadhwana', '391110'),
    ('Vadodara', 'Wadi S.n. road', '390017'),
    ('Vadodara', 'Wagach', '391152'),
    ('Vadodara', 'Waghodia Road', '390019'),
    ('Vadodara', 'Waghva', '391160'),
    ('Vadodara', 'Wandarda', '391761'),
    ('Vadodara', 'Zanzad', '391115'),
    ('Vadodara', 'Zer', '391152'),
    ('Vadodara', 'Zoz', '391165'),
    ('Ahmedabad', 'Abasana', '382120'),
    ('Ahmedabad', 'Adroda', '382220'),
    ('Ahmedabad', 'Adval', '382460'),
    ('Ahmedabad', 'Ahmedabad.', '380001'),
    ('Ahmedabad', 'Akru', '382250'),
    ('Ahmedabad', 'Alampur', '363610'),
    ('Ahmedabad', 'Alau', '382255'),
    ('Ahmedabad', 'Ambaliara', '387810'),
    ('Ahmedabad', 'Ambareli', '387810'),
    ('Ahmedabad', 'Ambawadi', '380006'),
    ('Ahmedabad', 'Ambawadi Vistar', '380015'),
    ('Ahmedabad', 'Ambli', '382463'),
    ('Ahmedabad', 'Amraiwadi', '380026'),
    ('Ahmedabad', 'Anandnagar', '380007'),
    ('Ahmedabad', 'Andej', '382115'),
    ('Ahmedabad', 'Aniali ( k )', '363610'),
    ('Ahmedabad', 'Aniali Bhimji', '382250'),
    ('Ahmedabad', 'Arnej', '382230'),
    ('Ahmedabad', 'Asalgam', '382150'),
    ('Ahmedabad', 'Asarwa Chakla', '380016'),
    ('Ahmedabad', 'Asarwa Ext south', '380024'),
    ('Ahmedabad', 'Ashoknagar', '382140'),
    ('Ahmedabad', 'Ashram Road', '380009'),
    ('Ahmedabad', 'Aslali', '382427'),
    ('Ahmedabad', 'Azad Society', '380015'),
    ('Ahmedabad', 'Badanpur', '382465'),
    ('Ahmedabad', 'Badarkha', '382276'),
    ('Ahmedabad', 'Bagad', '382255'),
    ('Ahmedabad', 'Bagodara', '382230'),
    ('Ahmedabad', 'Bajarda', '382460'),
    ('Ahmedabad', 'Bakrana', '382170'),
    ('Ahmedabad', 'Bakrol', '382430'),
    ('Ahmedabad', 'Baldana', '382240'),
    ('Ahmedabad', 'Balsasan', '382145'),
    ('Ahmedabad', 'Bamroli', '382145'),
    ('Ahmedabad', 'Bamsara', '382240'),
    ('Ahmedabad', 'Bapunagar', '380024'),
    ('Ahmedabad', 'Bareja', '382425'),
    ('Ahmedabad', 'Barwala Ghelasha', '382450'),
    ('Ahmedabad', 'Bavaliary', '382455'),
    ('Ahmedabad', 'Bavla', '382220'),
    ('Ahmedabad', 'Bavla Market yard', '382220'),
    ('Ahmedabad', 'Behrampura', '380022'),
    ('Ahmedabad', 'Bela', '382450'),
    ('Ahmedabad', 'Bhadaj', '380060'),
    ('Ahmedabad', 'Bhadana', '382140'),
    ('Ahmedabad', 'Bhadiad', '382463'),
    ('Ahmedabad', 'Bhairavnath Road', '380028'),
    ('Ahmedabad', 'Bhangadh', '382455'),
    ('Ahmedabad', 'Bhankoda', '382140'),
    ('Ahmedabad', 'Bhat', '382210'),
    ('Ahmedabad', 'Bhavda', '382433'),
    ('Ahmedabad', 'Bhayla', '382220'),
    ('Ahmedabad', 'Bhojwa', '382150'),
    ('Ahmedabad', 'Bholad', '382230'),
    ('Ahmedabad', 'Bhoyani', '382145'),
    ('Ahmedabad', 'Bhurkhi', '382230'),
    ('Ahmedabad', 'Bhuvaldi', '382430'),
    ('Ahmedabad', 'Bilasia', '382330'),
    ('Ahmedabad', 'Bodakdev', '380054'),
    ('Ahmedabad', 'Bodia', '363610'),
    ('Ahmedabad', 'Bol', '382170'),
    ('Ahmedabad', 'Bopal', '380058'),
    ('Ahmedabad', 'Bordibazar Viramgam', '382150'),
    ('Ahmedabad', 'Calico Mills', '380022'),
    ('Ahmedabad', 'Cantonment', '380004'),
    ('Ahmedabad', 'Chaloda', '382260'),
    ('Ahmedabad', 'Chandarva', '382255'),
    ('Ahmedabad', 'Chandial', '382433'),
    ('Ahmedabad', 'Chandisar', '382260'),
    ('Ahmedabad', 'Chandlodia', '382481'),
    ('Ahmedabad', 'Chandranagar', '382150'),
    ('Ahmedabad', 'Changodar', '382213'),
    ('Ahmedabad', 'Charanki', '382450'),
    ('Ahmedabad', 'Charel', '382110'),
    ('Ahmedabad', 'Charol', '382120'),
    ('Ahmedabad', 'Chaulaj', '382433'),
    ('Ahmedabad', 'Chekhla', '382115'),
    ('Ahmedabad', 'Chhabasar', '382220'),
    ('Ahmedabad', 'Chhaniar', '382140'),
    ('Ahmedabad', 'Chharodi', '382170'),
    ('Ahmedabad', 'Chhasiana', '382460'),
    ('Ahmedabad', 'Chiada', '382220'),
    ('Ahmedabad', 'Chokdi', '382250'),
    ('Ahmedabad', 'Chuval Dangarva', '382145'),
    ('Ahmedabad', 'Civil Hospital', '380016'),
    ('Ahmedabad', 'Ctm Char rasta', '380026'),
    ('Ahmedabad', 'D Cabin', '380019'),
    ('Ahmedabad', 'Dabhsar', '382120'),
    ('Ahmedabad', 'Dadhana', '382120'),
    ('Ahmedabad', 'Daduka', '382110'),
    ('Ahmedabad', 'Dadusar', '387810'),
    ('Ahmedabad', 'Dalod', '382130'),
    ('Ahmedabad', 'Daran', '382220'),
    ('Ahmedabad', 'Dariapur', '380001'),
    ('Ahmedabad', 'Daslana', '382150'),
    ('Ahmedabad', 'Daxini Society', '380008'),
    ('Ahmedabad', 'Dehgamda', '382220'),
    ('Ahmedabad', 'Dekawada', '382120'),
    ('Ahmedabad', 'Delhi Gate', '380004'),
    ('Ahmedabad', 'Deo Dholera', '382240'),
    ('Ahmedabad', 'Detroj', '382120'),
    ('Ahmedabad', 'Devaliya', '363610'),
    ('Ahmedabad', 'Devpura', '382140'),
    ('Ahmedabad', 'Devtimoti', '382213'),
    ('Ahmedabad', 'Dhakdi', '382150'),
    ('Ahmedabad', 'Dhamatvan', '382435'),
    ('Ahmedabad', 'Dhanala', '382465'),
    ('Ahmedabad', 'Dhandhuka', '382460'),
    ('Ahmedabad', 'Dhanwada', '382220'),
    ('Ahmedabad', 'Dharpipla', '363610'),
    ('Ahmedabad', 'Dhedasana', '382130'),
    ('Ahmedabad', 'Dhingada', '382230'),
    ('Ahmedabad', 'Dholera', '382455'),
    ('Ahmedabad', 'Dholi', '382240'),
    ('Ahmedabad', 'Dholka', '387810'),
    ('Ahmedabad', 'Digvijaynagar', '382470'),
    ('Ahmedabad', 'District Court', '380001'),
    ('Ahmedabad', 'Dodar', '382170'),
    ('Ahmedabad', 'Dudheshwar Tavdipura', '380004'),
    ('Ahmedabad', 'Dumana', '382150'),
    ('Ahmedabad', 'Durgi', '382230'),
    ('Ahmedabad', 'Ellisbridge', '380006'),
    ('Ahmedabad', 'Endla', '382130'),
    ('Ahmedabad', 'Fangadi', '382110'),
    ('Ahmedabad', 'Fatepur', '382465'),
    ('Ahmedabad', 'Fatewadi', '382210'),
    ('Ahmedabad', 'Fedra', '382465'),
    ('Ahmedabad', 'Galsana', '363610'),
    ('Ahmedabad', 'Gamanpura', '382120'),
    ('Ahmedabad', 'Gamdi', '382435'),
    ('Ahmedabad', 'Gamph', '382465'),
    ('Ahmedabad', 'Gandhi Ashram', '380027'),
    ('Ahmedabad', 'Gandhi Road', '380001'),
    ('Ahmedabad', 'Gangad', '382240'),
    ('Ahmedabad', 'Ganol', '382265'),
    ('Ahmedabad', 'Garodia', '382115'),
    ('Ahmedabad', 'Gatrad', '382449'),
    ('Ahmedabad', 'Geratpur', '382435'),
    ('Ahmedabad', 'Ghatisana', '382140'),
    ('Ahmedabad', 'Ghatlodia', '380061'),
    ('Ahmedabad', 'Gheekanta Road', '380001'),
    ('Ahmedabad', 'Ghoda', '382150'),
    ('Ahmedabad', 'Ghodasar', '380050'),
    ('Ahmedabad', 'Ghuma', '380058'),
    ('Ahmedabad', 'Girdharnagar', '380004'),
    ('Ahmedabad', 'Girmatha', '382425'),
    ('Ahmedabad', 'Gita Mandir road', '380022'),
    ('Ahmedabad', 'Godhavata', '382255'),
    ('Ahmedabad', 'Godhavi', '382115'),
    ('Ahmedabad', 'Gogla', '382463'),
    ('Ahmedabad', 'Gomtipur', '380021'),
    ('Ahmedabad', 'Goraiya', '382150'),
    ('Ahmedabad', 'Goraj', '382110'),
    ('Ahmedabad', 'Gorasu', '382463'),
    ('Ahmedabad', 'Gota', '382481'),
    ('Ahmedabad', 'Gujarat High court', '380060'),
    ('Ahmedabad', 'Gujarat University', '380009'),
    ('Ahmedabad', 'Gunda', '382255'),
    ('Ahmedabad', 'Gundi Sarvodaya ashram', '382230'),
    ('Ahmedabad', 'Gunjala', '382120'),
    ('Ahmedabad', 'Gunjar', '382460'),
    ('Ahmedabad', 'Gyaspur', '382405'),
    ('Ahmedabad', 'Hansalpur', '382150'),
    ('Ahmedabad', 'Harniav', '382435'),
    ('Ahmedabad', 'Hathijan', '382445'),
    ('Ahmedabad', 'Hebatpur', '382455'),
    ('Ahmedabad', 'Hirapur', '382435'),
    ('Ahmedabad', 'Hirapura', '382150'),
    ('Ahmedabad', 'I E bapunagar', '380024'),
    ('Ahmedabad', 'I I m', '380015'),
    ('Ahmedabad', 'Iawa (vasna)', '382170'),
    ('Ahmedabad', 'Ingoli', '382265'),
    ('Ahmedabad', 'Isanpur', '382443'),
    ('Ahmedabad', 'Jagatpur', '382470'),
    ('Ahmedabad', 'Jakhada', '382230'),
    ('Ahmedabad', 'Jakwada', '382150'),
    ('Ahmedabad', 'Jalalpur Vajifa', '387810'),
    ('Ahmedabad', 'Jalampur', '382150'),
    ('Ahmedabad', 'Jalila', '382255'),
    ('Ahmedabad', 'Jalisana', '382120'),
    ('Ahmedabad', 'Jaliya', '382460'),
    ('Ahmedabad', 'Jamalpur', '380001'),
    ('Ahmedabad', 'Jamp', '382110'),
    ('Ahmedabad', 'Jantanagar', '382449'),
    ('Ahmedabad', 'Jawahar Chowk', '380008'),
    ('Ahmedabad', 'Jawaraj', '382230'),
    ('Ahmedabad', 'Jeska', '382250'),
    ('Ahmedabad', 'Jetalpur', '382426'),
    ('Ahmedabad', 'Jholapur', '382170'),
    ('Ahmedabad', 'Jivraj Park', '380051'),
    ('Ahmedabad', 'Jodhpur Char rasta', '380015'),
    ('Ahmedabad', 'Joshipura', '382150'),
    ('Ahmedabad', 'Juhapura', '380055'),
    ('Ahmedabad', 'Junapadhar', '382150'),
    ('Ahmedabad', 'Juval', '382220'),
    ('Ahmedabad', 'Juval Rupavati', '382220'),
    ('Ahmedabad', 'Kabir Chowk', '380005'),
    ('Ahmedabad', 'Kadipur', '382463'),
    ('Ahmedabad', 'Kadvasan', '382130'),
    ('Ahmedabad', 'Kalana', '382170'),
    ('Ahmedabad', 'Kali', '382470'),
    ('Ahmedabad', 'Kalupur Chakla', '380001'),
    ('Ahmedabad', 'Kalyangadh', '382240'),
    ('Ahmedabad', 'Kamiala', '382465'),
    ('Ahmedabad', 'Kamijala', '382150'),
    ('Ahmedabad', 'Kanbha', '382430'),
    ('Ahmedabad', 'Kaneti', '382110'),
    ('Ahmedabad', 'Kaniyel', '382433'),
    ('Ahmedabad', 'Kankravadi', '382150'),
    ('Ahmedabad', 'Kanz', '382140'),
    ('Ahmedabad', 'Karakthal', '382150'),
    ('Ahmedabad', 'Kasindra', '382210'),
    ('Ahmedabad', 'Kathwada', '382430'),
    ('Ahmedabad', 'Kathwada Maize product', '382430'),
    ('Ahmedabad', 'Katosan Road', '382145'),
    ('Ahmedabad', 'Kauka', '382265'),
    ('Ahmedabad', 'Kavitha', '382260'),
    ('Ahmedabad', 'Kelia vasna', '387810'),
    ('Ahmedabad', 'Kerala', '382220'),
    ('Ahmedabad', 'Keria', '363610'),
    ('Ahmedabad', 'Kesardi', '382240'),
    ('Ahmedabad', 'Khadia', '380001'),
    ('Ahmedabad', 'Khadol', '382460'),
    ('Ahmedabad', 'Khambhada', '382450'),
    ('Ahmedabad', 'Khamidana', '382450'),
    ('Ahmedabad', 'Khandachora Dhandhuka', '382460'),
    ('Ahmedabad', 'Khanpur', '380001'),
    ('Ahmedabad', 'Kharad', '382460'),
    ('Ahmedabad', 'Kharakuva Dholka', '387810'),
    ('Ahmedabad', 'Khas', '382255'),
    ('Ahmedabad', 'Khasta', '382460'),
    ('Ahmedabad', 'Khoda', '382170'),
    ('Ahmedabad', 'Khodiyar', '382421'),
    ('Ahmedabad', 'Khodiyarnagar', '382350'),
    ('Ahmedabad', 'Khokarnesh', '363610'),
    ('Ahmedabad', 'Khokhara Mehmadabad', '380008'),
    ('Ahmedabad', 'Khorajnanoda', '382170'),
    ('Ahmedabad', 'Khudad', '382150'),
    ('Ahmedabad', 'Khun', '382455'),
    ('Ahmedabad', 'Kocharia', '382220'),
    ('Ahmedabad', 'Kointia', '382140'),
    ('Ahmedabad', 'Kokta', '382150'),
    ('Ahmedabad', 'Kolat', '382210'),
    ('Ahmedabad', 'Koth', '382240'),
    ('Ahmedabad', 'Kothadia', '382460'),
    ('Ahmedabad', 'Krishnanagar', '382345'),
    ('Ahmedabad', 'Kubadthal', '382430'),
    ('Ahmedabad', 'Kubernagar', '382340'),
    ('Ahmedabad', 'Kubernagar B a', '382340'),
    ('Ahmedabad', 'Kuha', '382433'),
    ('Ahmedabad', 'Kujad', '382430'),
    ('Ahmedabad', 'Kukvav', '382120'),
    ('Ahmedabad', 'Kumarkhan', '382150'),
    ('Ahmedabad', 'Kundal', '382110'),
    ('Ahmedabad', 'Kundli', '363610'),
    ('Ahmedabad', 'Kunpur', '382130'),
    ('Ahmedabad', 'Kunvar', '382110'),
    ('Ahmedabad', 'L G hospital', '380008'),
    ('Ahmedabad', 'Lal Darwaja', '380001'),
    ('Ahmedabad', 'Lambha', '382405'),
    ('Ahmedabad', 'Lapkaman', '380060'),
    ('Ahmedabad', 'Limbadia', '382330'),
    ('Ahmedabad', 'Loliya', '382465'),
    ('Ahmedabad', 'Lothal Bhurkhi rs', '382230'),
    ('Ahmedabad', 'M D marg', '380022'),
    ('Ahmedabad', 'Madhupura Market', '380004'),
    ('Ahmedabad', 'Madrisana', '382145'),
    ('Ahmedabad', 'Mahijada', '382425'),
    ('Ahmedabad', 'Makarba', '380051'),
    ('Ahmedabad', 'Makhiav', '382110'),
    ('Ahmedabad', 'Mandal', '382130'),
    ('Ahmedabad', 'Manek Chowk', '380001'),
    ('Ahmedabad', 'Maninagar', '380008'),
    ('Ahmedabad', 'Manipur', '382150'),
    ('Ahmedabad', 'Mankol', '382110'),
    ('Ahmedabad', 'Marusana', '382120'),
    ('Ahmedabad', 'Matoda', '382213'),
    ('Ahmedabad', 'Medra', '382330'),
    ('Ahmedabad', 'Meghaningar', '380016'),
    ('Ahmedabad', 'Melaj', '382150'),
    ('Ahmedabad', 'Memnagar', '380052'),
    ('Ahmedabad', 'Mingalpur', '382455'),
    ('Ahmedabad', 'Miroli', '382425'),
    ('Ahmedabad', 'Mithapur', '382230'),
    ('Ahmedabad', 'Modasar', '382220'),
    ('Ahmedabad', 'Moraiya', '382213'),
    ('Ahmedabad', 'Mota Tradia', '382460'),
    ('Ahmedabad', 'Motera', '380005'),
    ('Ahmedabad', 'Moti kishol', '382110'),
    ('Ahmedabad', 'Moti Boru', '382230'),
    ('Ahmedabad', 'Moti Rantai', '382145'),
    ('Ahmedabad', 'Moti Vavdi', '382255'),
    ('Ahmedabad', 'Muktipur', '382425'),
    ('Ahmedabad', 'Municipal Corporation', '380001'),
    ('Ahmedabad', 'N C market', '380002'),
    ('Ahmedabad', 'N C mills', '382345'),
    ('Ahmedabad', 'Nadishala', '382120'),
    ('Ahmedabad', 'Nana Chiloda', '382330'),
    ('Ahmedabad', 'Nana Ubhada', '382130'),
    ('Ahmedabad', 'Nandej', '382435'),
    ('Ahmedabad', 'Nani Boru', '382230'),
    ('Ahmedabad', 'Nani Devti', '382220'),
    ('Ahmedabad', 'Nani Vavdi', '363610'),
    ('Ahmedabad', 'Nanodara', '382220'),
    ('Ahmedabad', 'Naranpura Vistar', '380013'),
    ('Ahmedabad', 'Narayannagar', '380007'),
    ('Ahmedabad', 'Naroda', '382330'),
    ('Ahmedabad', 'Naroda I e', '382330'),
    ('Ahmedabad', 'Naroda Road', '382345'),
    ('Ahmedabad', 'Naroda S a', '382330'),
    ('Ahmedabad', 'Narol', '382405'),
    ('Ahmedabad', 'Nava Vadaj', '380013'),
    ('Ahmedabad', 'Navagam', '382130'),
    ('Ahmedabad', 'Navapura', '382210'),
    ('Ahmedabad', 'Navda', '382450'),
    ('Ahmedabad', 'Navjivan', '380014'),
    ('Ahmedabad', 'Navrangpura', '380009'),
    ('Ahmedabad', 'Nayakpur', '382130'),
    ('Ahmedabad', 'Nedharad', '382115'),
    ('Ahmedabad', 'Nesda', '387810'),
    ('Ahmedabad', 'Nikol', '382350'),
    ('Ahmedabad', 'Nirnaynagar', '382481'),
    ('Ahmedabad', 'Noblenagar', '382340'),
    ('Ahmedabad', 'O N g c', '380005'),
    ('Ahmedabad', 'Ode', '382427'),
    ('Ahmedabad', 'Odhav', '382120'),
    ('Ahmedabad', 'Odhav Industrial estate', '382415'),
    ('Ahmedabad', 'Ogan', '382150'),
    ('Ahmedabad', 'Ognaj', '380060'),
    ('Ahmedabad', 'Otaria', '382463'),
    ('Ahmedabad', 'Otaria Sarvodaya ashram', '382463'),
    ('Ahmedabad', 'Padana', '382460'),
    ('Ahmedabad', 'Paldi', '380007'),
    ('Ahmedabad', 'Paldi Kankaj', '382425'),
    ('Ahmedabad', 'Panar', '382140'),
    ('Ahmedabad', 'Panchham', '382465'),
    ('Ahmedabad', 'Panchhi', '382455'),
    ('Ahmedabad', 'Parabadi', '382250'),
    ('Ahmedabad', 'Pardhol', '382330'),
    ('Ahmedabad', 'Pasunj', '382430'),
    ('Ahmedabad', 'Pimpan', '382110'),
    ('Ahmedabad', 'Pipli', '382465'),
    ('Ahmedabad', 'Pirana', '382425'),
    ('Ahmedabad', 'Pisawada', '382265'),
    ('Ahmedabad', 'Polarpur', '382250'),
    ('Ahmedabad', 'Polytechnic', '380015'),
    ('Ahmedabad', 'Post Bureau extn counter', '380004'),
    ('Ahmedabad', 'Public Office', '380016'),
    ('Ahmedabad', 'Rahemalpur', '382150'),
    ('Ahmedabad', 'Raikhad', '380001'),
    ('Ahmedabad', 'Railway Colony', '380019'),
    ('Ahmedabad', 'Railwaypura', '380002'),
    ('Ahmedabad', 'Raipur', '380001'),
    ('Ahmedabad', 'Rajoda', '382220'),
    ('Ahmedabad', 'Rajpur Gomtipur', '380021'),
    ('Ahmedabad', 'Rajpura', '363610'),
    ('Ahmedabad', 'Rakhial', '380023'),
    ('Ahmedabad', 'Rakhial Udyog vistar', '380023'),
    ('Ahmedabad', 'Rakhiana', '382130'),
    ('Ahmedabad', 'Ramol', '382449'),
    ('Ahmedabad', 'Rampur', '387810'),
    ('Ahmedabad', 'Rampura', '382140'),
    ('Ahmedabad', 'Ranesar', '382220'),
    ('Ahmedabad', 'Rangpur', '382150'),
    ('Ahmedabad', 'Ranip', '382480'),
    ('Ahmedabad', 'Ranoda', '387810'),
    ('Ahmedabad', 'Ranodra', '382433'),
    ('Ahmedabad', 'Ranpur', '363610'),
    ('Ahmedabad', 'Rasam', '382220'),
    ('Ahmedabad', 'Rayka', '382460'),
    ('Ahmedabad', 'Refda', '382255'),
    ('Ahmedabad', 'Rethal', '382110'),
    ('Ahmedabad', 'Revdibazar', '380002'),
    ('Ahmedabad', 'Ribadi', '382130'),
    ('Ahmedabad', 'Rohika', '382230'),
    ('Ahmedabad', 'Rojid', '382450'),
    ('Ahmedabad', 'Rojka', '382460'),
    ('Ahmedabad', 'Rudatal', '382120'),
    ('Ahmedabad', 'Rupal', '382220'),
    ('Ahmedabad', 'Rupgadh', '382240'),
    ('Ahmedabad', 'S A c', '380015'),
    ('Ahmedabad', 'S A mills', '380008'),
    ('Ahmedabad', 'Sabarmati', '380005'),
    ('Ahmedabad', 'Sachana', '382150'),
    ('Ahmedabad', 'Sadatpura', '382120'),
    ('Ahmedabad', 'Sahij', '387810'),
    ('Ahmedabad', 'Saijpur', '382405'),
    ('Ahmedabad', 'Saijpur Bogha', '382345'),
    ('Ahmedabad', 'Saiwada', '387810'),
    ('Ahmedabad', 'Salajada', '382220'),
    ('Ahmedabad', 'Salangpur - hanuman', '382451'),
    ('Ahmedabad', 'Salasar', '363610'),
    ('Ahmedabad', 'Sanand', '382110'),
    ('Ahmedabad', 'Sanathal', '382210'),
    ('Ahmedabad', 'Sandhida', '382455'),
    ('Ahmedabad', 'Sangasar', '382463'),
    ('Ahmedabad', 'Sangpura', '382120'),
    ('Ahmedabad', 'Sankod', '382220'),
    ('Ahmedabad', 'Sarandi', '387810'),
    ('Ahmedabad', 'Saraspur', '380018'),
    ('Ahmedabad', 'Sardarnagar', '382475'),
    ('Ahmedabad', 'Sargwala', '382230'),
    ('Ahmedabad', 'Sari', '382220'),
    ('Ahmedabad', 'Sarkhej', '382210'),
    ('Ahmedabad', 'Sarkhej Road', '380007'),
    ('Ahmedabad', 'Saroda', '382260'),
    ('Ahmedabad', 'Sarsavadi', '382150'),
    ('Ahmedabad', 'Sathal', '387810'),
    ('Ahmedabad', 'Shah Alam roza', '380028'),
    ('Ahmedabad', 'Shahibag', '380004'),
    ('Ahmedabad', 'Shahpur', '382150'),
    ('Ahmedabad', 'Shahwadi', '382405'),
    ('Ahmedabad', 'Shardanagar', '380007'),
    ('Ahmedabad', 'Shastrinagar', '380013'),
    ('Ahmedabad', 'Shela', '380058'),
    ('Ahmedabad', 'Sher', '382130'),
    ('Ahmedabad', 'Sherpura', '387810'),
    ('Ahmedabad', 'Shilaj', '380058'),
    ('Ahmedabad', 'Shivpura', '382140'),
    ('Ahmedabad', 'Shiyal', '382230'),
    ('Ahmedabad', 'Siawada', '382170'),
    ('Ahmedabad', 'Sihor', '382140'),
    ('Ahmedabad', 'Simej', '382265'),
    ('Ahmedabad', 'Sinaj', '382120'),
    ('Ahmedabad', 'Sindharej', '387810'),
    ('Ahmedabad', 'Sitapur', '382130'),
    ('Ahmedabad', 'Sobhasan', '382145'),
    ('Ahmedabad', 'Sodhi', '382463'),
    ('Ahmedabad', 'Sola', '380060'),
    ('Ahmedabad', 'Sola H b c', '380013'),
    ('Ahmedabad', 'Solgam', '382130'),
    ('Ahmedabad', 'Stadium Marg', '380013'),
    ('Ahmedabad', 'Sub Foreign', '380004'),
    ('Ahmedabad', 'Sukhrampura', '380023'),
    ('Ahmedabad', 'Sunderiana', '382255'),
    ('Ahmedabad', 'Sunvala', '382145'),
    ('Ahmedabad', 'T B nagar', '382350'),
    ('Ahmedabad', 'Tagadi', '382250'),
    ('Ahmedabad', 'Telav', '382210'),
    ('Ahmedabad', 'Telavi', '382145'),
    ('Ahmedabad', 'Thaltej', '380059'),
    ('Ahmedabad', 'Thaltej Road', '380054'),
    ('Ahmedabad', 'Thori Mubarak', '382150'),
    ('Ahmedabad', 'Thori Thumbha', '382150'),
    ('Ahmedabad', 'Thuleta', '382150'),
    ('Ahmedabad', 'Timba', '382425'),
    ('Ahmedabad', 'Tragad', '382470'),
    ('Ahmedabad', 'Transad', '387810'),
    ('Ahmedabad', 'Trent', '382150'),
    ('Ahmedabad', 'Ughroj', '382120'),
    ('Ahmedabad', 'Ukardi', '382140'),
    ('Ahmedabad', 'Ukhlod', '382150'),
    ('Ahmedabad', 'Umrela', '363610'),
    ('Ahmedabad', 'Unchadi', '382250'),
    ('Ahmedabad', 'Undrel', '382433'),
    ('Ahmedabad', 'Upardal', '382110'),
    ('Ahmedabad', 'Utelia', '382230'),
    ('Ahmedabad', 'Vadaj', '380013'),
    ('Ahmedabad', 'Vadgas', '382150'),
    ('Ahmedabad', 'Vadhela', '382450'),
    ('Ahmedabad', 'Vadod', '382433'),
    ('Ahmedabad', 'Vagad', '363610'),
    ('Ahmedabad', 'Valdhera', '387810'),
    ('Ahmedabad', 'Valhia', '382450'),
    ('Ahmedabad', 'Valinda', '382463'),
    ('Ahmedabad', 'Vanch', '382449'),
    ('Ahmedabad', 'Vani', '382150'),
    ('Ahmedabad', 'Vanpardi', '382130'),
    ('Ahmedabad', 'Vansva', '382150'),
    ('Ahmedabad', 'Vanthal', '382150'),
    ('Ahmedabad', 'Varmor', '382130'),
    ('Ahmedabad', 'Varna', '382265'),
    ('Ahmedabad', 'Vasai', '382425'),
    ('Ahmedabad', 'Vasisthnagar', '380008'),
    ('Ahmedabad', 'Vasna', '382460'),
    ('Ahmedabad', 'Vasna (iawa)', '382170'),
    ('Ahmedabad', 'Vasna chacharvadi', '382213'),
    ('Ahmedabad', 'Vastral', '382418'),
    ('Ahmedabad', 'Vataman', '382265'),
    ('Ahmedabad', 'Vatva', '382440'),
    ('Ahmedabad', 'Vatva Industrial estate', '382445'),
    ('Ahmedabad', 'Vautha', '387810'),
    ('Ahmedabad', 'Vehlal', '382330'),
    ('Ahmedabad', 'Vejalka', '382230'),
    ('Ahmedabad', 'Vejalpur', '380051'),
    ('Ahmedabad', 'Vekaria', '382110'),
    ('Ahmedabad', 'Vinchhan', '382120'),
    ('Ahmedabad', 'Vinchhiya', '382110'),
    ('Ahmedabad', 'Vinzol', '382445'),
    ('Ahmedabad', 'Vinzuvada', '382130'),
    ('Ahmedabad', 'Viramgam', '382150'),
    ('Ahmedabad', 'Virdi', '382265'),
    ('Ahmedabad', 'Virochannagar', '382170'),
    ('Ahmedabad', 'Vishalpur', '382210'),
    ('Ahmedabad', 'Vithlpura', '382120'),
    ('Ahmedabad', 'Zanu', '382430'),
    ('Ahmedabad', 'Zanzarka', '382460'),
    ('Ahmedabad', 'Zanzarva', '382120'),
    ('Ahmedabad', 'Zezra', '382150'),
    ('Ahmedabad', 'Zinzar', '382250')
) AS v(CITY_NAME, AREA_NAME, PINCODE_TXT)
JOIN CITY c ON c.CITY_NAME = v.CITY_NAME;
GO

/* ============================================================================
   09_Employer_Otp_And_JobStarted_Status.sql
   ============================================================================ */
ALTER TABLE OTP_RECORD ADD OTP_TYPE_CD VARCHAR(20) NOT NULL CONSTRAINT DF_OTP_RECORD_TYPE_CD DEFAULT ('JOB_START');
GO

ALTER TABLE OTP_RECORD ADD EMPLOYER_USER_ACCOUNT_ID INT NULL;
GO

ALTER TABLE OTP_RECORD ADD CONSTRAINT FK_OTP_RECORD_EMPLOYER
    FOREIGN KEY (EMPLOYER_USER_ACCOUNT_ID) REFERENCES USER_ACCOUNT(USER_ACCOUNT_ID);
GO

ALTER TABLE OTP_RECORD ADD CONSTRAINT CK_OTP_RECORD_TYPE_CD
    CHECK (OTP_TYPE_CD IN ('JOB_START', 'SATISFACTION'));
GO

ALTER TABLE JOB_APPLICATION DROP CONSTRAINT CK_JOB_APPLICATION_STATUS_CD;
GO

UPDATE JOB_APPLICATION SET STATUS_CD = 'JOB_STARTED' WHERE STATUS_CD = 'OTP_VERIFIED';
GO

ALTER TABLE JOB_APPLICATION ADD CONSTRAINT CK_JOB_APPLICATION_STATUS_CD
    CHECK (STATUS_CD IN ('PENDING','EMPLOYER_VIEWED','EMPLOYER_CONTACTED','JOB_STARTED','COMPLETED'));
GO

/* ============================================================================
   10_Profile_Address_Field.sql
   ============================================================================ */
ALTER TABLE EMPLOYEE_PROFILE ADD
    ADDRESS_TXT  NVARCHAR(400) NULL;
GO

ALTER TABLE EMPLOYER_PROFILE ADD
    ADDRESS_TXT  NVARCHAR(400) NULL;
GO

/* ============================================================================
   11_Rating_HourlyRate_Selection.sql
   ============================================================================ */
ALTER TABLE JOB_POST ADD HOURLY_WAGE_AMT DECIMAL(10,2) NULL;
GO

UPDATE JOB_POST SET HOURLY_WAGE_AMT = DAILY_WAGE_AMT / 8.0 WHERE DAILY_WAGE_AMT IS NOT NULL;
GO

ALTER TABLE JOB_POST ADD PRIMARY_SKILL_ID INT NULL;
GO

ALTER TABLE JOB_POST ADD CONSTRAINT FK_JOB_POST_PRIMARY_SKILL
    FOREIGN KEY (PRIMARY_SKILL_ID) REFERENCES SKILL(SKILL_ID);
GO

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

/* ============================================================================
   12_KaarigarRating_LearningVideo.sql
   ============================================================================ */
CREATE TABLE LEARNING_VIDEO (
    LEARNING_VIDEO_ID   INT IDENTITY(1,1) NOT NULL,
    SKILL_ID             INT               NOT NULL,
    TITLE                  NVARCHAR(200)     NOT NULL,
    DESCRIPTION_TXT         NVARCHAR(1000)    NULL,
    VIDEO_URL                 NVARCHAR(400)     NOT NULL,
    IS_ACTIVE_FL              BIT               NOT NULL CONSTRAINT DF_LEARNING_VIDEO_IS_ACTIVE_FL DEFAULT (1),

    CREATED_BY           NVARCHAR(100)     NOT NULL CONSTRAINT DF_LEARNING_VIDEO_CREATED_BY DEFAULT ('SYSTEM'),
    CREATED_TS            DATETIME2         NOT NULL CONSTRAINT DF_LEARNING_VIDEO_CREATED_TS DEFAULT (SYSUTCDATETIME()),
    UPDATED_BY             NVARCHAR(100)     NULL,
    UPDATED_TS              DATETIME2         NULL,

    CONSTRAINT PK_LEARNING_VIDEO PRIMARY KEY (LEARNING_VIDEO_ID),
    CONSTRAINT FK_LEARNING_VIDEO_SKILL FOREIGN KEY (SKILL_ID) REFERENCES SKILL(SKILL_ID)
);
GO

CREATE INDEX IX_LEARNING_VIDEO_SKILL ON LEARNING_VIDEO(SKILL_ID, IS_ACTIVE_FL);
GO

/* ============================================================================
   13_Job_Hourly_Rate_And_Duration.sql
   ============================================================================ */
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_JOB_POST_JOB_CATEGORY_CD')
    ALTER TABLE JOB_POST DROP CONSTRAINT DF_JOB_POST_JOB_CATEGORY_CD;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_JOB_POST_JOB_CATEGORY_CD')
    ALTER TABLE JOB_POST DROP CONSTRAINT CK_JOB_POST_JOB_CATEGORY_CD;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JOB_POST_JOB_CATEGORY_CD' AND object_id = OBJECT_ID('JOB_POST'))
    DROP INDEX IX_JOB_POST_JOB_CATEGORY_CD ON JOB_POST;
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('JOB_POST') AND name = 'JOB_CATEGORY_CD' AND is_nullable = 0
)
    ALTER TABLE JOB_POST ALTER COLUMN JOB_CATEGORY_CD NVARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JOB_POST') AND name = 'HOURLY_WAGE_AMT')
    ALTER TABLE JOB_POST ADD HOURLY_WAGE_AMT DECIMAL(10,2) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JOB_POST') AND name = 'DURATION_HOUR_NBR')
    ALTER TABLE JOB_POST ADD DURATION_HOUR_NBR INT NULL;
GO

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

IF NOT EXISTS (SELECT 1 FROM HOURLY_RATE_OPTION)
BEGIN
    INSERT INTO HOURLY_RATE_OPTION (RATE_LABEL_TXT, HOURLY_RATE_AMT, DISPLAY_ORDER_NBR, CREATED_BY) VALUES
        (N'₹50 / hour',  50.00, 1, 'SEED_SCRIPT'),
        (N'₹80 / hour',  80.00, 2, 'SEED_SCRIPT'),
        (N'₹100 / hour', 100.00, 3, 'SEED_SCRIPT'),
        (N'₹150 / hour', 150.00, 4, 'SEED_SCRIPT');
END
GO

/* ============================================================================
   14_JobApplication_Cancel_Workflow.sql
   ============================================================================ */
ALTER TABLE JOB_APPLICATION DROP CONSTRAINT CK_JOB_APPLICATION_STATUS_CD;
GO
ALTER TABLE JOB_APPLICATION ADD CONSTRAINT CK_JOB_APPLICATION_STATUS_CD
    CHECK (STATUS_CD IN (
        'PENDING','EMPLOYER_VIEWED','EMPLOYER_CONTACTED','JOB_STARTED','COMPLETED','CANCELLED'
    ));
GO

ALTER TABLE JOB_APPLICATION ADD CANCEL_REASON_CD NVARCHAR(30) NULL;
GO
ALTER TABLE JOB_APPLICATION ADD CANCEL_REASON_TXT NVARCHAR(500) NULL;
GO
ALTER TABLE JOB_APPLICATION ADD CANCELLED_TS DATETIME2 NULL;
GO

/* ============================================================================
   15_JobPost_Gender_Preference.sql
   ============================================================================ */
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JOB_POST') AND name = 'GENDER_PREFERENCE_CD')
    ALTER TABLE JOB_POST ADD GENDER_PREFERENCE_CD NVARCHAR(10) NOT NULL DEFAULT 'ANY';
GO

PRINT 'All 15 scripts combined and executed successfully.';