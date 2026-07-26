using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Kaarigar.Models;

/// <summary>
/// ViewModel for the registration form.
/// Maps to: USER_ACCOUNT, EMPLOYER_PROFILE, EMPLOYEE_PROFILE, EMPLOYEE_SKILL
/// </summary>
public class RegisterModel
{
    // ── USER_ACCOUNT fields ──────────────────────────────────────────────────

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(75, ErrorMessage = "First name cannot exceed 75 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(75, ErrorMessage = "Last name cannot exceed 75 characters.")]
    public string? LastName { get; set; }

    /// <summary>
    /// Maps to USER_ACCOUNT.CONTACT_NBR — must is unique, 10 digits, used for OTP & login.
    /// </summary>
    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a valid 10-digit mobile number.")]
    public string ContactNbr { get; set; } = string.Empty;

    /// <summary>
    /// Maps to USER_ACCOUNT.EMAIL_ID — optional, unique index in DB.
    /// </summary>
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150)]
    public string? EmailId { get; set; }

    /// <summary>
    /// Maps to USER_ACCOUNT.ROLE_CD — 'EMPLOYER' | 'EMPLOYEE'
    /// </summary>
    [Required]
    public string RoleCd { get; set; } = "EMPLOYER";

    // ── Password (hashed via BCrypt before storing in PASSWORD_HASH_TXT) ────

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // ── EMPLOYER_PROFILE fields ──────────────────────────────────────────────

    /// <summary>Maps to EMPLOYER_PROFILE.COMPANY_NAME</summary>
    [StringLength(200)]
    public string? CompanyName { get; set; }

    // ── EMPLOYEE_PROFILE & EMPLOYER_PROFILE shared location fields ───────────

    /// <summary>Maps to EMPLOYER_PROFILE.CITY_NAME / EMPLOYEE_PROFILE.CITY_NAME</summary>
    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    public string? CityName { get; set; }

    /// <summary>Maps to EMPLOYER_PROFILE.AREA_ADDRESS_TXT / EMPLOYEE_PROFILE.AREA_ADDRESS_TXT</summary>
    [StringLength(300)]
    public string? AreaAddressTxt { get; set; }

    // ── EMPLOYEE_PROFILE specific ────────────────────────────────────────────

    /// <summary>
    /// Maps to EMPLOYEE_PROFILE.PREFERRED_RADIUS_NBR — 5 | 10 | 25 | 50 | NULL (Any)
    /// </summary>
    public int? PreferredRadiusNbr { get; set; }

    // ── EMPLOYEE_SKILL ───────────────────────────────────────────────────────

    /// <summary>Maps to EMPLOYEE_SKILL.SKILL_ID for each selected skill.</summary>
    public HashSet<int> SelectedSkillIds { get; set; } = new();

    // ── EMPLOYEE_DOCUMENT (initial capture, upload handled post-registration) ─

    /// <summary>
    /// Maps to EMPLOYEE_DOCUMENT.DOCUMENT_SUBTYPE_CD
    /// Allowed: 'AADHAAR' | 'PAN' | 'VOTER_ID' | 'DRIVING_LICENSE'
    /// </summary>
    [StringLength(30)]
    public string? DocumentSubtypeCd { get; set; }

    /// <summary>Maps to EMPLOYEE_DOCUMENT.ID_LAST_FOUR_DIGIT_TXT</summary>
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Enter the last 4 digits only.")]
    [StringLength(4)]
    public string? IdLastFourDigitTxt { get; set; }

    // ── UI only ──────────────────────────────────────────────────────────────

    [Required(ErrorMessage = "You must agree to the Terms of Use to register.")]
    public bool AgreeToTerms { get; set; }
}