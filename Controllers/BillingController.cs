using Kaarigar.Data;
using Kaarigar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Controllers;

/// <summary>
/// Minimal read/download surface for the two invoices generated at
/// Satisfaction OTP completion. This is intentionally lightweight — full
/// Admin/Employer billing dashboards (list, filter, mark-as-paid, resend)
/// are a separate, larger follow-up piece (see the "pending" list handed
/// back with this feature).
/// </summary>
[Authorize]
public class BillingController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IBillingDashboardService _billingDashboardService;

    public BillingController(AppDbContext db, IWebHostEnvironment env, IBillingDashboardService billingDashboardService)
    {
        _db = db;
        _env = env;
        _billingDashboardService = billingDashboardService;
    }

    /// <summary>Employer: "My Billing" — their own Kaarigar + Website invoices, most recent first.</summary>
    [HttpGet("/billing/my-invoices")]
    [Authorize(Roles = "EMPLOYER")]
    public async Task<IActionResult> MyInvoices(string? invoiceType, string? status)
    {
        var session = UserSession.FromContext(HttpContext);
        if (!session.IsLoggedIn) return RedirectToAction("Login", "Account");

        ViewBag.Summary = await _billingDashboardService.GetSummaryAsync(session.UserAccountId);
        ViewBag.Invoices = await _billingDashboardService.GetInvoicesAsync(
            employerUserAccountId: session.UserAccountId, invoiceTypeFilter: invoiceType, statusFilter: status);
        ViewBag.InvoiceTypeFilter = invoiceType;
        ViewBag.StatusFilter = status;

        return View();
    }

    /// <summary>Employer or Admin: list every Kaarigar Invoice + Website Invoice pair for a given JobApplication.</summary>
    [HttpGet("/billing/job/{jobApplicationId:int}")]
    public async Task<IActionResult> ForJobApplication(int jobApplicationId)
    {
        var session = UserSession.FromContext(HttpContext);
        if (!session.IsLoggedIn) return RedirectToAction("Login", "Account");

        var kaarigarInvoice = await _db.KaarigarInvoices.FirstOrDefaultAsync(i => i.JobApplicationId == jobApplicationId);
        var websiteInvoice = await _db.WebsiteInvoices.FirstOrDefaultAsync(i => i.JobApplicationId == jobApplicationId);

        if (kaarigarInvoice == null && websiteInvoice == null)
            return NotFound("No invoices have been generated for this job yet.");

        // Employers may only see their own invoices; Admins may see any.
        if (session.RoleCd != "ADMIN")
        {
            var ownsInvoice = kaarigarInvoice?.EmployerUserAccountId == session.UserAccountId
                               || websiteInvoice?.EmployerUserAccountId == session.UserAccountId;
            if (!ownsInvoice) return Forbid();
        }

        ViewBag.KaarigarInvoice = kaarigarInvoice;
        ViewBag.WebsiteInvoice = websiteInvoice;
        return View();
    }

    [HttpGet("/billing/kaarigar-invoice/{id:int}/download")]
    public async Task<IActionResult> DownloadKaarigarInvoice(int id)
    {
        var session = UserSession.FromContext(HttpContext);
        if (!session.IsLoggedIn) return RedirectToAction("Login", "Account");

        var invoice = await _db.KaarigarInvoices.FirstOrDefaultAsync(i => i.KaarigarInvoiceId == id);
        if (invoice == null || string.IsNullOrEmpty(invoice.PdfFilePathTxt)) return NotFound();

        if (session.RoleCd != "ADMIN" && invoice.EmployerUserAccountId != session.UserAccountId)
            return Forbid();

        var fullPath = Path.Combine(_env.ContentRootPath, "wwwroot", invoice.PdfFilePathTxt);
        if (!System.IO.File.Exists(fullPath)) return NotFound("Invoice PDF not found on disk.");

        return PhysicalFile(fullPath, "application/pdf", $"{invoice.InvoiceNbr.Replace("/", "-")}.pdf");
    }

    [HttpGet("/billing/website-invoice/{id:int}/download")]
    public async Task<IActionResult> DownloadWebsiteInvoice(int id)
    {
        var session = UserSession.FromContext(HttpContext);
        if (!session.IsLoggedIn) return RedirectToAction("Login", "Account");

        var invoice = await _db.WebsiteInvoices.FirstOrDefaultAsync(i => i.WebsiteInvoiceId == id);
        if (invoice == null || string.IsNullOrEmpty(invoice.PdfFilePathTxt)) return NotFound();

        if (session.RoleCd != "ADMIN" && invoice.EmployerUserAccountId != session.UserAccountId)
            return Forbid();

        var fullPath = Path.Combine(_env.ContentRootPath, "wwwroot", invoice.PdfFilePathTxt);
        if (!System.IO.File.Exists(fullPath)) return NotFound("Invoice PDF not found on disk.");

        return PhysicalFile(fullPath, "application/pdf", $"{invoice.InvoiceNbr.Replace("/", "-")}.pdf");
    }
}
