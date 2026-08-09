using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/// <summary>
/// Maps to JOB_POST table. A job listing created by an employer.
/// NOTE: the FK column is EMPLOYER_USER_ACCOUNT_ID, not USER_ACCOUNT_ID.
/// </summary>
[Table("JOB_POST")]
public class JobPost
{
    [Key]
    [Column("JOB_POST_ID")]
    public int JobPostId { get; set; }

    [Required]
    [Column("EMPLOYER_USER_ACCOUNT_ID")]
    public int EmployerUserAccountId { get; set; }

    [Required]
    [Column("JOB_TITLE")]
    [StringLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// Deprecated — kept nullable for historical rows. Job cards now show
    /// Job Title (chosen from Skill) + Required Skills instead of a category.
    /// </summary>
    [Column("JOB_CATEGORY_CD")]
    [StringLength(50)]
    public string? JobCategoryCd { get; set; }

    [Column("DESCRIPTION_TXT")]
    public string? DescriptionTxt { get; set; }

    [Column("REQUIRED_WORKER_NBR")]
    public int RequiredWorkerNbr { get; set; } = 1;

    /// <summary>Deprecated — see HourlyWageAmt. Left nullable for historical rows.</summary>
    [Column("DAILY_WAGE_AMT")]
    public decimal? DailyWageAmt { get; set; }

    /// <summary>Hourly rate, picked from the admin-editable HOURLY_RATE_OPTION list.</summary>
    [Column("HOURLY_WAGE_AMT")]
    public decimal? HourlyWageAmt { get; set; }

    [Column("START_DT")]
    public DateTime? StartDt { get; set; }

    /// <summary>Deprecated — see DurationHourNbr. Left nullable for historical rows.</summary>
    [Column("DURATION_DAY_NBR")]
    public int? DurationDayNbr { get; set; }

    /// <summary>Total job duration in hours — replaces day-count + separate start/end time-of-day.</summary>
    [Column("DURATION_HOUR_NBR")]
    public int? DurationHourNbr { get; set; }

    /// <summary>Time of day the job starts on StartDt (e.g. 09:00 AM).</summary>
    [Column("START_TIME")]
    public TimeSpan? StartTime { get; set; }

    /// <summary>Deprecated — end is now calculated from StartDateTime + DurationHourNbr. Left nullable for historical rows.</summary>
    [Column("END_TIME")]
    public TimeSpan? EndTime { get; set; }

    /// <summary>Calculated: StartDt + StartTime. Null when either part is missing.</summary>
    [NotMapped]
    public DateTime? StartDateTime => StartDt.HasValue ? StartDt.Value.Date + (StartTime ?? TimeSpan.Zero) : null;

    /// <summary>Calculated: StartDateTime + DurationHourNbr hours. Null when either part is missing.</summary>
    [NotMapped]
    public DateTime? EndDateTime => StartDateTime.HasValue && DurationHourNbr.HasValue
        ? StartDateTime.Value.AddHours(DurationHourNbr.Value)
        : null;

    /// <summary>
    /// Calculated: a clickable Google Maps link for the job site — built from
    /// LatitudeNbr/LongitudeNbr when available (exact pin), otherwise falling
    /// back to a text search on LocationAddressTxt, so employees can always
    /// tap through and see the location on a map. Null only when neither
    /// GPS coordinates nor an address are present.
    /// </summary>
    [NotMapped]
    public string? GoogleMapsUrl
    {
        get
        {
            if (LatitudeNbr.HasValue && LongitudeNbr.HasValue)
            {
                var lat = LatitudeNbr.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var lng = LongitudeNbr.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return $"https://www.google.com/maps/search/?api=1&query={lat},{lng}";
            }

            if (!string.IsNullOrWhiteSpace(LocationAddressTxt))
                return $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(LocationAddressTxt)}";

            return null;
        }
    }

    [Column("LOCATION_ADDRESS_TXT")]
    [StringLength(300)]
    public string? LocationAddressTxt { get; set; }

    [Column("LATITUDE_NBR")]
    public decimal? LatitudeNbr { get; set; }

    [Column("LONGITUDE_NBR")]
    public decimal? LongitudeNbr { get; set; }

    [Column("CONTACT_NBR")]
    [StringLength(15)]
    public string? ContactNbr { get; set; }

