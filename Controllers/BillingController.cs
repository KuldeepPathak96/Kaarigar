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

    /// <summary>Employer, Admin, or the Worker themselves: the Website Invoice + Employee Invoice pair for a given JobApplication.</summary>
    [HttpGet("/billing/job/{jobApplicationId:int}")]
    public async Task<IActionResult> ForJobApplication(int jobApplicationId)
    {
        var session = UserSession.FromContext(HttpContext);
        if (!session.IsLoggedIn) return RedirectToAction("Login", "Account");

        var application = await _db.JobApplications
            .Include(a => a.JobPost)
            .FirstOrDefaultAsync(a => a.JobApplicationId == jobApplicationId);

        if (application == null) return NotFound("Job application not found.");

        var employerUserAccountId = application.JobPost.EmployerUserAccountId;
        var isEmployer = employerUserAccountId == session.UserAccountId;
        var isWorker = application.EmployeeUserAccountId == session.UserAccountId;

        // Employers and the worker themselves may see their own bills; Admins may see any.
        if (session.RoleCd != "ADMIN" && !isEmployer && !isWorker)
            return Forbid();

        var kaarigarInvoice = await _db.KaarigarInvoices.FirstOrDefaultAsync(i => i.JobApplicationId == jobApplicationId);
        var websiteInvoice = await _db.WebsiteInvoices.FirstOrDefaultAsync(i => i.JobApplicationId == jobApplicationId);
        var worker = await _db.UserAccounts.FindAsync(application.EmployeeUserAccountId);

        ViewBag.JobTitle = application.JobPost.JobTitle;
        ViewBag.WorkerName = worker != null ? $"{worker.FirstName} {worker.LastName}".Trim() : null;
        ViewBag.StillGenerating = kaarigarInvoice == null && websiteInvoice == null;
        ViewBag.KaarigarInvoice = kaarigarInvoice;
        ViewBag.WebsiteInvoice = websiteInvoice;
        // Only the worker who did this job can mark their own Employee Invoice collected.
        ViewBag.CanMarkCollected = isWorker;
        return View();
    }

    /// <summary>
    /// Worker: "I received this payment from the Employer directly." Only
    /// the worker on this specific job can mark it — not the Employer, not
    /// anyone else. Admin can also mark it on the worker's behalf if needed
    /// (e.g. a support request), since Admin can see any invoice.
    /// </summary>
    [HttpPost("/billing/employee-invoice/{id:int}/mark-collected")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkEmployeeInvoiceCollected(int id)
    {
        var session = UserSession.FromContext(HttpContext);
        if (!session.IsLoggedIn) return RedirectToAction("Login", "Account");

        var invoice = await _db.WebsiteInvoices.FirstOrDefaultAsync(i => i.WebsiteInvoiceId == id);
        if (invoice == null) return NotFound();

        if (session.RoleCd != "ADMIN" && invoice.WorkerUserAccountId != session.UserAccountId)
            return Forbid();

        if (invoice.StatusCd != "COLLECTED")
        {
            invoice.StatusCd = "COLLECTED";
            invoice.CollectedByEmployeeTs = DateTime.UtcNow;
            invoice.PaidTs = DateTime.UtcNow;
            invoice.UpdatedBy = session.RoleCd == "ADMIN" ? "ADMIN_MARK_COLLECTED" : "EMPLOYEE_MARK_COLLECTED";
            invoice.UpdatedTs = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Marked as collected — thanks for confirming!";
        }

        return RedirectToAction(nameof(ForJobApplication), new { jobApplicationId = invoice.JobApplicationId });
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
