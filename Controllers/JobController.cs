using Kaarigar.Models;
using Kaarigar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaarigar.Controllers;

[Authorize(Roles = "EMPLOYER")]
public class JobController : Controller
{
    private readonly IJobPostService _jobPostService;
    private readonly IJobApplicantService _jobApplicantService;

    public JobController(IJobPostService jobPostService, IJobApplicantService jobApplicantService)
    {
        _jobPostService = jobPostService;
        _jobApplicantService = jobApplicantService;
    }

    // ── E-03: POST A JOB ─────────────────────────────────────────────────────

    [HttpGet("/job/post")]
    public async Task<IActionResult> PostJob()
    {
        var session = UserSession.FromContext(HttpContext);

        if (!await _jobPostService.IsEmployerApprovedAsync(session.UserAccountId))
        {
            TempData["ErrorMessage"] = "Your account is pending Admin approval. You will be able to post jobs once approved.";
            return RedirectToAction(nameof(MyJobs));
        }

        var vm = await _jobPostService.GetNewJobFormAsync(session.UserAccountId);
        return View(vm);
    }

    [HttpPost("/job/post")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostJob(PostJobViewModel vm)
    {
        var session = UserSession.FromContext(HttpContext);

        if (!await _jobPostService.IsEmployerApprovedAsync(session.UserAccountId))
        {
            TempData["ErrorMessage"] = "Your account is pending Admin approval. You will be able to post jobs once approved.";
            return RedirectToAction(nameof(MyJobs));
        }

        if (!ModelState.IsValid)
        {
            await RepopulateOptionsAsync(vm);
            return View(vm);
        }

        var result = await _jobPostService.CreateJobAsync(
            session.UserAccountId, vm, HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            await RepopulateOptionsAsync(vm);
            return View(vm);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(MyJobs));
    }

    // ── EDIT (reuses the same PostJob view/form) ────────────────────────────

    [HttpGet("/job/edit/{id:int}")]
    public async Task<IActionResult> EditJob(int id)
    {
        var session = UserSession.FromContext(HttpContext);

        if (!await _jobPostService.IsEmployerApprovedAsync(session.UserAccountId))
        {
            TempData["ErrorMessage"] = "Your account is pending Admin approval. You will be able to manage jobs once approved.";
            return RedirectToAction(nameof(MyJobs));
        }

        var vm = await _jobPostService.GetJobForEditAsync(id, session.UserAccountId);

        if (vm == null)
        {
            TempData["ErrorMessage"] = "Job post not found.";
            return RedirectToAction(nameof(MyJobs));
        }

        ViewBag.IsEdit = true;
        return View("PostJob", vm);
    }

    [HttpPost("/job/edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditJob(int id, PostJobViewModel vm)
    {
        var session = UserSession.FromContext(HttpContext);
        vm.JobPostId = id;

        if (!await _jobPostService.IsEmployerApprovedAsync(session.UserAccountId))
        {
            TempData["ErrorMessage"] = "Your account is pending Admin approval. You will be able to manage jobs once approved.";
            return RedirectToAction(nameof(MyJobs));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.IsEdit = true;
            await RepopulateOptionsAsync(vm);
            return View("PostJob", vm);
        }

        var result = await _jobPostService.UpdateJobAsync(
            session.UserAccountId, vm, HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            ViewBag.IsEdit = true;
            await RepopulateOptionsAsync(vm);
            return View("PostJob", vm);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(MyJobs));
    }

    // ── E-04: MY JOB POSTS ───────────────────────────────────────────────────

    [HttpGet("/job/my-jobs")]
    public async Task<IActionResult> MyJobs()
    {
        var session = UserSession.FromContext(HttpContext);

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        var isApproved = await _jobPostService.IsEmployerApprovedAsync(session.UserAccountId);
        ViewBag.IsApproved = isApproved;

        // NOTE: approval only gates *creating*/*editing* jobs (enforced in
        // PostJob/EditJob above) — it must never hide jobs the employer
        // already has. Previously this returned an empty list whenever the
        // employer wasn't currently approved, which made existing job posts
        // disappear from "My Job Posts" (and made the dashboard look like
        // nothing had ever been posted) if Admin ever revoked approval after
        // the job was created.
        var jobs = await _jobPostService.GetMyJobPostsAsync(session.UserAccountId);
        return View(jobs);
    }

    [HttpPost("/job/toggle-status/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, string newStatusCd)
    {
        var session = UserSession.FromContext(HttpContext);

        if (!await _jobPostService.IsEmployerApprovedAsync(session.UserAccountId))
        {
            TempData["ErrorMessage"] = "Your account is pending Admin approval. You will be able to manage jobs once approved.";
            return RedirectToAction(nameof(MyJobs));
        }

        var result = await _jobPostService.ToggleStatusAsync(id, session.UserAccountId, newStatusCd);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(MyJobs));
    }

    // ── E-05: JOB DETAIL & APPLICANTS ───────────────────────────────────────

    [HttpGet("/job/applicants/{id:int}")]
    public async Task<IActionResult> JobDetail(int id)
    {
        var session = UserSession.FromContext(HttpContext);

        if (!await _jobPostService.IsEmployerApprovedAsync(session.UserAccountId))
        {
            TempData["ErrorMessage"] = "Your account is pending Admin approval. You will be able to view job applicants once approved.";
            return RedirectToAction(nameof(MyJobs));
        }

        var vm = await _jobApplicantService.GetJobDetailAsync(id, session.UserAccountId);
        if (vm == null)
        {
            TempData["ErrorMessage"] = "Job post not found.";
            return RedirectToAction(nameof(MyJobs));
        }

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        return View(vm);
    }

    [HttpPost("/job/applicant/{jobApplicationId:int}/contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactEmployee(int jobApplicationId, int jobPostId)
    {
        var session = UserSession.FromContext(HttpContext);

        var result = await _jobApplicantService.ContactEmployeeAsync(jobApplicationId, session.UserAccountId);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] =
            result.Success ? $"Employee's contact number: {result.PhoneNbr}" : result.Message;

        return RedirectToAction(nameof(JobDetail), new { id = jobPostId });
    }

    // ── EMPLOYER-GENERATED OTP (Job Starting / Satisfaction) ────────────────

    [HttpPost("/job/applicant/{jobApplicationId:int}/generate-otp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateOtp(int jobApplicationId, int jobPostId, string otpTypeCd)
    {
        var session = UserSession.FromContext(HttpContext);

        var result = await _jobApplicantService.GenerateOtpAsync(
            jobApplicationId, session.UserAccountId, otpTypeCd, HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(JobDetail), new { id = jobPostId });
    }

    [HttpPost("/job/applicant/{jobApplicationId:int}/rate")]
    public async Task<IActionResult> SubmitRating(int jobApplicationId, int jobPostId, byte ratingNbr, string? reviewTxt)
    {
        var session = UserSession.FromContext(HttpContext);

        var result = await _jobApplicantService.SubmitRatingAsync(jobApplicationId, session.UserAccountId, ratingNbr, reviewTxt);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(JobDetail), new { id = jobPostId });
    }

    // ── E-06: EMPLOYEE PROFILE VIEW (read-only) ─────────────────────────────

    [HttpGet("/job/applicant/{jobApplicationId:int}")]
    public async Task<IActionResult> ApplicantProfile(int jobApplicationId)
    {
        var session = UserSession.FromContext(HttpContext);

        if (!await _jobPostService.IsEmployerApprovedAsync(session.UserAccountId))
        {
            TempData["ErrorMessage"] = "Your account is pending Admin approval. You will be able to view applicant profiles once approved.";
            return RedirectToAction(nameof(MyJobs));
        }

        var vm = await _jobApplicantService.GetApplicantProfileAsync(jobApplicationId, session.UserAccountId);
        if (vm == null)
        {
            TempData["ErrorMessage"] = "Applicant not found.";
            return RedirectToAction(nameof(MyJobs));
        }

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        return View(vm);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task RepopulateOptionsAsync(PostJobViewModel vm)
    {
        var session = UserSession.FromContext(HttpContext);
        var fresh = await _jobPostService.GetNewJobFormAsync(session.UserAccountId);
        vm.SkillOptions = fresh.SkillOptions;
        vm.JobTitleOptions = fresh.JobTitleOptions;
        vm.HourlyRateOptionsList = fresh.HourlyRateOptionsList;
    }
}
