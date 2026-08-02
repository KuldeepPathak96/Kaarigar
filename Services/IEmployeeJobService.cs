using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IEmployeeJobService
{
    Task<JobBrowseViewModel> BrowseAsync(
        int employeeUserAccountId, string? skillFilter, string? cityFilter, decimal? minWage, decimal? maxWage,
        bool matchesMyProfileOnly = false);

    /// <summary>"Take Work" — employee applies for the job (one click, cannot be undone). Inserts the JOB_APPLICATION row.</summary>
    Task<ServiceResult> TakeWorkAsync(int employeeUserAccountId, int jobPostId, string? ipAddress);

    // ── W-05: MY APPLICATIONS ────────────────────────────────────────────────

    Task<EmployeeApplicationsViewModel> GetMyApplicationsAsync(int employeeUserAccountId);

    // ── W-06: ENTER OTP (employer-generated, Kaarigar-entered) ───────────────

    /// <summary>
    /// Kaarigar enters the OTP the employer generated and shared in person.
    /// The expected OTP type is inferred from the application's current
    /// status: JOB_START while not yet started, SATISFACTION once started.
    /// </summary>
    Task<ServiceResult> VerifyOtpAsync(int employeeUserAccountId, int jobApplicationId, string otpCd);
}
