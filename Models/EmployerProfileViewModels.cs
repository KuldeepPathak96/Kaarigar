using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kaarigar.Models;

/// <summary>
/// Screen E-02: Employer Profile — view + edit form.
/// Backed by USER_ACCOUNT (ContactPersonName/ContactNbr/EmailId login fields)
/// + EMPLOYER_PROFILE (company/location/proof fields).
/// </summary>
public class EmployerProfileViewModel
{
    // ── USER_ACCOUNT-backed fields ──────────────────────────────────────────

    [Required(ErrorMessage = "Contact person name is required.")]
    [StringLength(150)]
    public string ContactPersonName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a valid 10-digit mobile number.")]
    public string ContactNbr { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150)]
    public string? EmailId { get; set; }

    // ── EMPLOYER_PROFILE-backed fields ──────────────────────────────────────

    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    public string? CityName { get; set; }

    [StringLength(300)]
    public string? AreaAddressTxt { get; set; }

    /// <summary>Flat/building number, street, landmark — separate from City/Area, which stay picker-driven.</summary>
    [StringLength(400)]
    public string? AddressTxt { get; set; }

    /// <summary>
    /// Set by the "Use My Current Location" button (browser Geolocation API)
    /// before the form is submitted. Optional — a manually typed
    /// City/Address is still accepted without GPS.
    /// </summary>
    [Range(-90, 90)]
    public decimal? LatitudeNbr { get; set; }

    [Range(-180, 180)]
    public decimal? LongitudeNbr { get; set; }

    /// <summary>Selected BUSINESS_CATEGORY.BUSINESS_CATEGORY_ID.</summary>
    [Required(ErrorMessage = "Please select a business category.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a business category.")]
    public int? BusinessCategoryId { get; set; }

    /// <summary>Populated by the controller for the dropdown. Not bound from the form.</summary>
    public List<SelectListItem> BusinessCategoryOptions { get; set; } = new();

    // ── Logo upload ──────────────────────────────────────────────────────────

    /// <summary>Existing logo path, shown as a preview. Null if none uploaded yet.</summary>
    public string? ExistingLogoPath { get; set; }

    /// <summary>New logo file selected by the user (optional — keeps the old one if not provided).</summary>
    public IFormFile? LogoFile { get; set; }

    // ── Business proof upload (GST / Gumasthadhara) ──────────────────────────

    /// <summary>'GST' | 'GUMASTHA'</summary>
    [Required(ErrorMessage = "Please select the type of business proof.")]
    public string? BusinessProofTypeCd { get; set; }

    [Required(ErrorMessage = "Please enter the GST / Gumasthadhara number.")]
    [StringLength(30)]
    public string? BusinessProofNumberTxt { get; set; }

    /// <summary>Existing uploaded proof file name, shown for reference. Null if none uploaded yet.</summary>
    public string? ExistingProofFileName { get; set; }

    /// <summary>Current admin review status of the uploaded proof: PENDING | APPROVED | REJECTED.</summary>
    public string? ProofReviewStatusCd { get; set; }

    /// <summary>New proof file (PDF/JPG/PNG) selected by the user. Required only on first-time upload.</summary>
    public IFormFile? ProofFile { get; set; }
}

/// <summary>
/// Admin-only screen: manage the BUSINESS_CATEGORY master list that feeds
/// the Employer Profile's "Business Category" dropdown.
/// </summary>
public class BusinessCategoryAdminViewModel
{
    public List<BusinessCategory> Categories { get; set; } = new();

    /// <summary>Bound from the "Add new category" mini-form at the top of the page.</summary>
    [StringLength(150)]
    public string? NewCategoryName { get; set; }
}

/// <summary>Admin screen: manage the Hourly Rate dropdown used on Post a Job (Screen E-03).</summary>
public class HourlyRateOptionAdminViewModel
{
    public List<HourlyRateOption> RateOptions { get; set; } = new();
}
