using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

[Table("EMPLOYER_PROFILE")]
public class EmployerProfile
{
    [Key]
    [Column("EMPLOYER_PROFILE_ID")]
    public int EmployerProfileId { get; set; }

    [Required]
    [Column("USER_ACCOUNT_ID")]
    public int UserAccountId { get; set; }

    [Column("COMPANY_NAME")]
    [StringLength(200)]
    public string? CompanyName { get; set; }

    [Column("LOGO_FILE_PATH_TXT")]
    [StringLength(400)]
    public string? LogoFilePathTxt { get; set; }

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

    // ── E-02 additions ───────────────────────────────────────────────────────

    /// <summary>Name of the person employers should be contacted through (can differ from the login user's name).</summary>
    [Column("CONTACT_PERSON_NAME")]
    [StringLength(150)]
    public string? ContactPersonName { get; set; }

    /// <summary>FK to BUSINESS_CATEGORY. Admin-managed dropdown list.</summary>
    [Column("BUSINESS_CATEGORY_ID")]
    public int? BusinessCategoryId { get; set; }

    /// <summary>'GST' | 'GUMASTHA'</summary>
    [Column("BUSINESS_PROOF_TYPE_CD")]
    [StringLength(20)]
    public string? BusinessProofTypeCd { get; set; }

    [Column("BUSINESS_PROOF_NUMBER_TXT")]
    [StringLength(30)]
    public string? BusinessProofNumberTxt { get; set; }

    [Column("BUSINESS_PROOF_ORIGINAL_FILE_NAME_TXT")]
    [StringLength(255)]
    public string? BusinessProofOriginalFileNameTxt { get; set; }

    [Column("BUSINESS_PROOF_FILE_PATH_TXT")]
    [StringLength(500)]
    public string? BusinessProofFilePathTxt { get; set; }

    /// <summary>'PENDING' | 'APPROVED' | 'REJECTED' — set by Admin review, not the employer.</summary>
    [Required]
    [Column("BUSINESS_PROOF_REVIEW_STATUS_CD")]
    [StringLength(20)]
    public string BusinessProofReviewStatusCd { get; set; } = "PENDING";

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }

    [ForeignKey("UserAccountId")]
    public UserAccount? UserAccount { get; set; }

    [ForeignKey("BusinessCategoryId")]
    public BusinessCategory? BusinessCategory { get; set; }
}
