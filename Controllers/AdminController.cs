using Kaarigar.Models;
using Kaarigar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaarigar.Controllers;

/// <summary>
/// Admin-only screen for managing fields that only Admin is allowed to
/// add/amend — starting with the Business Category dropdown used on the
/// Employer Profile screen (E-02).
/// </summary>
[Authorize(Roles = "ADMIN")]
public class AdminController : Controller
{
    private readonly IBusinessCategoryService _businessCategoryService;
    private readonly IHourlyRateOptionService _hourlyRateOptionService;
    private readonly IAdminEmployerService _adminEmployerService;
    private readonly IAdminEmployeeService _adminEmployeeService;
    private readonly IAdminJobPostService _adminJobPostService;
    private readonly IAdminNotificationLogService _adminNotificationLogService;
    private readonly ILearningVideoService _learningVideoService;

    public AdminController(
        IBusinessCategoryService businessCategoryService,
        IHourlyRateOptionService hourlyRateOptionService,
        IAdminEmployerService adminEmployerService,
        IAdminEmployeeService adminEmployeeService,
        IAdminJobPostService adminJobPostService,
        IAdminNotificationLogService adminNotificationLogService,
        ILearningVideoService learningVideoService)
    {
        _businessCategoryService = businessCategoryService;
        _hourlyRateOptionService = hourlyRateOptionService;
        _adminEmployerService = adminEmployerService;
        _adminEmployeeService = adminEmployeeService;
        _adminJobPostService = adminJobPostService;
        _adminNotificationLogService = adminNotificationLogService;
        _learningVideoService = learningVideoService;
    }

    // ── LEARNING VIDEOS (admin upload, skill-based) ──────────────────────────

    [HttpGet("/admin/learning-videos")]
    public async Task<IActionResult> LearningVideos()
    {
        var vm = await _learningVideoService.GetAdminListAsync();

        if (TempData["SuccessMessage"] is string success) ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error) ViewBag.ErrorMessage = error;

