/* ============================================================================
   E-03 POST A JOB — schema additions
   ============================================================================ */

-- Job Category is a fixed, predefined list (not admin-editable, unlike the
-- Business Category dropdown in E-02), so it's stored as a checked CODE
-- column rather than a separate master table.
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

-- (SKILL and JOB_SKILL tables already exist per the original schema script —
--  no changes needed there. This script only adds the category column.)
