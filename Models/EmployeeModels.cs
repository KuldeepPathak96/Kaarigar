using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

[Table("EMPLOYEE_PROFILE")]
public class EmployeeProfile
{
    [Key]
    [Column("EMPLOYEE_PROFILE_ID")]
    public int EmployeeProfileId { get; set; }

    [Required]
    [Column("USER_ACCOUNT_ID")]
    public int UserAccountId { get; set; }

    [Column("CITY_NAME")]
    [StringLength(100)]
    public string? CityName { get; set; }

    [Column("AREA_ADDRESS_TXT")]
    [StringLength(300)]
    public string? AreaAddressTxt { get; set; }

    /// <summary>Free-text street/building address — separate from City/Area, which stay DB-driven pickers.</summary>
    [Column("ADDRESS_TXT")]
    [StringLength(400)]
    public string? AddressTxt { get; set; }

    [Column("LATITUDE_NBR")]
    public decimal? LatitudeNbr { get; set; }

    [Column("LONGITUDE_NBR")]
    public decimal? LongitudeNbr { get; set; }

    /// <summary>5 / 10 / 25 / 50 / NULL = Any (km)</summary>
    [Column("PREFERRED_RADIUS_NBR")]
    public int? PreferredRadiusNbr { get; set; }

    /// <summary>Employee-controlled opt-out for job-match WhatsApp alerts (Screen W-02 / W-06 improvement). Defaults to on.</summary>
    [Column("IS_NOTIFICATION_ENABLED_FL")]
    public bool IsNotificationEnabledFl { get; set; } = true;

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }

    [ForeignKey("UserAccountId")]
    public UserAccount? UserAccount { get; set; }
}

[Table("EMPLOYEE_SKILL")]
public class EmployeeSkill
{
    [Key]
    [Column("EMPLOYEE_SKILL_ID")]
    public int EmployeeSkillId { get; set; }

    [Required]
    [Column("USER_ACCOUNT_ID")]
    public int UserAccountId { get; set; }

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
}
