using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kaarigar.Models;

/// <summary>
/// Screen E-03: Post a Job — create/edit form. Used for both the initial
/// post and later edits (Screen E-04's "Edit" quick action reuses this).
/// </summary>
public class PostJobViewModel
{
    /// <summary>Null when creating a new job; set when editing an existing one.</summary>
    public int? JobPostId { get; set; }

    /// <summary>
    /// Job Title — chosen from the Skill list (dropdown), not typed freely.
    /// Stores the selected skill's name directly, same as before, so no
    /// schema change was needed. Job Category has been removed: Job Title +
    /// Required Skills together now cover what Category used to.
    /// </summary>
    [Required(ErrorMessage = "Job title is required.")]
    [StringLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>Populated by the controller — same Skill list as SkillOptions, but Value=SkillName for this dropdown.</summary>
    public List<SelectListItem> JobTitleOptions { get; set; } = new();

    [Required(ErrorMessage = "Number of workers required is required.")]
    [Range(1, 500, ErrorMessage = "Enter a number of workers between 1 and 500.")]
    public int RequiredWorkerNbr { get; set; } = 1;

    [StringLength(4000)]
    public string? DescriptionTxt { get; set; }

    /// <summary>Selected SKILL.SKILL_ID values from the multi-select.</summary>
    public List<int> SelectedSkillIds { get; set; } = new();

    /// <summary>Populated by the controller for the multi-select checkboxes.</summary>
    public List<SelectListItem> SkillOptions { get; set; } = new();

    /// <summary>Chosen from the admin-editable HOURLY_RATE_OPTION dropdown — not typed freely.</summary>
    [Range(0, 100000, ErrorMessage = "Please select a valid hourly rate.")]
    public decimal? HourlyWageAmt { get; set; }

    /// <summary>Populated by the controller from active HourlyRateOption rows.</summary>
    public List<SelectListItem> HourlyRateOptionsList { get; set; } = new();

    [Required(ErrorMessage = "Job start date is required.")]
    [DataType(DataType.Date)]
    public DateTime? StartDt { get; set; }

    /// <summary>The single moment the job starts (date + time of day).</summary>
    [Required(ErrorMessage = "Job start time is required.")]
    [DataType(DataType.Time)]
    public TimeSpan? StartTime { get; set; }

    /// <summary>Total job duration in hours — end is calculated from this, not entered separately.</summary>
    [Required(ErrorMessage = "Job duration is required.")]
    [Range(1, 720, ErrorMessage = "Enter a duration between 1 and 720 hours (30 days).")]
    public int? DurationHourNbr { get; set; }

    /// <summary>Calculated read-only preview shown on the form: StartDt + StartTime.</summary>
    public DateTime? StartDateTime => StartDt.HasValue ? StartDt.Value.Date + (StartTime ?? TimeSpan.Zero) : null;

    /// <summary>Calculated read-only preview shown on the form: StartDateTime + DurationHourNbr hours.</summary>
    public DateTime? EndDateTime => StartDateTime.HasValue && DurationHourNbr.HasValue
        ? StartDateTime.Value.AddHours(DurationHourNbr.Value)
        : null;

    /// <summary>Mandatory — used for employee proximity matching. Prefilled from the employer's Profile address, but still editable.</summary>
    [Required(ErrorMessage = "Job location is required for matching nearby workers.")]
    [StringLength(300)]
    public string? LocationAddressTxt { get; set; }

    /// <summary>Set by the "Use My Current Location" GPS button (optional assist).</summary>
    [Range(-90, 90)]
    public decimal? LatitudeNbr { get; set; }

    [Range(-180, 180)]
    public decimal? LongitudeNbr { get; set; }

    [Required(ErrorMessage = "Contact number is required.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a valid 10-digit contact number.")]
    public string ContactNbr { get; set; } = string.Empty;

    /// <summary>'ACTIVE' | 'PAUSED' — only meaningful when editing; new posts always start ACTIVE.</summary>
    public string StatusCd { get; set; } = "ACTIVE";
}

/// <summary>
/// Screen E-04: My Job Posts — one row per job card in the list.
/// Job Category removed — cards show Job Title + Skills only.
/// </summary>
public class JobPostListItemViewModel
{
    public int JobPostId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public List<string> RequiredSkillNames { get; set; } = new();
    public DateTime DatePosted { get; set; }
    public int ApplicationsCount { get; set; }
    public int RequiredWorkerNbr { get; set; }
    public int FilledWorkerNbr { get; set; }
    public int RemainingWorkerNbr => Math.Max(0, RequiredWorkerNbr - FilledWorkerNbr);
    public string StatusCd { get; set; } = "ACTIVE"; // ACTIVE | PAUSED | CLOSED
}
