namespace Kaarigar.Models;

/// <summary>Screen A-04: Manage Employees — one row of the employees table.</summary>
public class EmployeeListItemViewModel
{
    public int UserAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactNbr { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public List<string> SkillNames { get; set; } = new();
    public string? CityName { get; set; }
    public DateTime DateRegistered { get; set; }

    /// <summary>true = Active, false = Blocked (USER_ACCOUNT.IS_ACTIVE_FL).</summary>
    public bool IsActiveFl { get; set; }

    public bool IsApprovedFl { get; set; }

    public int ApplicationsCount { get; set; }
}

/// <summary>Screen A-04 page model: the filtered/searched list + the filter state to re-populate the form.</summary>
public class ManageEmployeesViewModel
{
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
    public List<string> CityOptions { get; set; } = new();
    public List<string> SkillOptions { get; set; } = new();

    public string? SearchTerm { get; set; }
    public string? CityFilter { get; set; }
    public string? SkillFilter { get; set; }
    /// <summary>"ACTIVE" | "BLOCKED" | null (= all)</summary>
    public string? StatusFilter { get; set; }
}

/// <summary>Screen A-04 "View Details" — full employee profile, read-only.</summary>
public class EmployeeDetailViewModel
{
    public int UserAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactNbr { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public DateTime DateRegistered { get; set; }
    public DateTime? LastLoginTs { get; set; }
    public bool IsActiveFl { get; set; }
    public bool IsApprovedFl { get; set; }

    public List<string> SkillNames { get; set; } = new();
    public string? CityName { get; set; }
    public string? AreaAddressTxt { get; set; }
    public string? AddressTxt { get; set; }
    public int? PreferredRadiusNbr { get; set; }

    public int ApplicationsCount { get; set; }
    public int JobsCompletedCount { get; set; }

    /// <summary>Uploaded EMPLOYEE_DOCUMENT rows (ID_PROOF, RESUME) so Admin can view/verify them here.</summary>
    public List<EmployeeDocumentViewModel> Documents { get; set; } = new();
}

/// <summary>One uploaded EMPLOYEE_DOCUMENT row, for the Admin "View Details" screen.</summary>
public class EmployeeDocumentViewModel
{
    public int EmployeeDocumentId { get; set; }

    /// <summary>'ID_PROOF' | 'RESUME'</summary>
    public string DocumentTypeCd { get; set; } = string.Empty;

    /// <summary>'AADHAAR' | 'PAN' | 'VOTER_ID' | 'DRIVING_LICENSE' — null for RESUME rows.</summary>
    public string? DocumentSubtypeCd { get; set; }
    public string? IdLastFourDigitTxt { get; set; }

    public string OriginalFileNameTxt { get; set; } = string.Empty;

    /// <summary>Web-relative path (e.g. "/uploads/employee-documents/xyz.pdf") — link straight to this.</summary>
    public string FileUrl { get; set; } = string.Empty;

    public string? MimeTypeTxt { get; set; }
    public DateTime UploadedTs { get; set; }

    /// <summary>'PENDING' | 'APPROVED' | 'REJECTED'</summary>
    public string ReviewStatusCd { get; set; } = "PENDING";

    /// <summary>Set only when ReviewStatusCd is REJECTED.</summary>
    public string? RejectionReasonTxt { get; set; }
}
