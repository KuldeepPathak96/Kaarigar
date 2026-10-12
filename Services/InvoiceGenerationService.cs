using Kaarigar.Data;
using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Services;

public interface IInvoiceGenerationService
{
    /// <summary>
    /// Called the moment a JobApplication reaches STATUS_CD = 'COMPLETED'
    /// (Satisfaction OTP confirmed). Generates the Website Invoice
    /// (Kaarigar's commission — class KaarigarInvoice) and the Employee
    /// Invoice (Hours x Rate — class WebsiteInvoice), saves both to the
    /// database, and renders their PDFs. Payment collection is handled
    /// separately — the worker marks the Employee Invoice collected
    /// themselves (see BillingController.MarkEmployeeInvoiceCollected).
    ///
    /// Safe to call more than once for the same JobApplicationId — if
    /// invoices already exist for it, this is a no-op (see the UNIQUE
    /// constraint on JOB_APPLICATION_ID in both tables).
    /// </summary>
    Task GenerateForJobApplicationAsync(int jobApplicationId);
}

public class InvoiceGenerationService : IInvoiceGenerationService
{
    // ── Statutory / commercial assumptions — CONFIRM THESE WITH FINANCE BEFORE GO-LIVE ──
    //
    // BUSINESS TERMINOLOGY (see BillingModels.cs header comment for the full
    // C#-class <-> business-term mapping):
    //   "Employee Invoice" (class WebsiteInvoice) = Hours x Rate ONLY. No PF,
    //     no ESIC, no GST. Paid directly worker <-> employer, outside the
    //     platform; the worker marks it collected themselves.
    //   "Website Invoice" (class KaarigarInvoice) = 19% Service Fee on the
    //     Employee Invoice amount, PLUS 18% GST computed on (Employee
    //     Invoice Amount + Service Fee Amount). This is the only amount the
    //     platform bills the Employer for — the wage itself is only the GST
    //     calculation base, it is not part of what's payable here.
    //
    // Kaarigar's own commission — used only when EmployerProfile.ServiceChargePct is not set.
    private const decimal DefaultServiceChargePct = 19.00m;

    private const decimal GstRatePct = 18.00m;
    private const string KaarigarHomeState = "Gujarat";

    private readonly AppDbContext _db;
    private readonly IInvoiceNumberService _invoiceNumberService;
    private readonly IInvoicePdfService _invoicePdfService;
    private readonly IWhatsAppNotificationService _whatsAppService;
    private readonly IConfiguration _config;
    private readonly ILogger<InvoiceGenerationService> _logger;

    public InvoiceGenerationService(
        AppDbContext db,
        IInvoiceNumberService invoiceNumberService,
        IInvoicePdfService invoicePdfService,
        IWhatsAppNotificationService whatsAppService,
        IConfiguration config,
        ILogger<InvoiceGenerationService> logger)
    {
        _db = db;
        _invoiceNumberService = invoiceNumberService;
        _invoicePdfService = invoicePdfService;
        _whatsAppService = whatsAppService;
        _config = config;
        _logger = logger;
    }

