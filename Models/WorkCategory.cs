using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/// <summary>
/// Maps to WORK_CATEGORY table. A Work Category sits under one Business
/// Category (e.g. "Restaurants" → "Steward / Waiter") and is what a Worker
/// selects as their "Work Preference" at registration. Drives which Skills
/// are shown next, via WORK_CATEGORY_SKILL.
/// </summary>
[Table("WORK_CATEGORY")]
public class WorkCategory
{
    [Key]
    [Column("WORK_CATEGORY_ID")]
    public int WorkCategoryId { get; set; }

    [Required]
    [Column("BUSINESS_CATEGORY_ID")]
    public int BusinessCategoryId { get; set; }

    [Required]
    [Column("CATEGORY_NAME")]
    [StringLength(200)]
    public string CategoryName { get; set; } = string.Empty;

    [Column("IS_ACTIVE_FL")]
    public bool IsActiveFl { get; set; } = true;

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }

    [ForeignKey("BusinessCategoryId")]
    public BusinessCategory? BusinessCategory { get; set; }
}

/// <summary>
/// Maps to WORK_CATEGORY_SKILL table. Many-to-many mapping between a Work
/// Category and the Skills relevant to it — drives the category-filtered
/// skill chips shown on the Register page and Employee Profile.
/// </summary>
[Table("WORK_CATEGORY_SKILL")]
public class WorkCategorySkill
{
    [Key]
    [Column("WORK_CATEGORY_SKILL_ID")]
    public int WorkCategorySkillId { get; set; }

    [Required]
    [Column("WORK_CATEGORY_ID")]
    public int WorkCategoryId { get; set; }

    [Required]
    [Column("SKILL_ID")]
    public int SkillId { get; set; }

    /// <summary>Core responsibility text for this skill within this Work Category (from the Business/Work/Skill master draft).</summary>
    [Column("RESPONSIBILITY_TXT")]
    [StringLength(500)]
    public string? ResponsibilityTxt { get; set; }

    [Column("IS_ACTIVE_FL")]
    public bool IsActiveFl { get; set; } = true;

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }

    [ForeignKey("WorkCategoryId")]
    public WorkCategory? WorkCategory { get; set; }

    [ForeignKey("SkillId")]
    public Skill? Skill { get; set; }
}
