using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/// <summary>
/// Maps to BUSINESS_CATEGORY table. Admin-managed master list shown as the
/// "Business Category" dropdown on the Employer Profile screen (E-02).
/// Employers can only pick from this list; only Admin can add/remove entries.
/// </summary>
[Table("BUSINESS_CATEGORY")]
public class BusinessCategory
{
    [Key]
    [Column("BUSINESS_CATEGORY_ID")]
    public int BusinessCategoryId { get; set; }

    [Required]
    [Column("CATEGORY_NAME")]
    [StringLength(150)]
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
}