        return View(vm);
    }

    [HttpPost("/admin/learning-videos/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadLearningVideo(int skillId, string title, string? descriptionTxt, IFormFile videoFile)
    {
        var session = UserSession.FromContext(HttpContext);
        var result = await _learningVideoService.UploadAsync(skillId, title, descriptionTxt, videoFile, session.FullName ?? "ADMIN");
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(LearningVideos));
    }

    [HttpPost("/admin/learning-videos/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLearningVideo(int learningVideoId)
    {
        var result = await _learningVideoService.DeleteAsync(learningVideoId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(LearningVideos));
    }

    [HttpPost("/admin/learning-videos/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLearningVideo(int learningVideoId, bool isActive)
    {
        var result = await _learningVideoService.SetActiveAsync(learningVideoId, isActive);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(LearningVideos));
    }

    [HttpGet("/admin/business-categories")]
    public async Task<IActionResult> BusinessCategories()
    {
        var vm = new BusinessCategoryAdminViewModel
        {
            Categories = await _businessCategoryService.GetAllAsync(),
        };

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        return View(vm);
    }

    [HttpPost("/admin/business-categories/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBusinessCategory(string newCategoryName)
    {
        var session = UserSession.FromContext(HttpContext);
        var result = await _businessCategoryService.AddAsync(
            newCategoryName,
            adminUser: session.FullName ?? "ADMIN",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(BusinessCategories));
    }

    [HttpPost("/admin/business-categories/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBusinessCategory(int businessCategoryId)
    {
        var result = await _businessCategoryService.RemoveAsync(businessCategoryId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(BusinessCategories));
    }

    [HttpPost("/admin/business-categories/reactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateBusinessCategory(int businessCategoryId)
    {
        var result = await _businessCategoryService.ReactivateAsync(businessCategoryId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(BusinessCategories));
    }

    // ── HOURLY RATE OPTIONS (Post a Job "Hourly Rate" dropdown) ─────────────

    [HttpGet("/admin/hourly-rates")]
    public async Task<IActionResult> HourlyRates()
    {
        var vm = new HourlyRateOptionAdminViewModel
        {
            RateOptions = await _hourlyRateOptionService.GetAllAsync(),
        };

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        return View(vm);
    }

    [HttpPost("/admin/hourly-rates/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHourlyRate(string rateLabelTxt, decimal hourlyRateAmt)
    {
        var session = UserSession.FromContext(HttpContext);
        var result = await _hourlyRateOptionService.AddAsync(rateLabelTxt, hourlyRateAmt, session.FullName ?? "ADMIN");
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(HourlyRates));
    }

    [HttpPost("/admin/hourly-rates/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHourlyRate(int rateOptionId, string rateLabelTxt, decimal hourlyRateAmt)
    {
        var session = UserSession.FromContext(HttpContext);
        var result = await _hourlyRateOptionService.UpdateAsync(rateOptionId, rateLabelTxt, hourlyRateAmt, session.FullName ?? "ADMIN");
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(HourlyRates));
    }

    [HttpPost("/admin/hourly-rates/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveHourlyRate(int rateOptionId)
    {
        var result = await _hourlyRateOptionService.RemoveAsync(rateOptionId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(HourlyRates));
    }

    [HttpPost("/admin/hourly-rates/reactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateHourlyRate(int rateOptionId)
    {
        var result = await _hourlyRateOptionService.ReactivateAsync(rateOptionId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(HourlyRates));
    }

    // ── A-03: MANAGE EMPLOYERS ───────────────────────────────────────────────

    [HttpGet("/admin/employers")]
    public async Task<IActionResult> Employers(string? search, string? city, string? status)
    {
        var vm = await _adminEmployerService.SearchAsync(search, city, status);

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        return View(vm);
    }

    [HttpGet("/admin/employers/{id:int}")]
    public async Task<IActionResult> EmployerDetail(int id)
    {
        var vm = await _adminEmployerService.GetDetailAsync(id);
        if (vm == null)
        {
            TempData["ErrorMessage"] = "Employer not found.";
            return RedirectToAction(nameof(Employers));
        }

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        return View(vm);
    }

    [HttpPost("/admin/employers/{id:int}/business-proof/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveBusinessProof(int id)
    {
        var result = await _adminEmployerService.ApproveBusinessProofAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(EmployerDetail), new { id });
    }

    [HttpPost("/admin/employers/{id:int}/business-proof/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBusinessProof(int id)
    {
        var result = await _adminEmployerService.RejectBusinessProofAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(EmployerDetail), new { id });
    }

    [HttpPost("/admin/employers/{id:int}/toggle-active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmployerActive(int id)
    {
        var result = await _adminEmployerService.ToggleActiveAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Employers));
    }

    [HttpPost("/admin/employers/{id:int}/toggle-approval")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmployerApproval(int id)
    {
        var result = await _adminEmployerService.ToggleApprovalAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Employers));
    }

    [HttpPost("/admin/employers/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmployer(int id)
    {
        var result = await _adminEmployerService.DeleteAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Employers));
    }

    // ── A-04: MANAGE EMPLOYEES ───────────────────────────────────────────────

    [HttpGet("/admin/employees")]
    public async Task<IActionResult> Employees(string? search, string? city, string? skill, string? status)
    {
        var vm = await _adminEmployeeService.SearchAsync(search, city, skill, status);

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        return View(vm);
    }

    [HttpGet("/admin/employees/{id:int}")]
    public async Task<IActionResult> EmployeeDetail(int id)
    {
        var vm = await _adminEmployeeService.GetDetailAsync(id);
        if (vm == null)
        {
            TempData["ErrorMessage"] = "Employee not found.";
            return RedirectToAction(nameof(Employees));
        }

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        return View(vm);
    }

    [HttpPost("/admin/employees/{employeeId:int}/documents/{documentId:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveEmployeeDocument(int employeeId, int documentId)
    {
        var session = UserSession.FromContext(HttpContext);
        var result = await _adminEmployeeService.ApproveDocumentAsync(documentId, session.UserAccountId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(EmployeeDetail), new { id = employeeId });
    }

    [HttpPost("/admin/employees/{employeeId:int}/documents/{documentId:int}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectEmployeeDocument(int employeeId, int documentId, string? reason)
    {
        var session = UserSession.FromContext(HttpContext);
        var result = await _adminEmployeeService.RejectDocumentAsync(documentId, session.UserAccountId, reason);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(EmployeeDetail), new { id = employeeId });
    }

    [HttpPost("/admin/employees/{id:int}/toggle-active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmployeeActive(int id)
    {
        var result = await _adminEmployeeService.ToggleActiveAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Employees));
    }

    [HttpPost("/admin/employees/{id:int}/toggle-approval")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmployeeApproval(int id)
    {
        var result = await _adminEmployeeService.ToggleApprovalAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Employees));
    }

    [HttpPost("/admin/employees/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var result = await _adminEmployeeService.DeleteAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Employees));
    }

    // ── A-05: MANAGE JOB POSTS ───────────────────────────────────────────────

    [HttpGet("/admin/jobs")]
    public async Task<IActionResult> JobPosts(string? search, string? city, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var vm = await _adminJobPostService.SearchAsync(search, city, status, fromDate, toDate);

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        return View(vm);
    }

    [HttpGet("/admin/jobs/{id:int}")]
    public async Task<IActionResult> JobPostDetail(int id)
    {
        var vm = await _adminJobPostService.GetDetailAsync(id);
        if (vm == null)
        {
            TempData["ErrorMessage"] = "Job post not found.";
            return RedirectToAction(nameof(JobPosts));
        }

        return View(vm);
    }

    [HttpPost("/admin/jobs/{id:int}/close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseJobPost(int id)
    {
        var result = await _adminJobPostService.CloseJobAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(JobPosts));
    }

    [HttpPost("/admin/jobs/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteJobPost(int id)
    {
        var result = await _adminJobPostService.DeleteAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(JobPosts));
    }

    // ── A-06: NOTIFICATION LOGS ──────────────────────────────────────────────

    [HttpGet("/admin/notifications")]
    public async Task<IActionResult> Notifications(string? search, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var vm = await _adminNotificationLogService.SearchAsync(search, status, fromDate, toDate);
        return View(vm);
    }
}
