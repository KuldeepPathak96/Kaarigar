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
    /// <summary>
    /// Auto-set server-side from the selected Work Category's name — no
    /// longer chosen directly via a dropdown (see WorkCategoryId below).
    /// Kept as a plain string since JOB_POST.JOB_TITLE is read in many
    /// places (Browse, Job Detail, invoices) for display.
    /// </summary>
    [StringLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// "Worker Category" — same Work Category cascade used on Registration
    /// and the Worker's own Profile page. Determines which Skills are
    /// offered below, and JobTitle is derived from this on save.
    /// </summary>
    [Required(ErrorMessage = "Please select a worker category.")]
    public int? WorkCategoryId { get; set; }

    [Required(ErrorMessage = "Number of workers required is required.")]
    [Range(1, 500, ErrorMessage = "Enter a number of workers between 1 and 500.")]
    public int RequiredWorkerNbr { get; set; } = 1;

    [StringLength(4000)]
    public string? DescriptionTxt { get; set; }

    /// <summary>Optional — shown to workers on the job card / My Applications: dress code, footwear, uniform, items to bring, etc. Kept separate from DescriptionTxt so it reads as a short, scannable checklist rather than being buried in the general description.</summary>
    [StringLength(1000, ErrorMessage = "Keep the note under 1000 characters.")]
    public string? NoteForWorkerTxt { get; set; }

    /// <summary>Selected SKILL.SKILL_ID values, filtered by WorkCategoryId (see work-category-picker.js).</summary>
    public List<int> SelectedSkillIds { get; set; } = new();

    /// <summary>Chosen from the admin-editable HOURLY_RATE_OPTION dropdown — not typed freely.</summary>
    [Required(ErrorMessage = "Per hour rate is required.")]
    [Range(0.01, 100000, ErrorMessage = "Please select a valid hourly rate.")]
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

    /// <summary>'ANY' | 'MALE' | 'FEMALE' — optional gender preference for this shift.</summary>
    public string GenderPreferenceCd { get; set; } = "ANY";

    /// <summary>'ACTIVE' | 'PAUSED' — only meaningful when editing; new posts always start ACTIVE.</summary>
    public string StatusCd { get; set; } = "ACTIVE";

    // ── REPEAT SHIFT ─────────────────────────────────────────────────────────
    // Populated when this form was opened via "Repeat Shift" on My Job Posts.
    // Every other field above is prefilled from the original job; only
    // StartDt/StartTime are left blank for the employer to fill in fresh.

    /// <summary>The original JOB_POST.JOB_POST_ID this form is repeating. Null for a normal new/edit post.</summary>
    public int? RepeatFromJobPostId { get; set; }

    /// <summary>Original job's title/date, shown as a banner on the form so the employer knows what they're repeating. Not submitted — display only.</summary>
    public string? RepeatFromSummaryTxt { get; set; }

    /// <summary>
    /// "Repeat Staff" checkbox. When true, workers who completed the
    /// original job get a priority notification window (see
    /// JobNotificationBackgroundService) before the shift is broadcast to
    /// everyone else who matches.
    /// </summary>
    public bool RepeatStaffFl { get; set; }
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
