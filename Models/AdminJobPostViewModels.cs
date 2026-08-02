namespace Kaarigar.Models;

/// <summary>Screen A-05: Manage Job Posts — one row of the job posts table.</summary>
public class AdminJobPostListItemViewModel
{
    public int JobPostId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string EmployerName { get; set; } = string.Empty;
    public int EmployerUserAccountId { get; set; }
    public string? CityName { get; set; }

    /// <summary>'ACTIVE' | 'PAUSED' | 'CLOSED'</summary>
    public string StatusCd { get; set; } = "ACTIVE";
    public DateTime DatePosted { get; set; }
    public int ApplicantsCount { get; set; }
}

/// <summary>Screen A-05 page model: the filtered/searched list + the filter state to re-populate the form.</summary>
public class ManageJobPostsViewModel
{
    public List<AdminJobPostListItemViewModel> JobPosts { get; set; } = new();
    public List<string> CityOptions { get; set; } = new();

    public string? SearchTerm { get; set; }
    public string? CityFilter { get; set; }
    /// <summary>"ACTIVE" | "PAUSED" | "CLOSED" | null (= all)</summary>
    public string? StatusFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>Screen A-05 "View Details" — full job post, read-only, from the Admin's perspective.</summary>
public class AdminJobPostDetailViewModel
{
    public int JobPostId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? DescriptionTxt { get; set; }
    public int RequiredWorkerNbr { get; set; }
    public decimal? HourlyWageAmt { get; set; }
    public DateTime? StartDt { get; set; }
    public int? DurationHourNbr { get; set; }
    public string? LocationAddressTxt { get; set; }
    public string? ContactNbr { get; set; }
    public string StatusCd { get; set; } = "ACTIVE";
    public DateTime DatePosted { get; set; }
    public List<string> RequiredSkillNames { get; set; } = new();

    public int EmployerUserAccountId { get; set; }
    public string EmployerName { get; set; } = string.Empty;
    public string EmployerContactNbr { get; set; } = string.Empty;
    public string? EmployerCityName { get; set; }

    public List<AdminApplicantSummaryViewModel> Applicants { get; set; } = new();
}

/// <summary>Read-only applicant row shown on the Admin's job detail page (no contact-reveal/OTP actions — that's employer-only).</summary>
public class AdminApplicantSummaryViewModel
{
    public string Name { get; set; } = string.Empty;
    public string StatusCd { get; set; } = "PENDING";
    public DateTime AppliedTs { get; set; }

    /// <summary>Human-friendly label for StatusCd, shown as a badge (matches the wording shown to the Kaarigar).</summary>
    public string StatusLabel =>
        StatusCd switch
        {
            "PENDING" => "Work Taken",
            "EMPLOYER_VIEWED" => "Profile Viewed",
            "EMPLOYER_CONTACTED" => "Employer Contacted",
            "JOB_STARTED" => "Job Started",
            "COMPLETED" => "Completed",
            "CANCELLED" => "Cancelled",
            _ => StatusCd.Replace("_", " "),
        };
}
