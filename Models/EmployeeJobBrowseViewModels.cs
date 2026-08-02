namespace Kaarigar.Models;

/// <summary>
/// Screen W-03 job card. Deliberately limited fields — an employee browsing
/// jobs should only see enough to decide whether to take the job: job title,
/// required skills, wage, start date/duration, and a distance/location
/// label. Full employer identity and phone number are only revealed
/// employer-side, in the applicants list (see JobApplicantService.GetJobDetailAsync).
/// </summary>
public class JobBrowseListItemViewModel
{
    public int JobPostId { get; set; }
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>Masked employer identity — business category only, e.g. "Construction", never the company name.</summary>
    public string EmployerDisplayName { get; set; } = "Registered Employer";

    public List<string> RequiredSkillNames { get; set; } = new();

    public decimal? HourlyWageAmt { get; set; }
    public DateTime? StartDt { get; set; }
    public int? DurationHourNbr { get; set; }

    /// <summary>City/area label only — never the exact street address or GPS pin.</summary>
    public string? LocationLabel { get; set; }
    public double? DistanceKm { get; set; }

    public DateTime PostedTs { get; set; }

    /// <summary>True if this job's required skills overlap the employee's own skills (or the job requires none). Informational badge only — jobs are never hidden based on this.</summary>
    public bool MatchesSkills { get; set; }

    /// <summary>True if this employee has already expressed interest — disables the button and shows status instead.</summary>
    public bool HasApplied { get; set; }
}

/// <summary>Screen W-03 page model: matched/filtered job list + filter state + approval gate.</summary>
public class JobBrowseViewModel
{
    public List<JobBrowseListItemViewModel> Jobs { get; set; } = new();

    /// <summary>All active skills, for the "Skill" filter dropdown.</summary>
    public List<(int SkillId, string SkillName)> SkillOptions { get; set; } = new();

    public string? SkillFilter { get; set; }
    public string? CityFilter { get; set; }
    public decimal? MinWage { get; set; }
    public decimal? MaxWage { get; set; }

    /// <summary>When true, jobs are additionally filtered down to only those matching the employee's skills + preferred radius. Default (false) shows every active job.</summary>
    public bool MatchesMyProfileOnly { get; set; }

    /// <summary>The employee's own preferred radius in km, or null = "Any". Shown so the UI can explain the automatic filtering.</summary>
    public int? PreferredRadiusKm { get; set; }

    /// <summary>False when the employee hasn't set a location yet — proximity filtering can't run, so we show all matching-skill jobs instead and prompt them to update their profile.</summary>
    public bool HasLocation { get; set; }

    /// <summary>Admin-approval gate (Screen W-02/Admin A-04). While false, jobs are visible but "Take Work" is disabled everywhere.</summary>
    public bool IsApproved { get; set; }
}
