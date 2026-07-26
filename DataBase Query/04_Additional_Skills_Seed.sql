/* ============================================================================
   02_Additional_Skills_Seed.sql
   Adds the skills from the client-supplied list (warehouse/retail/hospitality
   roles) to the existing SKILL master table, so they immediately become
   available in:
     - Screen E-03 Post a Job -> Required Skills (multi-select)
     - Screen W-02 Employee Profile -> Skills (multi-select)
     - Screen W-03 Browse Available Jobs -> Skill filter dropdown

   Safe to re-run: uses NOT EXISTS so it never creates a duplicate
   SKILL_NAME (SKILL_NAME already has a unique index — see
   01_Database_Schema.sql, modelBuilder.Entity<Skill> in AppDbContext.cs).

   NOTE: "Housekeeping Staff" and "Sorting Staff" (which appeared 3 times in
   the supplied image) are intentionally NOT duplicated — Housekeeping Staff
   already exists from the original seed, and Sorting Staff is inserted once.
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
