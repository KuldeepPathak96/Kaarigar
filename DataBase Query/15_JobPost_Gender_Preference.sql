/* ============================================================================
   JOB_POST — Gender Preference for job posts
   ============================================================================
   Adds an optional Gender Preference field to a job post, so employers can
   specify the shift is open to ANY worker, MALE only, or FEMALE only.
   Defaults to 'ANY' for all existing rows so nothing already posted is
   affected.

   This script is safe to re-run: it checks whether the column already
   exists before adding it, so running it twice won't error out.
*/

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JOB_POST') AND name = 'GENDER_PREFERENCE_CD')
    ALTER TABLE JOB_POST ADD GENDER_PREFERENCE_CD NVARCHAR(10) NOT NULL DEFAULT 'ANY';
GO