    /// <summary>'ANY' | 'MALE' | 'FEMALE' — optional gender preference for this shift.</summary>
    [Required]
    [Column("GENDER_PREFERENCE_CD")]
    [StringLength(10)]
    public string GenderPreferenceCd { get; set; } = "ANY";

    /// <summary>'ACTIVE' | 'PAUSED' | 'CLOSED'</summary>
    [Required]
    [Column("STATUS_CD")]
    [StringLength(20)]
    public string StatusCd { get; set; } = "ACTIVE";

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }

    [ForeignKey("EmployerUserAccountId")]
    public UserAccount? EmployerUserAccount { get; set; }

    public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
}

/// <summary>
/// Maps to JOB_SKILL table. Junction between JOB_POST and SKILL for the
/// "Required Skills" multi-select on Screen E-03.
/// </summary>
[Table("JOB_SKILL")]
public class JobSkill
{
    [Key]
    [Column("JOB_SKILL_ID")]
    public int JobSkillId { get; set; }

    [Required]
    [Column("JOB_POST_ID")]
    public int JobPostId { get; set; }

    [Required]
    [Column("SKILL_ID")]
    public int SkillId { get; set; }

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }

    [ForeignKey("JobPostId")]
    public JobPost? JobPost { get; set; }

    [ForeignKey("SkillId")]
    public Skill? Skill { get; set; }
}

/// <summary>
/// Maps to JOB_APPLICATION table. An employee's application against a job post.
/// NOTE: there is no EMPLOYER_USER_ACCOUNT_ID column here — the employer is
/// only reachable by joining through JobPost.EmployerUserAccountId.
/// </summary>
[Table("JOB_APPLICATION")]
public class JobApplication
{
    [Key]
    [Column("JOB_APPLICATION_ID")]
    public int JobApplicationId { get; set; }

    [Required]
    [Column("JOB_POST_ID")]
    public int JobPostId { get; set; }

    [Required]
    [Column("EMPLOYEE_USER_ACCOUNT_ID")]
    public int EmployeeUserAccountId { get; set; }

    /// <summary>PENDING | EMPLOYER_VIEWED | EMPLOYER_CONTACTED | JOB_STARTED | COMPLETED | CANCELLED</summary>
    [Required]
    [Column("STATUS_CD")]
    [StringLength(30)]
    public string StatusCd { get; set; } = "PENDING";

    [Column("APPLIED_TS")]
    public DateTime AppliedTs { get; set; } = DateTime.UtcNow;

    [Column("CANCEL_REASON_CD")] [StringLength(30)] public string? CancelReasonCd { get; set; }
    [Column("CANCEL_REASON_TXT")] [StringLength(500)] public string? CancelReasonTxt { get; set; }
    [Column("CANCELLED_TS")] public DateTime? CancelledTs { get; set; }

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }

    [ForeignKey("JobPostId")]
    public JobPost JobPost { get; set; } = null!;

    [ForeignKey("EmployeeUserAccountId")]
    public UserAccount EmployeeUserAccount { get; set; } = null!;

    public KaarigarRating? KaarigarRating { get; set; }
}

/// <summary>Employer rates the Kaarigar once JobApplication.StatusCd = COMPLETED. One row per application.</summary>
[Table("KAARIGAR_RATING")]
public class KaarigarRating
{
    [Key] [Column("KAARIGAR_RATING_ID")] public int KaarigarRatingId { get; set; }
    [Required] [Column("JOB_APPLICATION_ID")] public int JobApplicationId { get; set; }
    [Required] [Column("JOB_POST_ID")] public int JobPostId { get; set; }
    [Required] [Column("EMPLOYEE_USER_ACCOUNT_ID")] public int EmployeeUserAccountId { get; set; }
    [Required] [Column("EMPLOYER_USER_ACCOUNT_ID")] public int EmployerUserAccountId { get; set; }
    [Required] [Range(1, 5)] [Column("RATING_NBR")] public byte RatingNbr { get; set; }
    [Column("REVIEW_TXT")] [StringLength(1000)] public string? ReviewTxt { get; set; }
    [Column("RATED_TS")] public DateTime RatedTs { get; set; } = DateTime.UtcNow;

