namespace Kaarigar.Models;

/// <summary>
/// Screen W-05 row: one job the employee has expressed interest in, plus
/// where it currently sits in the PENDING -> EMPLOYER_VIEWED ->
/// EMPLOYER_CONTACTED -> JOB_STARTED -> COMPLETED pipeline (see
/// JobApplication.StatusCd / JobApplicantDao.StatusOrder).
/// </summary>
public class EmployeeApplicationListItemViewModel
{
    public int JobApplicationId { get; set; }
    public int JobPostId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    /// <summary>Masked employer identity — business category only, never the company name (same rule as W-03).</summary>
    public string EmployerDisplayName { get; set; } = "Registered Employer";

    public decimal? HourlyWageAmt { get; set; }
    public string? LocationLabel { get; set; }

    /// <summary>Clickable Google Maps link for the job site, when a location was captured.</summary>
    public string? MapUrl { get; set; }

    public DateTime AppliedTs { get; set; }

    /// <summary>PENDING | EMPLOYER_VIEWED | EMPLOYER_CONTACTED | JOB_STARTED | COMPLETED</summary>
    public string StatusCd { get; set; } = "PENDING";

    /// <summary>Human-friendly label for StatusCd, shown as a badge.</summary>
    public string StatusLabel =>
        StatusCd switch
        {
            "PENDING" => "Work Taken",
            "EMPLOYER_VIEWED" => "Profile viewed",
            "EMPLOYER_CONTACTED" => "Employer contacted you",
            "JOB_STARTED" => "Job started",
            "COMPLETED" => "Job completed",
            _ => StatusCd,
        };

    /// <summary>
    /// True while the job hasn't started yet — the Kaarigar should enter the
    /// Job Starting OTP the employer shares in person once they reach the site.
    /// </summary>
    public bool AwaitingJobStartOtp => StatusCd is "PENDING" or "EMPLOYER_VIEWED" or "EMPLOYER_CONTACTED";

    /// <summary>
    /// True once the job has started — the Kaarigar should enter the
    /// Satisfaction OTP the employer shares in person once the job is done.
    /// </summary>
    public bool AwaitingSatisfactionOtp => StatusCd == "JOB_STARTED";
}

/// <summary>Screen W-05 page model.</summary>
public class EmployeeApplicationsViewModel
{
    public List<EmployeeApplicationListItemViewModel> Applications { get; set; } = new();
}
