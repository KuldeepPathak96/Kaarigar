namespace Kaarigar.Models;

/// <summary>Screen A-03: Manage Employers — one row of the employers table.</summary>
public class EmployerListItemViewModel
{
    public int UserAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactNbr { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public string? CityName { get; set; }
    public DateTime DateRegistered { get; set; }

    /// <summary>true = Active, false = Blocked (USER_ACCOUNT.IS_ACTIVE_FL).</summary>
    public bool IsActiveFl { get; set; }

    /// <summary>Admin-approval state — shown alongside Status so Admin can tell "pending" apart from "blocked".</summary>
    public bool IsApprovedFl { get; set; }

    public int JobsPostedCount { get; set; }
}

/// <summary>Screen A-03 page model: the filtered/searched list + the filter state to re-populate the form.</summary>
public class ManageEmployersViewModel
{
    public List<EmployerListItemViewModel> Employers { get; set; } = new();
    public List<string> CityOptions { get; set; } = new();

    public string? SearchTerm { get; set; }
    public string? CityFilter { get; set; }
    /// <summary>"ACTIVE" | "BLOCKED" | null (= all)</summary>
    public string? StatusFilter { get; set; }
}

/// <summary>Screen A-03 "View Details" — full employer profile, read-only.</summary>
public class EmployerDetailViewModel
{
    public int UserAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactNbr { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public DateTime DateRegistered { get; set; }
    public DateTime? LastLoginTs { get; set; }
    public bool IsActiveFl { get; set; }
    public bool IsApprovedFl { get; set; }

    public string? CompanyName { get; set; }
    public string? ContactPersonName { get; set; }
    public string? CityName { get; set; }
    public string? AreaAddressTxt { get; set; }
    public string? AddressTxt { get; set; }
    public string? BusinessCategoryName { get; set; }
    public string? BusinessProofTypeCd { get; set; }
    public string? BusinessProofNumberTxt { get; set; }
    public string BusinessProofReviewStatusCd { get; set; } = "PENDING";

    /// <summary>Original uploaded file name for the business proof document, so Admin can see/verify it here.</summary>
    public string? BusinessProofOriginalFileNameTxt { get; set; }

    /// <summary>Web-relative path (e.g. "/uploads/business-proofs/xyz.pdf") — link straight to this.</summary>
    public string? BusinessProofFileUrl { get; set; }

    /// <summary>Employer's logo, if uploaded — shown alongside the profile, not a verification document.</summary>
    public string? LogoFileUrl { get; set; }

    public int JobsPostedCount { get; set; }
    public int ActiveJobsCount { get; set; }
}
