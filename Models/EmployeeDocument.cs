using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/// <summary>
/// Maps to EMPLOYEE_DOCUMENT table. Stores an employee's uploaded ID proof
/// and/or resume/CV. One row per document type per employee (ID_PROOF and
/// RESUME are separate rows) — uploading again replaces that row's file
/// fields and resets it back to PENDING review.
///
/// Only the last 4 digits/characters of the ID number are ever persisted
/// (IdLastFourDigitTxt) — the full number is never stored in the DB; Admin
/// verifies the real number by opening the uploaded file itself.
/// </summary>
[Table("EMPLOYEE_DOCUMENT")]
public class EmployeeDocument
{
    [Key]
    [Column("EMPLOYEE_DOCUMENT_ID")]
    public int EmployeeDocumentId { get; set; }

    [Required]
    [Column("USER_ACCOUNT_ID")]
    public int UserAccountId { get; set; }

    /// <summary>'ID_PROOF' | 'RESUME'</summary>
    [Required]
    [Column("DOCUMENT_TYPE_CD")]
    [StringLength(30)]
    public string DocumentTypeCd { get; set; } = string.Empty;

    /// <summary>'AADHAAR' | 'PAN' | 'VOTER_ID' | 'DRIVING_LICENSE' — null for RESUME rows.</summary>
    [Column("DOCUMENT_SUBTYPE_CD")]
    [StringLength(30)]
    public string? DocumentSubtypeCd { get; set; }

    /// <summary>Last 4 digits/characters only — null for RESUME rows.</summary>
    [Column("ID_LAST_FOUR_DIGIT_TXT")]
    [StringLength(4)]
    public string? IdLastFourDigitTxt { get; set; }

    [Required]
    [Column("ORIGINAL_FILE_NAME_TXT")]
    [StringLength(255)]
    public string OriginalFileNameTxt { get; set; } = string.Empty;

    [Required]
    [Column("STORED_FILE_NAME_TXT")]
    [StringLength(255)]
    public string StoredFileNameTxt { get; set; } = string.Empty;

    [Required]
    [Column("SERVER_FILE_PATH_TXT")]
    [StringLength(500)]
    public string ServerFilePathTxt { get; set; } = string.Empty;

    [Column("FILE_SIZE_KB_NBR")]
    public int? FileSizeKbNbr { get; set; }

    [Column("MIME_TYPE_TXT")]
    [StringLength(100)]
    public string? MimeTypeTxt { get; set; }

    [Column("UPLOADED_TS")]
    public DateTime UploadedTs { get; set; } = DateTime.UtcNow;

    /// <summary>'PENDING' | 'APPROVED' | 'REJECTED'</summary>
    [Required]
    [Column("REVIEW_STATUS_CD")]
    [StringLength(20)]
    public string ReviewStatusCd { get; set; } = "PENDING";

    [Column("REVIEWED_BY_USER_ACCOUNT_ID")]
    public int? ReviewedByUserAccountId { get; set; }

    [Column("REVIEWED_TS")]
    public DateTime? ReviewedTs { get; set; }

    [Column("REJECTION_REASON_TXT")]
    [StringLength(300)]
    public string? RejectionReasonTxt { get; set; }

    // Audit
    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("CREATED_IP_ADDR")] public string? CreatedIpAddr { get; set; }
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }
    [Column("UPDATED_IP_ADDR")] public string? UpdatedIpAddr { get; set; }

    [ForeignKey("UserAccountId")]
    public UserAccount? UserAccount { get; set; }

    [ForeignKey("ReviewedByUserAccountId")]
    public UserAccount? ReviewedByUserAccount { get; set; }
}
