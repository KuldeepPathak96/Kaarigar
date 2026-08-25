using Kaarigar.Data;
using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Services;

public class BillingDashboardRow
{
    public string InvoiceType { get; set; } = string.Empty; // "KAARIGAR" | "WEBSITE"
    public int InvoiceId { get; set; }
    public string InvoiceNbr { get; set; } = string.Empty;
    public DateTime InvoiceDt { get; set; }
    public string EmployerName { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public decimal TotalAmt { get; set; }
    public string StatusCd { get; set; } = string.Empty;
    public bool WhatsAppSent { get; set; }
    public string? PdfFilePathTxt { get; set; }
}

public class BillingDashboardSummary
{
    public int TotalKaarigarInvoices { get; set; }
    public decimal TotalKaarigarAmt { get; set; }
    public int TotalWebsiteInvoices { get; set; }
    public decimal TotalWebsiteAmt { get; set; }
    public int PendingPaymentCount { get; set; }
    public decimal PendingPaymentAmt { get; set; }
    public int PaidCount { get; set; }
}

public interface IBillingDashboardService
{
    Task<BillingDashboardSummary> GetSummaryAsync(int? employerUserAccountId = null);

    Task<List<BillingDashboardRow>> GetInvoicesAsync(
        int? employerUserAccountId = null, string? invoiceTypeFilter = null, string? statusFilter = null,
        DateTime? fromDt = null, DateTime? toDt = null);

    Task<bool> MarkPaidAsync(string invoiceType, int invoiceId, string updatedBy);

    Task<bool> ResendWhatsAppAsync(string invoiceType, int invoiceId);
}

public class BillingDashboardService : IBillingDashboardService
{
    private readonly AppDbContext _db;
    private readonly IWhatsAppNotificationService _whatsAppService;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

    public BillingDashboardService(AppDbContext db, IWhatsAppNotificationService whatsAppService, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _db = db;
        _whatsAppService = whatsAppService;
        _config = config;
    }

    public async Task<BillingDashboardSummary> GetSummaryAsync(int? employerUserAccountId = null)
    {
        var kaarigarQuery = _db.KaarigarInvoices.AsNoTracking().AsQueryable();
        var websiteQuery = _db.WebsiteInvoices.AsNoTracking().AsQueryable();

        if (employerUserAccountId.HasValue)
        {
            kaarigarQuery = kaarigarQuery.Where(i => i.EmployerUserAccountId == employerUserAccountId.Value);
            websiteQuery = websiteQuery.Where(i => i.EmployerUserAccountId == employerUserAccountId.Value);
        }

        var summary = new BillingDashboardSummary
        {
            TotalKaarigarInvoices = await kaarigarQuery.CountAsync(),
            TotalKaarigarAmt = await kaarigarQuery.SumAsync(i => (decimal?)i.TotalAmt) ?? 0,
            TotalWebsiteInvoices = await websiteQuery.CountAsync(),
            TotalWebsiteAmt = await websiteQuery.SumAsync(i => (decimal?)i.TotalAmt) ?? 0,
            PendingPaymentCount = await websiteQuery.CountAsync(i => i.StatusCd == "PENDING_PAYMENT"),
            PendingPaymentAmt = await websiteQuery.Where(i => i.StatusCd == "PENDING_PAYMENT").SumAsync(i => (decimal?)i.TotalAmt) ?? 0,
            PaidCount = await kaarigarQuery.CountAsync(i => i.StatusCd == "PAID") + await websiteQuery.CountAsync(i => i.StatusCd == "PAID"),
        };

        return summary;
    }

