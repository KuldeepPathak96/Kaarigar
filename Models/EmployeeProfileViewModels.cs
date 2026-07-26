using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Kaarigar.Models;

/// <summary>
/// Screen W-02: Employee Profile — view + edit form.
/// Backed by USER_ACCOUNT (Name/Mobile/Email login fields) + EMPLOYEE_PROFILE
/// (location/radius) + EMPLOYEE_SKILL (multi-select) + EMPLOYEE_DOCUMENT
/// (ID Proof and CV/Resume uploads).
/// </summary>
public class EmployeeProfileViewModel
{
    // ── USER_ACCOUNT-backed fields ──────────────────────────────────────────

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(75)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(75)]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a valid 10-digit mobile number.")]
    public string ContactNbr { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150)]
    public string? EmailId { get; set; }

    // ── EMPLOYEE_PROFILE-backed fields ──────────────────────────────────────

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    public string? CityName { get; set; }

    [StringLength(300)]
    public string? AreaAddressTxt { get; set; }

    /// <summary>Flat/building number, street, landmark — separate from City/Area, which stay picker-driven.</summary>
    [StringLength(400)]
    public string? AddressTxt { get; set; }

    /// <summary>Set by the "Use My Current Location" button before submit. Optional — City/Address can still be typed manually.</summary>
    [Range(-90, 90)]
    public decimal? LatitudeNbr { get; set; }

    [Range(-180, 180)]
    public decimal? LongitudeNbr { get; set; }

    /// <summary>5 | 10 | 25 | 50 | null = Any (km).</summary>
    public int? PreferredRadiusNbr { get; set; }

    /// <summary>Opt-out toggle for job-match WhatsApp alerts. Defaults to on for new profiles.</summary>
    public bool IsNotificationEnabledFl { get; set; } = true;

    // ── EMPLOYEE_SKILL (multi-select) ────────────────────────────────────────

    /// <summary>Minimum 1 skill required, same rule as registration.</summary>
    [MinLength(1, ErrorMessage = "Please select at least one skill.")]
    public HashSet<int> SelectedSkillIds { get; set; } = new();

    /// <summary>Populated by the controller for the checkbox list, grouped by Skill.CategoryName. Not bound from the form.</summary>
    public List<Skill> SkillOptions { get; set; } = new();

    // ── ID Proof upload (EMPLOYEE_DOCUMENT, DOCUMENT_TYPE_CD = 'ID_PROOF') ──

    /// <summary>'AADHAAR' | 'PAN' | 'VOTER_ID' | 'DRIVING_LICENSE' — the employee's own selection of which ID they're uploading.</summary>
    [Required(ErrorMessage = "Please select the type of ID proof you're uploading.")]
    public string? IdProofTypeCd { get; set; }

    /// <summary>New ID proof file (required on first-time upload only).</summary>
    public IFormFile? IdProofFile { get; set; }

    /// <summary>Existing uploaded ID proof file name, shown for reference. Null if none uploaded yet.</summary>
    public string? ExistingIdProofFileName { get; set; }

    /// <summary>Current Admin review status of the uploaded ID proof: PENDING | APPROVED | REJECTED.</summary>
    public string? IdProofReviewStatusCd { get; set; }

    // ── CV / Resume upload (EMPLOYEE_DOCUMENT, DOCUMENT_TYPE_CD = 'RESUME') ─

    /// <summary>New CV/resume file (optional).</summary>
    public IFormFile? CvFile { get; set; }

    /// <summary>Existing uploaded CV file name, shown for reference. Null if none uploaded yet.</summary>
    public string? ExistingCvFileName { get; set; }

    public string? CvReviewStatusCd { get; set; }

    // ── Profile completeness (Screen W-02: "encourage full profile for better matches") ─

    /// <summary>0–100. Computed from how many of: location, radius, skills, ID proof, CV are filled in.</summary>
    public int ProfileCompletenessPercent { get; set; }
}