    public async Task GenerateForJobApplicationAsync(int jobApplicationId)
    {
        var alreadyExists = await _db.KaarigarInvoices.AnyAsync(i => i.JobApplicationId == jobApplicationId);
        if (alreadyExists)
        {
            _logger.LogInformation("Invoices already exist for JobApplicationId={Id} — skipping.", jobApplicationId);
            return;
        }

        var application = await _db.JobApplications
            .Include(a => a.JobPost)
            .FirstOrDefaultAsync(a => a.JobApplicationId == jobApplicationId);

        if (application == null)
        {
            _logger.LogWarning("GenerateForJobApplicationAsync: JobApplicationId={Id} not found.", jobApplicationId);
            return;
        }

        var jobPost = application.JobPost;
        var employerUserAccountId = jobPost.EmployerUserAccountId;
        var workerUserAccountId = application.EmployeeUserAccountId;

        var employerProfile = await _db.EmployerProfiles.FirstOrDefaultAsync(p => p.UserAccountId == employerUserAccountId);

        var employerState = await ResolveStateForCityAsync(employerProfile?.CityName);
        var gstTypeCd = string.Equals(employerState, KaarigarHomeState, StringComparison.OrdinalIgnoreCase) ? "INTRA" : "INTER";
        var placeOfSupply = employerState ?? KaarigarHomeState;

        var hours = jobPost.DurationHourNbr ?? 0m;
        var rate = jobPost.HourlyWageAmt ?? 0m;

        // "Employee Invoice Amount" — Hours x Rate. This is the ONLY amount
        // on the Employee Invoice (WebsiteInvoice); no PF/ESIC/GST are added
        // to it. It also doubles as the GST calculation base for the
        // Website Invoice (KaarigarInvoice) below.
        var employeeInvoiceAmt = Math.Round(hours * rate, 2);

        var serviceChargePct = employerProfile?.ServiceChargePct ?? DefaultServiceChargePct;
        var serviceChargeAmt = Math.Round(employeeInvoiceAmt * serviceChargePct / 100m, 2);

        var invoiceDt = DateTime.UtcNow.Date;
        var dueDt = invoiceDt; // "payable right away" per business requirement

        // ── Website Invoice (Kaarigar's own commission — class KaarigarInvoice) ──
        // GST is computed on (Employee Invoice Amount + Service Fee), but only
        // the Service Fee + GST is actually payable here — the wage itself is
        // collected directly by the worker, not billed through this invoice.
        var kaarigarGstBaseAmt = Math.Round(employeeInvoiceAmt + serviceChargeAmt, 2);
        var (kCgst, kSgst, kIgst, _) = ApplyGst(kaarigarGstBaseAmt, gstTypeCd);
        var kaarigarGstTotal = kCgst + kSgst + kIgst;
        var kaarigarTotal = Math.Round(serviceChargeAmt + kaarigarGstTotal, 2);

        var kaarigarInvoice = new KaarigarInvoice
        {
            InvoiceNbr = await _invoiceNumberService.GetNextInvoiceNumberAsync("KAARIGAR_INVOICE", "KT"),
            JobApplicationId = jobApplicationId,
            JobPostId = jobPost.JobPostId,
            EmployerUserAccountId = employerUserAccountId,
            WorkerUserAccountId = workerUserAccountId,
            InvoiceDt = invoiceDt,
            DueDt = dueDt,
            PlaceOfSupplyTxt = placeOfSupply,
            GstTypeCd = gstTypeCd,
            ServiceChargeAmt = serviceChargeAmt,
            TaxableAmt = kaarigarGstBaseAmt, // GST calculation base (wage + service fee) — NOT the payable amount
            GstRatePct = GstRatePct,
            CgstAmt = kCgst,
            SgstAmt = kSgst,
            IgstAmt = kIgst,
            TotalAmt = kaarigarTotal, // service fee + GST only — wage excluded
            StatusCd = "DUE",
            CreatedBy = "SATISFACTION_OTP",
        };

        // ── Employee Invoice (Hours x Rate only — class WebsiteInvoice) ──────
        // No PF Employer Contribution, no ESIC Employer Contribution, no GST.
        // Collected directly by the worker from the Employer; see
        // BillingController.MarkEmployeeInvoiceCollected for how the worker
        // marks it received.
        var websiteInvoice = new WebsiteInvoice
        {
            InvoiceNbr = await _invoiceNumberService.GetNextInvoiceNumberAsync("WEBSITE_INVOICE", "WEB"),
            JobApplicationId = jobApplicationId,
            JobPostId = jobPost.JobPostId,
            EmployerUserAccountId = employerUserAccountId,
            WorkerUserAccountId = workerUserAccountId,
            InvoiceDt = invoiceDt,
            DueDt = dueDt,
            PlaceOfSupplyTxt = placeOfSupply,
            GstTypeCd = gstTypeCd,
            HoursWorkedNbr = hours,
            HourlyRateAmt = rate,
            ManpowerAmt = employeeInvoiceAmt,
            PfEmployerAmt = 0m,
            EsicEmployerAmt = 0m,
            TaxableAmt = employeeInvoiceAmt,
            GstRatePct = 0m,
            CgstAmt = 0m,
            SgstAmt = 0m,
            IgstAmt = 0m,
            TotalAmt = employeeInvoiceAmt,
            StatusCd = "PENDING_PAYMENT",
            CreatedBy = "SATISFACTION_OTP",
        };

        _db.KaarigarInvoices.Add(kaarigarInvoice);
        _db.WebsiteInvoices.Add(websiteInvoice);
        await _db.SaveChangesAsync();

        try
        {
            var employer = await _db.UserAccounts.FindAsync(employerUserAccountId);
            var worker = await _db.UserAccounts.FindAsync(workerUserAccountId);

            kaarigarInvoice.PdfFilePathTxt = await _invoicePdfService.GenerateKaarigarInvoicePdfAsync(kaarigarInvoice, employerProfile, employer, worker, jobPost);
            websiteInvoice.PdfFilePathTxt = await _invoicePdfService.GenerateWebsiteInvoicePdfAsync(websiteInvoice, employerProfile, employer, worker, jobPost);
            await _db.SaveChangesAsync();

            if (employer != null)
            {
                await SendInvoiceViaWhatsAppAsync(
                    employer.ContactNbr, kaarigarInvoice.PdfFilePathTxt, kaarigarInvoice.InvoiceNbr,
                    $"Website Invoice {kaarigarInvoice.InvoiceNbr} — Platform Service Fee ₹{kaarigarInvoice.TotalAmt:N2}. Payable immediately.",
                    async () => { kaarigarInvoice.WhatsAppSentTs = DateTime.UtcNow; await _db.SaveChangesAsync(); });

                await SendInvoiceViaWhatsAppAsync(
                    employer.ContactNbr, websiteInvoice.PdfFilePathTxt, websiteInvoice.InvoiceNbr,
                    $"Employee Invoice {websiteInvoice.InvoiceNbr} — Amount Payable to Worker ₹{websiteInvoice.TotalAmt:N2}. Please pay {worker?.FirstName ?? "the worker"} directly.",
                    async () => { websiteInvoice.WhatsAppSentTs = DateTime.UtcNow; await _db.SaveChangesAsync(); });
            }
        }
        catch (Exception ex)
        {
            // Invoice rows are already saved with correct amounts even if PDF
            // rendering or WhatsApp sending fails — both can be retried later
            // without redoing the (financially important) calculation.
            _logger.LogError(ex, "Failed to generate/send PDF for JobApplicationId={Id}", jobApplicationId);
        }

        _logger.LogInformation(
            "Generated invoices for JobApplicationId={Id}: Website Invoice {KaarigarInvoiceNbr} (₹{KaarigarTotal}), Employee Invoice {WebsiteInvoiceNbr} (₹{WebsiteTotal})",
            jobApplicationId, kaarigarInvoice.InvoiceNbr, kaarigarInvoice.TotalAmt, websiteInvoice.InvoiceNbr, websiteInvoice.TotalAmt);
    }

