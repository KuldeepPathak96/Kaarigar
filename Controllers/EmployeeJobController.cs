using Kaarigar.Models;
using Kaarigar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaarigar.Controllers;

/// <summary>
/// Employee-facing job discovery. Wires up the two links referenced from the
/// W-01 Employee Dashboard that previously had no controller behind them:
/// "/jobs/browse" (Screen W-03) and "/notifications".
///
/// Every action here requires the EMPLOYEE role. Take Work additionally
/// requires the employee's USER_ACCOUNT to be Admin-approved
/// (UserAccount.IsApprovedFl) — until then the employee can browse and see
/// job cards, but cannot apply. This mirrors the existing employer-side gate
/// (see IJobPostService.IsEmployerApprovedAsync / JobController).
/// </summary>
[Authorize(Roles = "EMPLOYEE")]
public class EmployeeJobController : Controller
{
    private readonly IEmployeeJobService _jobService;
    private readonly IEmployeeNotificationService _notificationService;
    private readonly ILearningVideoService _learningVideoService;

    public EmployeeJobController(IEmployeeJobService jobService, IEmployeeNotificationService notificationService, ILearningVideoService learningVideoService)
    {
        _jobService = jobService;
        _notificationService = notificationService;
        _learningVideoService = learningVideoService;
    }

    [HttpGet("/learning-videos")]
    public async Task<IActionResult> LearningVideos(int? skillId)
    {
        var vm = await _learningVideoService.GetEmployeeVideosAsync(skillId);
        return View(vm);
    }

    // ── W-03: BROWSE AVAILABLE JOBS ──────────────────────────────────────────

    [HttpGet("/jobs/browse")]
    public async Task<IActionResult> Browse(string? skill, string? city, decimal? minWage, decimal? maxWage, bool matchMyProfile = false)
    {
        var session = UserSession.FromContext(HttpContext);

        if (TempData["SuccessMessage"] is string success) ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error) ViewBag.ErrorMessage = error;

        var vm = await _jobService.BrowseAsync(session.UserAccountId, skill, city, minWage, maxWage, matchMyProfile);
        return View(vm);
    }

    // ── W-03/W-04: TAKE WORK (one click, cannot be undone) ───────────────────

    [HttpPost("/jobs/{id:int}/take-work")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TakeWork(int id)
    {
        var session = UserSession.FromContext(HttpContext);

        var result = await _jobService.TakeWorkAsync(
            session.UserAccountId, id, HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Browse));
    }

    // ── W-05: MY APPLICATIONS ────────────────────────────────────────────────

    [HttpGet("/applications")]
    public async Task<IActionResult> MyApplications()
    {
        var session = UserSession.FromContext(HttpContext);

        if (TempData["SuccessMessage"] is string success) ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error) ViewBag.ErrorMessage = error;

        var vm = await _jobService.GetMyApplicationsAsync(session.UserAccountId);
        return View(vm);
    }

    // ── W-06: ENTER OTP (employer-generated, Kaarigar-entered) ───────────────

    [HttpPost("/applications/{id:int}/verify-otp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(int id, string otpCd)
    {
        var session = UserSession.FromContext(HttpContext);

        var result = await _jobService.VerifyOtpAsync(session.UserAccountId, id, otpCd);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(MyApplications));
    }

    // ── NOTIFICATIONS (W-01 bell banner link) ───────────────────────────────

    [HttpGet("/notifications")]
    public async Task<IActionResult> Notifications()
    {
        var session = UserSession.FromContext(HttpContext);
        var vm = await _notificationService.GetForEmployeeAsync(session.UserAccountId);
        return View(vm);
    }
}
