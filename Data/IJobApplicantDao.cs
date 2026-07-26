using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data Access Object interface for Screens E-05 (Job Detail & Applicants)
/// and E-06 (Employee Profile View) — both read JOB_APPLICATION rows joined
/// against the employee's profile/skills, and handle the OTP verification
/// step described in Section 8 (OTP_RECORD).
/// </summary>
public interface IJobApplicantDao
{
    /// <summary>Loads a job post (with its required skills), only if it belongs to the given employer.</summary>
    Task<JobPost?> GetJobPostWithSkillsForEmployerAsync(int jobPostId, int employerUserAccountId);

    /// <summary>All applications against this job, newest first.</summary>
    Task<List<JobApplication>> GetApplicationsForJobAsync(int jobPostId);

    /// <summary>Loads a single application, only if it belongs to a job post owned by the given employer.</summary>
    Task<JobApplication?> GetApplicationForEmployerAsync(int jobApplicationId, int employerUserAccountId);

    /// <summary>EMPLOYEE_PROFILE rows for the given set of employee USER_ACCOUNT_IDs, keyed by id.</summary>
    Task<Dictionary<int, EmployeeProfile>> GetEmployeeProfilesAsync(List<int> employeeUserAccountIds);

    /// <summary>Skill names for the given set of employee USER_ACCOUNT_IDs, keyed by id.</summary>
    Task<Dictionary<int, List<string>>> GetEmployeeSkillNamesAsync(List<int> employeeUserAccountIds);

    /// <summary>
    /// Advances the application's STATUS_CD, but never moves it backwards
    /// (e.g. won't downgrade JOB_STARTED back to EMPLOYER_VIEWED).
    /// </summary>
    Task AdvanceStatusAsync(int jobApplicationId, string newStatusCd);

    /// <summary>The most recent, unused, unexpired OTP for this employee+job+type matching the given code.</summary>
    Task<OtpRecord?> GetValidOtpAsync(int employeeUserAccountId, int jobPostId, string otpTypeCd, string otpCd);

    /// <summary>The current live (unused, unexpired) OTP of the given type for this employee+job, if any — so it can be re-displayed after a redirect.</summary>
    Task<OtpRecord?> GetActiveOtpAsync(int employeeUserAccountId, int jobPostId, string otpTypeCd);

    Task MarkOtpUsedAsync(int otpRecordId);

    /// <summary>Marks any earlier, still-unused OTPs of the given type for this employee+job as used, so only the freshest OTP is valid.</summary>
    Task InvalidatePreviousOtpsAsync(int employeeUserAccountId, int jobPostId, string otpTypeCd);

    Task<OtpRecord> CreateOtpRecordAsync(OtpRecord otp);

    Task<KaarigarRating?> GetRatingForApplicationAsync(int jobApplicationId);
    Task<KaarigarRating> CreateRatingAsync(KaarigarRating rating);
}
