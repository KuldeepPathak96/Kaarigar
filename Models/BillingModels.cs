using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/* ============================================================================
   BUSINESS-TERM <-> C#-CLASS MAPPING (read this first)
   ============================================================================
   The business terminology for these two invoices changed after this file
   was first written. The C# class names and DB table names were NOT
   renamed (that would touch routes, DbSet names, and several Views for a
   purely cosmetic change) -- so the mapping is:

     Business term "Employee Invoice"  ==  C# class WebsiteInvoice  (table WEBSITE_INVOICE)
       Hours x Rate ONLY. No PF Employer Contribution, no ESIC Employer
       Contribution, no GST. The Employer pays the worker this amount
       directly (cash/UPI/etc, outside the platform) -- the worker then
       marks it collected themselves (see CollectedByEmployeeTs / StatusCd
       = "COLLECTED" below).

     Business term "Website Invoice"   ==  C# class KaarigarInvoice (table KAARIGAR_INVOICE)
       19% Service Fee on the Employee Invoice amount, plus 18% GST
       computed on (Employee Invoice Amount + Service Fee Amount). This is
       the only amount the platform itself bills the Employer for -- the
       wage portion is referenced only as the GST calculation base, it is
       NOT part of what's payable on this invoice.

   See InvoiceGenerationService.cs for the actual calculation.
   ============================================================================ */

/// <summary>
/// BUSINESS TERM: "Website Invoice". Kaarigar's own commission invoice for
/// one completed job (one JOB_APPLICATION): 19% Service Fee on the Employee
/// Invoice amount + 18% GST on (Employee Invoice Amount + Service Fee).
/// Generated automatically the moment the Employer confirms the
/// Satisfaction OTP. Payable immediately — payment collection itself is a
/// separate, later feature; this only creates and records the invoice.
/// </summary>
[Table("KAARIGAR_INVOICE")]
public class KaarigarInvoice
{
    [Key]
    [Column("KAARIGAR_INVOICE_ID")]
    public int KaarigarInvoiceId { get; set; }

    [Required]
    [Column("INVOICE_NBR")]
    [StringLength(40)]
    public string InvoiceNbr { get; set; } = string.Empty;

    [Required]
    [Column("JOB_APPLICATION_ID")]
    public int JobApplicationId { get; set; }

    [Required]
    [Column("JOB_POST_ID")]
    public int JobPostId { get; set; }

    [Required]
    [Column("EMPLOYER_USER_ACCOUNT_ID")]
    public int EmployerUserAccountId { get; set; }

    [Required]
    [Column("WORKER_USER_ACCOUNT_ID")]
    public int WorkerUserAccountId { get; set; }

    [Column("INVOICE_DT")]
    public DateTime InvoiceDt { get; set; }

    [Column("DUE_DT")]
    public DateTime DueDt { get; set; }

    [Column("PLACE_OF_SUPPLY_TXT")]
    [StringLength(100)]
    public string? PlaceOfSupplyTxt { get; set; }

    /// <summary>'INTRA' (CGST+SGST) | 'INTER' (IGST) — based on Employer's state vs Kaarigar's home state (Gujarat).</summary>
    [Required]
    [Column("GST_TYPE_CD")]
    [StringLength(10)]
    public string GstTypeCd { get; set; } = "INTRA";

    [Column("SERVICE_CHARGE_AMT")]
    public decimal ServiceChargeAmt { get; set; }

    [Column("TAXABLE_AMT")]
    public decimal TaxableAmt { get; set; }

    [Column("GST_RATE_PCT")]
    public decimal GstRatePct { get; set; } = 18.00m;

    [Column("CGST_AMT")]
    public decimal CgstAmt { get; set; }

    [Column("SGST_AMT")]
    public decimal SgstAmt { get; set; }

    [Column("IGST_AMT")]
    public decimal IgstAmt { get; set; }

    [Column("TOTAL_AMT")]
    public decimal TotalAmt { get; set; }

    /// <summary>DUE | PAID | OVERDUE | CANCELLED</summary>
    [Required]
    [Column("STATUS_CD")]
    [StringLength(20)]
    public string StatusCd { get; set; } = "DUE";

    [Column("PDF_FILE_PATH_TXT")]
    [StringLength(400)]
    public string? PdfFilePathTxt { get; set; }

    [Column("WHATS_APP_SENT_TS")]
    public DateTime? WhatsAppSentTs { get; set; }

    [Column("PAID_TS")]
    public DateTime? PaidTs { get; set; }

    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }

    [ForeignKey("JobApplicationId")]
    public JobApplication? JobApplication { get; set; }

    [ForeignKey("JobPostId")]
    public JobPost? JobPost { get; set; }
}