    private async Task SendInvoiceViaWhatsAppAsync(string toContactNbr, string? pdfRelativePath, string invoiceNbr, string caption, Func<Task> onSent)
    {
        if (string.IsNullOrWhiteSpace(pdfRelativePath))
        {
            _logger.LogWarning("Skipping WhatsApp send for {InvoiceNbr} — no PDF path was generated.", invoiceNbr);
            return;
        }

        var baseUrl = _config["App:BaseUrl"]?.TrimEnd('/') ?? "";
        var documentUrl = $"{baseUrl}/{pdfRelativePath}";
        var filename = $"{invoiceNbr.Replace("/", "-")}.pdf";

        var status = await _whatsAppService.SendDocumentAsync(toContactNbr, documentUrl, filename, caption);

        if (status == "SENT")
        {
            await onSent();
        }
        else
        {
            _logger.LogWarning("WhatsApp send failed for invoice {InvoiceNbr} to {ToContactNbr}.", invoiceNbr, toContactNbr);
        }
    }

    private static (decimal Cgst, decimal Sgst, decimal Igst, decimal Total) ApplyGst(decimal taxableAmt, string gstTypeCd)
    {
        var totalGst = Math.Round(taxableAmt * GstRatePct / 100m, 2);

        if (gstTypeCd == "INTRA")
        {
            var half = Math.Round(totalGst / 2m, 2);
            return (half, half, 0m, taxableAmt + half + half);
        }

        return (0m, 0m, totalGst, taxableAmt + totalGst);
    }

    private async Task<string?> ResolveStateForCityAsync(string? cityName)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return null;

        var city = await _db.Cities.FirstOrDefaultAsync(c => c.CityName == cityName);
        return city?.StateName;
    }
}
