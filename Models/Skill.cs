using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/// <summary>
/// Maps to SKILL table. Predefined, seeded master list (per schema comment:
/// "FUTURE WEEK — Week 3, created now to keep schema complete"). Used both
/// by EMPLOYEE_SKILL (employee's own skills) and JOB_SKILL (skills required
/// for a job post).
/// </summary>
[Table("SKILL")]
public class Skill
{
    [Key]
    [Column("SKILL_ID")]
    public int SkillId { get; set; }

    [Required]
    [Column("SKILL_NAME")]
    [StringLength(100)]
    public string SkillName { get; set; } = string.Empty;

    [Column("CATEGORY_NAME")]
    [StringLength(100)]
    public string? CategoryName { get; set; }

    [Column("IS_ACTIVE_FL")]
    public bool IsActiveFl { get; set; } = true;

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }
}