    [ForeignKey("JobApplicationId")] public JobApplication? JobApplication { get; set; }
}

/// <summary>
/// Maps to NOTIFICATION_LOG table.
/// NOTE: there is no generic USER_ACCOUNT_ID column and no read/unread flag.
/// The table carries separate nullable Employee/Employer FKs and only a
/// SENT/FAILED delivery STATUS_CD.
/// </summary>
[Table("NOTIFICATION_LOG")]
public class NotificationLog
{
    [Key]
    [Column("NOTIFICATION_LOG_ID")]
    public int NotificationLogId { get; set; }

    [Column("EMPLOYEE_USER_ACCOUNT_ID")]
    public int? EmployeeUserAccountId { get; set; }

    [Column("EMPLOYER_USER_ACCOUNT_ID")]
    public int? EmployerUserAccountId { get; set; }

    [Column("JOB_POST_ID")]
    public int? JobPostId { get; set; }

    /// <summary>'WHATSAPP' | 'SMS'</summary>
    [Required]
    [Column("CHANNEL_CD")]
    [StringLength(20)]
    public string ChannelCd { get; set; } = string.Empty;

    [Column("MESSAGE_TXT")]
    public string? MessageTxt { get; set; }

    [Column("SENT_TS")]
    public DateTime SentTs { get; set; } = DateTime.UtcNow;

    /// <summary>'SENT' | 'FAILED'</summary>
    [Required]
    [Column("STATUS_CD")]
    [StringLength(20)]
    public string StatusCd { get; set; } = "SENT";

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }

    [ForeignKey("EmployeeUserAccountId")]
    public UserAccount? EmployeeUserAccount { get; set; }

    [ForeignKey("EmployerUserAccountId")]
    public UserAccount? EmployerUserAccount { get; set; }

    [ForeignKey("JobPostId")]
    public JobPost? JobPost { get; set; }
}

/// <summary>OTP_RECORD.OTP_TYPE_CD values.</summary>
public static class OtpType
{
    /// <summary>Employer generates this when the Kaarigar reaches the job site; Kaarigar enters it to start the job.</summary>
    public const string JobStart = "JOB_START";

    /// <summary>Employer generates this once the job is done; Kaarigar enters it to confirm completion.</summary>
    public const string Satisfaction = "SATISFACTION";
}

/// <summary>
/// Maps to OTP_RECORD table (job-site identity verification OTP — NOT the
/// registration/forgot-password OTP, which lives in MOBILE_VERIFICATION_OTP).
/// Employer-generated: the employer creates a fresh OTP of a given type and
/// the Kaarigar enters it in their own app to advance the job's status.
/// </summary>
[Table("OTP_RECORD")]
public class OtpRecord
{
    [Key]
    [Column("OTP_RECORD_ID")]
    public int OtpRecordId { get; set; }

    [Required]
    [Column("EMPLOYEE_USER_ACCOUNT_ID")]
    public int EmployeeUserAccountId { get; set; }

    [Required]
    [Column("JOB_POST_ID")]
    public int JobPostId { get; set; }

    /// <summary>The employer who generated this OTP (OTPs are now employer-triggered, not employee-triggered).</summary>
    [Column("EMPLOYER_USER_ACCOUNT_ID")]
    public int? EmployerUserAccountId { get; set; }

    /// <summary>'JOB_START' (Kaarigar has reached the site) | 'SATISFACTION' (job completed).</summary>
    [Required]
    [Column("OTP_TYPE_CD")]
    [StringLength(20)]
    public string OtpTypeCd { get; set; } = "JOB_START";

    [Required]
    [Column("OTP_CD")]
    [StringLength(6)]
    public string OtpCd { get; set; } = string.Empty;

    [Column("GENERATED_TS")]
    public DateTime GeneratedTs { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("EXPIRES_TS")]
    public DateTime ExpiresTs { get; set; }

    [Column("IS_USED_FL")]
    public bool IsUsedFl { get; set; } = false;

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }

    [ForeignKey("EmployeeUserAccountId")]
    public UserAccount? EmployeeUserAccount { get; set; }

    [ForeignKey("EmployerUserAccountId")]
    public UserAccount? EmployerUserAccount { get; set; }

    [ForeignKey("JobPostId")]
    public JobPost? JobPost { get; set; }
}
