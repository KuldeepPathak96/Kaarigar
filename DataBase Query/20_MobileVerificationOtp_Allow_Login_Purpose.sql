/* ============================================================================
   20_MobileVerificationOtp_Allow_Login_Purpose.sql
   ============================================================================
   PURPOSE
   -------
   AuthService.cs / LoginOtpDao.cs implement a "Login via OTP" flow that
   inserts MOBILE_VERIFICATION_OTP rows with PURPOSE_CD = 'LOGIN'. The
   original CHECK constraint (01_Database_Schema.sql) only allowed
   'REGISTRATION' and 'FORGOT_PASSWORD', so every login-OTP request currently
   fails with:

     SqlException: The INSERT statement conflicted with the CHECK constraint
     "CK_MOBILE_VERIFICATION_OTP_PURPOSE_CD" ...

   This script drops and recreates that constraint to also allow 'LOGIN'.
   SQL Server has no ALTER CHECK CONSTRAINT ... ADD VALUE, so a CHECK
   constraint can only be widened by dropping and recreating it — this is
   safe here because it doesn't touch existing data, it only relaxes what
   future inserts are allowed to contain.

   Idempotent — safe to re-run.
   ============================================================================ */

SET NOCOUNT ON;

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_MOBILE_VERIFICATION_OTP_PURPOSE_CD'
)
BEGIN
    ALTER TABLE dbo.MOBILE_VERIFICATION_OTP
        DROP CONSTRAINT CK_MOBILE_VERIFICATION_OTP_PURPOSE_CD;
END;
GO

ALTER TABLE dbo.MOBILE_VERIFICATION_OTP
    ADD CONSTRAINT CK_MOBILE_VERIFICATION_OTP_PURPOSE_CD
    CHECK (PURPOSE_CD IN ('REGISTRATION', 'FORGOT_PASSWORD', 'LOGIN'));
GO

-- Validation: confirm the new constraint definition
SELECT cc.name AS ConstraintName, cc.definition
FROM sys.check_constraints cc
WHERE cc.name = 'CK_MOBILE_VERIFICATION_OTP_PURPOSE_CD';
