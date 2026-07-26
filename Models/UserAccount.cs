using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/// <summary>
/// Maps to USER_ACCOUNT table (DB: MANPOWER_HIRING_DB).
/// Central table for all logins: EMPLOYER, EMPLOYEE, ADMIN.
/// </summary>
[Table("USER_ACCOUNT")]
public class UserAccount
{
    [Key]
    [Column("USER_ACCOUNT_ID")]
    public int UserAccountId { get; set; }

    [Required]
    [Column("FIRST_NAME")]
    [StringLength(75)]
    public string FirstName { get; set; } = string.Empty;

    [Column("LAST_NAME")]
    [StringLength(75)]
    public string? LastName { get; set; }

    [Required]
    [Column("CONTACT_NBR")]
    [StringLength(15)]
    public string ContactNbr { get; set; } = string.Empty;

    [Column("EMAIL_ID")]
    [StringLength(150)]
    public string? EmailId { get; set; }

    [Required]
    [Column("PASSWORD_HASH_TXT")]
    [StringLength(255)]
    public string PasswordHashTxt { get; set; } = string.Empty;

    /// <summary>'EMPLOYER' | 'EMPLOYEE' | 'ADMIN'</summary>
    [Required]
    [Column("ROLE_CD")]
    [StringLength(20)]
    public string RoleCd { get; set; } = string.Empty;

    [Column("IS_MOBILE_VERIFIED_FL")]
    public bool IsMobileVerifiedFl { get; set; } = false;

    [Column("IS_ACTIVE_FL")]
    public bool IsActiveFl { get; set; } = true;

    [Column("IS_APPROVED_FL")]
    public bool IsApprovedFl { get; set; } = false;

    [Column("LAST_LOGIN_TS")]
    public DateTime? LastLoginTs { get; set; }

    // ── Audit columns ────────────────────────────────────────────────────────

    [Column("CREATED_BY")]
    [StringLength(100)]
    public string CreatedBy { get; set; } = "SYSTEM";

    [Column("CREATED_TS")]
    public DateTime CreatedTs { get; set; } = DateTime.UtcNow;

    [Column("CREATED_IP_ADDR")]
    [StringLength(45)]
    public string? CreatedIpAddr { get; set; }

    [Column("UPDATED_BY")]
    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    [Column("UPDATED_TS")]
    public DateTime? UpdatedTs { get; set; }

    [Column("UPDATED_IP_ADDR")]
    [StringLength(45)]
    public string? UpdatedIpAddr { get; set; }
}
