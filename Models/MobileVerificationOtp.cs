using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/// <summary>
/// Maps to MOBILE_VERIFICATION_OTP table.
/// Used for REGISTRATION and FORGOT_PASSWORD OTP flows.
/// NOTE: schema column is CONTACT_NBR (mobile-oriented), but for the
/// Forgot Password screen we resolve the user by EMAIL_ID first, then
/// store/verify the OTP against that user's CONTACT_NBR value here so we
/// reuse the existing table instead of adding a new one.
/// </summary>
[Table("MOBILE_VERIFICATION_OTP")]
public class MobileVerificationOtp
{
    [Key]
    [Column("MOBILE_VERIFICATION_OTP_ID")]
    public int MobileVerificationOtpId { get; set; }

    [Required]
    [Column("CONTACT_NBR")]
    [StringLength(15)]
    public string ContactNbr { get; set; } = string.Empty;

    [Required]
    [Column("OTP_CD")]
    [StringLength(6)]
    public string OtpCd { get; set; } = string.Empty;

    /// <summary>'REGISTRATION' | 'FORGOT_PASSWORD'</summary>
    [Required]
    [Column("PURPOSE_CD")]
    [StringLength(30)]
    public string PurposeCd { get; set; } = string.Empty;

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
}
