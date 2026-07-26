/* ============================================================================
   W-02 EMPLOYEE PROFILE — schema additions
   Adds an opt-out toggle for job-match WhatsApp notifications (Section 7 /
   Screen W-06 improvement). Defaults to enabled (1) so existing employees
   keep getting notified unless they turn it off.
   ============================================================================ */

ALTER TABLE EMPLOYEE_PROFILE ADD
    IS_NOTIFICATION_ENABLED_FL   BIT   NOT NULL
        CONSTRAINT DF_EMPLOYEE_PROFILE_IS_NOTIFICATION_ENABLED_FL DEFAULT (1);
GO