/// <summary>
/// BUSINESS TERM: "Employee Invoice". What the worker is owed for one
/// completed job: Hours x Rate ONLY — no PF Employer Contribution, no ESIC
/// Employer Contribution, no GST (all zeroed by InvoiceGenerationService).
/// The Employer pays the worker this amount directly (cash/UPI/etc,
/// outside the platform); the worker then marks it collected themselves
/// via StatusCd = "COLLECTED" / CollectedByEmployeeTs below. Generated
/// alongside the Website Invoice (KaarigarInvoice) at Satisfaction OTP
/// confirmation.
/// </summary>
[Table("WEBSITE_INVOICE")]
public class WebsiteInvoice
{
    [Key]
    [Column("WEBSITE_INVOICE_ID")]
    public int WebsiteInvoiceId { get; set; }

    [Required]
    [Column("INVOICE_NBR")]
    [StringLength(40)]
    public string InvoiceNbr { get; set; } = string.Empty;

    [Required]
    [Column("JOB_APPLICATION_ID")]
    public int JobApplicationId { get; set; }

    [Required]
    [Column("JOB_POST_ID")]
    public int JobPostId { get; set; }

    [Required]
    [Column("EMPLOYER_USER_ACCOUNT_ID")]
    public int EmployerUserAccountId { get; set; }

    [Required]
    [Column("WORKER_USER_ACCOUNT_ID")]
    public int WorkerUserAccountId { get; set; }

    [Column("INVOICE_DT")]
    public DateTime InvoiceDt { get; set; }

    [Column("DUE_DT")]
    public DateTime DueDt { get; set; }

    [Column("PLACE_OF_SUPPLY_TXT")]
    [StringLength(100)]
    public string? PlaceOfSupplyTxt { get; set; }

    [Required]
    [Column("GST_TYPE_CD")]
    [StringLength(10)]
    public string GstTypeCd { get; set; } = "INTRA";

    [Column("HOURS_WORKED_NBR")]
    public decimal HoursWorkedNbr { get; set; }

    [Column("HOURLY_RATE_AMT")]
    public decimal HourlyRateAmt { get; set; }

    [Column("MANPOWER_AMT")]
    public decimal ManpowerAmt { get; set; }

    [Column("PF_EMPLOYER_AMT")]
    public decimal PfEmployerAmt { get; set; }

    [Column("ESIC_EMPLOYER_AMT")]
    public decimal EsicEmployerAmt { get; set; }

    [Column("TAXABLE_AMT")]
    public decimal TaxableAmt { get; set; }

    [Column("GST_RATE_PCT")]
    public decimal GstRatePct { get; set; } = 18.00m;

    [Column("CGST_AMT")]
    public decimal CgstAmt { get; set; }

    [Column("SGST_AMT")]
    public decimal SgstAmt { get; set; }

    [Column("IGST_AMT")]
    public decimal IgstAmt { get; set; }

    [Column("TOTAL_AMT")]
    public decimal TotalAmt { get; set; }

    /// <summary>PENDING_PAYMENT | COLLECTED | CANCELLED. "COLLECTED" = the worker marked they received this amount directly from the Employer (see CollectedByEmployeeTs).</summary>
    [Required]
    [Column("STATUS_CD")]
    [StringLength(20)]
    public string StatusCd { get; set; } = "PENDING_PAYMENT";

    [Column("PDF_FILE_PATH_TXT")]
    [StringLength(400)]
    public string? PdfFilePathTxt { get; set; }

    [Column("WHATS_APP_SENT_TS")]
    public DateTime? WhatsAppSentTs { get; set; }

    [Column("PAID_TS")]
    public DateTime? PaidTs { get; set; }

    /// <summary>Set when the worker taps "Mark Payment Collected" — they're confirming they personally received this amount from the Employer (cash/UPI/etc), outside the platform. Null until then.</summary>
    [Column("COLLECTED_BY_EMPLOYEE_TS")]
    public DateTime? CollectedByEmployeeTs { get; set; }

    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }

    [ForeignKey("JobApplicationId")]
    public JobApplication? JobApplication { get; set; }

    [ForeignKey("JobPostId")]
    public JobPost? JobPost { get; set; }
}

/// <summary>Per-invoice-type numbering counter (KT/26-27/0001 style).</summary>
[Table("INVOICE_NUMBER_SEQUENCE")]
public class InvoiceNumberSequence
{
    [Key]
    [Column("SEQUENCE_CD")]
    [StringLength(30)]
    public string SequenceCd { get; set; } = string.Empty;

    [Column("FINANCIAL_YEAR_TXT")]
    [StringLength(10)]
    public string FinancialYearTxt { get; set; } = string.Empty;

    [Column("LAST_NUMBER_NBR")]
    public int LastNumberNbr { get; set; }
}
