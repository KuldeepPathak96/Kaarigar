/* ============================================================================
   EMPLOYEE_PROFILE / EMPLOYER_PROFILE — Address field addition
   ============================================================================
   Adds a free-text street/building address, separate from CITY_NAME and
   AREA_ADDRESS_TXT (which remain as-is, backed by the CITY/AREA pickers
   added in 08_City_Area_Schema_And_Seed.sql). ADDRESS_TXT is for
   flat/building number, street name, landmark — the detail that doesn't
   belong in a City dropdown or Area type-ahead.
*/

ALTER TABLE EMPLOYEE_PROFILE ADD
    ADDRESS_TXT  NVARCHAR(400) NULL;
GO

ALTER TABLE EMPLOYER_PROFILE ADD
    ADDRESS_TXT  NVARCHAR(400) NULL;
GO