    public async Task<List<BillingDashboardRow>> GetInvoicesAsync(
        int? employerUserAccountId = null, string? invoiceTypeFilter = null, string? statusFilter = null,
        DateTime? fromDt = null, DateTime? toDt = null)
    {
        var rows = new List<BillingDashboardRow>();

        if (invoiceTypeFilter is null or "KAARIGAR")
        {
            var q = _db.KaarigarInvoices.AsNoTracking()
                .Include(i => i.JobPost)
                .AsQueryable();

            if (employerUserAccountId.HasValue) q = q.Where(i => i.EmployerUserAccountId == employerUserAccountId.Value);
            if (!string.IsNullOrEmpty(statusFilter)) q = q.Where(i => i.StatusCd == statusFilter);
            if (fromDt.HasValue) q = q.Where(i => i.InvoiceDt >= fromDt.Value);
            if (toDt.HasValue) q = q.Where(i => i.InvoiceDt <= toDt.Value);

            var kaarigarInvoices = await q.OrderByDescending(i => i.InvoiceDt).ToListAsync();
            var employerIds = kaarigarInvoices.Select(i => i.EmployerUserAccountId).Distinct().ToList();
            var workerIds = kaarigarInvoices.Select(i => i.WorkerUserAccountId).Distinct().ToList();
            var names = await GetUserNamesAsync(employerIds.Concat(workerIds).Distinct().ToList());

            rows.AddRange(kaarigarInvoices.Select(i => new BillingDashboardRow
            {
                InvoiceType = "KAARIGAR",
                InvoiceId = i.KaarigarInvoiceId,
                InvoiceNbr = i.InvoiceNbr,
                InvoiceDt = i.InvoiceDt,
                EmployerName = names.GetValueOrDefault(i.EmployerUserAccountId, "—"),
                WorkerName = names.GetValueOrDefault(i.WorkerUserAccountId, "—"),
                JobTitle = i.JobPost?.JobTitle ?? "—",
                TotalAmt = i.TotalAmt,
                StatusCd = i.StatusCd,
                WhatsAppSent = i.WhatsAppSentTs.HasValue,
                PdfFilePathTxt = i.PdfFilePathTxt,
            }));
        }

        if (invoiceTypeFilter is null or "WEBSITE")
        {
            var q = _db.WebsiteInvoices.AsNoTracking()
                .Include(i => i.JobPost)
                .AsQueryable();

            if (employerUserAccountId.HasValue) q = q.Where(i => i.EmployerUserAccountId == employerUserAccountId.Value);
            if (!string.IsNullOrEmpty(statusFilter)) q = q.Where(i => i.StatusCd == statusFilter);
            if (fromDt.HasValue) q = q.Where(i => i.InvoiceDt >= fromDt.Value);
            if (toDt.HasValue) q = q.Where(i => i.InvoiceDt <= toDt.Value);

            var websiteInvoices = await q.OrderByDescending(i => i.InvoiceDt).ToListAsync();
            var employerIds = websiteInvoices.Select(i => i.EmployerUserAccountId).Distinct().ToList();
            var workerIds = websiteInvoices.Select(i => i.WorkerUserAccountId).Distinct().ToList();
            var names = await GetUserNamesAsync(employerIds.Concat(workerIds).Distinct().ToList());

            rows.AddRange(websiteInvoices.Select(i => new BillingDashboardRow
            {
                InvoiceType = "WEBSITE",
                InvoiceId = i.WebsiteInvoiceId,
                InvoiceNbr = i.InvoiceNbr,
                InvoiceDt = i.InvoiceDt,
                EmployerName = names.GetValueOrDefault(i.EmployerUserAccountId, "—"),
                WorkerName = names.GetValueOrDefault(i.WorkerUserAccountId, "—"),
                JobTitle = i.JobPost?.JobTitle ?? "—",
                TotalAmt = i.TotalAmt,
                StatusCd = i.StatusCd,
                WhatsAppSent = i.WhatsAppSentTs.HasValue,
                PdfFilePathTxt = i.PdfFilePathTxt,
            }));
        }

        return rows.OrderByDescending(r => r.InvoiceDt).ToList();
    }

    public async Task<bool> MarkPaidAsync(string invoiceType, int invoiceId, string updatedBy)
    {
        if (invoiceType == "KAARIGAR")
        {
            var invoice = await _db.KaarigarInvoices.FindAsync(invoiceId);
            if (invoice == null) return false;
            invoice.StatusCd = "PAID";
            invoice.PaidTs = DateTime.UtcNow;
            invoice.UpdatedBy = updatedBy;
            invoice.UpdatedTs = DateTime.UtcNow;
        }
        else if (invoiceType == "WEBSITE")
        {
            var invoice = await _db.WebsiteInvoices.FindAsync(invoiceId);
            if (invoice == null) return false;
            invoice.StatusCd = "PAID";
            invoice.PaidTs = DateTime.UtcNow;
            invoice.UpdatedBy = updatedBy;
            invoice.UpdatedTs = DateTime.UtcNow;
        }
        else
        {
            return false;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResendWhatsAppAsync(string invoiceType, int invoiceId)
    {
        if (invoiceType == "KAARIGAR")
        {
            var invoice = await _db.KaarigarInvoices.FindAsync(invoiceId);
            if (invoice == null || string.IsNullOrEmpty(invoice.PdfFilePathTxt)) return false;

            var employer = await _db.UserAccounts.FindAsync(invoice.EmployerUserAccountId);
            if (employer == null) return false;

            var status = await SendAsync(employer.ContactNbr, invoice.PdfFilePathTxt, invoice.InvoiceNbr,
                $"Kaarigar Invoice {invoice.InvoiceNbr} — Platform Service Charge Rs.{invoice.TotalAmt:N2}. Payable immediately.");

            if (status == "SENT")
            {
                invoice.WhatsAppSentTs = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        if (invoiceType == "WEBSITE")
        {
            var invoice = await _db.WebsiteInvoices.FindAsync(invoiceId);
            if (invoice == null || string.IsNullOrEmpty(invoice.PdfFilePathTxt)) return false;

            var employer = await _db.UserAccounts.FindAsync(invoice.EmployerUserAccountId);
            if (employer == null) return false;

            var status = await SendAsync(employer.ContactNbr, invoice.PdfFilePathTxt, invoice.InvoiceNbr,
                $"Website Invoice {invoice.InvoiceNbr} — Worker Funding Amount Rs.{invoice.TotalAmt:N2}. Please pay to release the worker's payout.");

            if (status == "SENT")
            {
                invoice.WhatsAppSentTs = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        return false;
    }

    private async Task<string> SendAsync(string contactNbr, string pdfRelativePath, string invoiceNbr, string caption)
    {
        var baseUrl = _config["App:BaseUrl"]?.TrimEnd('/') ?? "";
        var documentUrl = $"{baseUrl}/{pdfRelativePath}";
        var filename = $"{invoiceNbr.Replace("/", "-")}.pdf";
        return await _whatsAppService.SendDocumentAsync(contactNbr, documentUrl, filename, caption);
    }

    private async Task<Dictionary<int, string>> GetUserNamesAsync(List<int> userAccountIds)
    {
        var users = await _db.UserAccounts.AsNoTracking()
            .Where(u => userAccountIds.Contains(u.UserAccountId))
            .ToListAsync();

        return users.ToDictionary(u => u.UserAccountId, u => $"{u.FirstName} {u.LastName}".Trim());
    }
}
