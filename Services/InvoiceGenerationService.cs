using Kaarigar.Data;
using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Services;

public interface IInvoiceGenerationService
{
    /// <summary>
    /// Called the moment a JobApplication reaches STATUS_CD = 'COMPLETED'
    /// (Satisfaction OTP confirmed). Generates the Kaarigar Invoice
    /// (commission) and the Website Invoice (worker-funding amount), saves
    /// both to the database, and renders their PDFs. Payment collection is
    /// NOT part of this — it just creates the two invoice records.
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
    // PF Employer Contribution: 13% (12% EPF + ~0.5%–1% EDLI/admin, matches the
    // "13%" label on the real sample invoice), capped at a ₹15,000/month wage ceiling.
    private const decimal PfEmployerRatePct = 13.00m;
    private const decimal PfWageCeilingAmt = 15_000m;

    // ESIC Employer Contribution: 3.25% of gross wage, only applicable when
    // gross wage for the period is <= ₹21,000 (the ESIC wage threshold).
    private const decimal EsicEmployerRatePct = 3.25m;
    private const decimal EsicWageThresholdAmt = 21_000m;

    // Kaarigar's own commission — used only when EmployerProfile.ServiceChargePct is not set.
    private const decimal DefaultServiceChargePct = 15.00m;

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
        var manpowerAmt = Math.Round(hours * rate, 2);

        var pfBase = Math.Min(manpowerAmt, PfWageCeilingAmt);
        var pfEmployerAmt = Math.Round(pfBase * PfEmployerRatePct / 100m, 2);

        var esicApplicable = manpowerAmt <= EsicWageThresholdAmt;
        var esicEmployerAmt = esicApplicable ? Math.Round(manpowerAmt * EsicEmployerRatePct / 100m, 2) : 0m;

        var serviceChargePct = employerProfile?.ServiceChargePct ?? DefaultServiceChargePct;
        var serviceChargeAmt = Math.Round(manpowerAmt * serviceChargePct / 100m, 2);

        var invoiceDt = DateTime.UtcNow.Date;
        var dueDt = invoiceDt; // "payable right away" per business requirement

        // ── Kaarigar Invoice (commission) ───────────────────────────────────
        var kaarigarTaxable = serviceChargeAmt;
        var (kCgst, kSgst, kIgst, kTotal) = ApplyGst(kaarigarTaxable, gstTypeCd);

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
            TaxableAmt = kaarigarTaxable,
            GstRatePct = GstRatePct,
            CgstAmt = kCgst,
            SgstAmt = kSgst,
            IgstAmt = kIgst,
            TotalAmt = kTotal,
            StatusCd = "DUE",
            CreatedBy = "SATISFACTION_OTP",
        };

        // ── Website Invoice (worker-funding amount) ─────────────────────────
        var websiteTaxable = Math.Round(manpowerAmt + pfEmployerAmt + esicEmployerAmt, 2);
        var (wCgst, wSgst, wIgst, wTotal) = ApplyGst(websiteTaxable, gstTypeCd);

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
            ManpowerAmt = manpowerAmt,
            PfEmployerAmt = pfEmployerAmt,
            EsicEmployerAmt = esicEmployerAmt,
            TaxableAmt = websiteTaxable,
            GstRatePct = GstRatePct,
            CgstAmt = wCgst,
            SgstAmt = wSgst,
            IgstAmt = wIgst,
            TotalAmt = wTotal,
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
                    $"Kaarigar Invoice {kaarigarInvoice.InvoiceNbr} — Platform Service Charge ₹{kaarigarInvoice.TotalAmt:N2}. Payable immediately.",
                    async () => { kaarigarInvoice.WhatsAppSentTs = DateTime.UtcNow; await _db.SaveChangesAsync(); });

                await SendInvoiceViaWhatsAppAsync(
                    employer.ContactNbr, websiteInvoice.PdfFilePathTxt, websiteInvoice.InvoiceNbr,
                    $"Website Invoice {websiteInvoice.InvoiceNbr} — Worker Funding Amount ₹{websiteInvoice.TotalAmt:N2}. Please pay to release the worker's payout.",
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
            "Generated invoices for JobApplicationId={Id}: Kaarigar {KaarigarInvoiceNbr} (₹{KaarigarTotal}), Website {WebsiteInvoiceNbr} (₹{WebsiteTotal})",
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
