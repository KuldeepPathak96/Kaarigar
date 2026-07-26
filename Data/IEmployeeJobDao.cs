using Kaarigar.Models;

namespace Kaarigar.Data;

public interface IEmployeeJobDao
{
    Task<bool> IsEmployeeApprovedAsync(int employeeUserAccountId);

    Task<EmployeeProfile?> GetEmployeeProfileAsync(int employeeUserAccountId);

    Task<List<int>> GetEmployeeSkillIdsAsync(int employeeUserAccountId);

    /// <summary>All ACTIVE job posts with their required skills and employer's business category loaded.</summary>
    Task<List<JobPost>> GetActiveJobPostsAsync();

    Task<Dictionary<int, string?>> GetEmployerBusinessCategoryNamesAsync(IEnumerable<int> employerUserAccountIds);

    /// <summary>Job post IDs this employee has already expressed interest in.</summary>
    Task<HashSet<int>> GetAppliedJobPostIdsAsync(int employeeUserAccountId);

    Task<List<(int SkillId, string SkillName)>> GetActiveSkillOptionsAsync();

    Task<JobPost?> GetActiveJobPostByIdAsync(int jobPostId);

    Task<bool> HasAppliedAsync(int employeeUserAccountId, int jobPostId);

    /// <summary>Inserts the JOB_APPLICATION row representing "Express Interest" (Screen W-04). One-click, cannot be undone.</summary>
    Task InsertApplicationAsync(int employeeUserAccountId, int jobPostId, string? ipAddress);

    // ── W-05: MY APPLICATIONS ────────────────────────────────────────────────

    /// <summary>All of this employee's applications (with JobPost loaded), newest first.</summary>
    Task<List<JobApplication>> GetApplicationsForEmployeeAsync(int employeeUserAccountId);

    /// <summary>Loads a single application, only if it belongs to the given employee, with JobPost + UserAccount loaded.</summary>
    Task<JobApplication?> GetApplicationForEmployeeAsync(int jobApplicationId, int employeeUserAccountId);

    // ── W-06: ENTER OTP (employer-generated, Kaarigar-entered) ───────────────

    /// <summary>The most recent, unused, unexpired OTP of the given type for this employee+job matching the given code.</summary>
    Task<OtpRecord?> GetValidOtpAsync(int employeeUserAccountId, int jobPostId, string otpTypeCd, string otpCd);

    Task MarkOtpUsedAsync(int otpRecordId);

    /// <summary>
    /// Advances the application's STATUS_CD, but never moves it backwards
    /// (e.g. won't downgrade JOB_STARTED back to EMPLOYER_VIEWED).
    /// </summary>
    Task AdvanceStatusAsync(int jobApplicationId, string newStatusCd);
}
