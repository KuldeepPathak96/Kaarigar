/* ============================================================================
   E-03 POST A JOB — job timing additions
   ----------------------------------------------------------------------------
   Adds a time-of-day to the job's existing START_DT / DURATION_DAY_NBR pair,
   so a full START datetime and END datetime can be calculated:

       StartDateTime = START_DT + START_TIME
       EndDt         = START_DT + (DURATION_DAY_NBR - 1) days
       EndDateTime   = EndDt    + END_TIME

   (EndDt/EndDateTime are computed in the application layer — see
   JobPost.EndDt / JobPost.StartDateTime / JobPost.EndDateTime in
   Models/JobModels.cs — no generated column needed here.)
   ============================================================================ */
ALTER TABLE JOB_POST ADD
    START_TIME   TIME(0)   NULL,
    END_TIME     TIME(0)   NULL;
GO
