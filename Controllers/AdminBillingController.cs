using Kaarigar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaarigar.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminBillingController : Controller
{
    private readonly IBillingDashboardService _billingDashboardService;

    public AdminBillingController(IBillingDashboardService billingDashboardService)
    {
        _billingDashboardService = billingDashboardService;
    }

    [HttpGet("/admin/billing")]
    public async Task<IActionResult> Index(string? invoiceType, string? status, DateTime? fromDt, DateTime? toDt)
    {
        ViewBag.Summary = await _billingDashboardService.GetSummaryAsync();
        ViewBag.Invoices = await _billingDashboardService.GetInvoicesAsync(
            invoiceTypeFilter: invoiceType, statusFilter: status, fromDt: fromDt, toDt: toDt);

        ViewBag.InvoiceTypeFilter = invoiceType;
        ViewBag.StatusFilter = status;
        ViewBag.FromDt = fromDt;
        ViewBag.ToDt = toDt;

        return View();
    }

    [HttpPost("/admin/billing/mark-paid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(string invoiceType, int invoiceId)
    {
        var session = UserSession.FromContext(HttpContext);
        var ok = await _billingDashboardService.MarkPaidAsync(invoiceType, invoiceId, session.FullName ?? "ADMIN");
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Invoice marked as paid." : "Could not update invoice.";
        return RedirectToAction("Index");
    }

    [HttpPost("/admin/billing/resend-whatsapp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendWhatsApp(string invoiceType, int invoiceId)
    {
        var ok = await _billingDashboardService.ResendWhatsAppAsync(invoiceType, invoiceId);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Invoice resent via WhatsApp." : "Could not resend — check WhatsApp configuration.";
        return RedirectToAction("Index");
    }
}
