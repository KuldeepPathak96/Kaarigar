using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/// <summary>
/// Maps to HOURLY_RATE_OPTION — the admin-editable preset list backing the
/// "Hourly Rate" dropdown on Post a Job (Screen E-03). Employers pick from
/// this list rather than typing a free amount, so Admin can enforce/adjust
/// what rates are offered platform-wide.
/// </summary>
[Table("HOURLY_RATE_OPTION")]
public class HourlyRateOption
{
    [Key]
    [Column("RATE_OPTION_ID")]
    public int RateOptionId { get; set; }

    [Required]
    [Column("RATE_LABEL_TXT")]
    [StringLength(100)]
    public string RateLabelTxt { get; set; } = string.Empty;

    [Required]
    [Column("HOURLY_RATE_AMT")]
    public decimal HourlyRateAmt { get; set; }

    [Column("DISPLAY_ORDER_NBR")]
    public int DisplayOrderNbr { get; set; }

    [Column("IS_ACTIVE_FL")]
    public bool IsActiveFl { get; set; } = true;

    // Audit
    [Column("CREATED_BY")] public string? CreatedBy { get; set; }
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
}
