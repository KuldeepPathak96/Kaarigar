using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class JobPostService : IJobPostService
{
    private readonly IJobPostDao _dao;
    private readonly IEmployerProfileDao _employerProfileDao;
    private readonly IHourlyRateOptionDao _hourlyRateOptionDao;
    private readonly IJobNotificationQueue _notificationQueue;
    private readonly ILogger<JobPostService> _logger;

    public JobPostService(
        IJobPostDao dao,
        IEmployerProfileDao employerProfileDao,
        IHourlyRateOptionDao hourlyRateOptionDao,
        IJobNotificationQueue notificationQueue,
        ILogger<JobPostService> logger)
    {
        _dao = dao;
        _employerProfileDao = employerProfileDao;
        _hourlyRateOptionDao = hourlyRateOptionDao;
        _notificationQueue = notificationQueue;
        _logger = logger;
    }

    public async Task<PostJobViewModel> GetNewJobFormAsync(int employerUserAccountId)
    {
        var skills = await _dao.GetActiveSkillsAsync();
        var loaded = await _employerProfileDao.GetProfileAsync(employerUserAccountId);
        var rateOptions = await _hourlyRateOptionDao.GetActiveAsync();

        return new PostJobViewModel
        {
            ContactNbr = loaded?.User.ContactNbr ?? string.Empty,
            // Prefilled from the employer's own Profile so they don't have to
            // retype it every time — still editable for jobs at a different site.
            LocationAddressTxt = BuildAddress(loaded?.Profile),
            LatitudeNbr = loaded?.Profile?.LatitudeNbr,
            LongitudeNbr = loaded?.Profile?.LongitudeNbr,
            StartDt = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(9, 0, 0),
            SkillOptions = ToSkillOptions(skills),
            JobTitleOptions = ToTitleOptions(skills),
            HourlyRateOptionsList = ToRateOptions(rateOptions),
        };
    }

    public async Task<PostJobViewModel?> GetJobForEditAsync(int jobPostId, int employerUserAccountId)
    {
        var jobPost = await _dao.GetJobPostForEmployerAsync(jobPostId, employerUserAccountId);
        if (jobPost == null) return null;

        var skills = await _dao.GetActiveSkillsAsync();
        var rateOptions = await _hourlyRateOptionDao.GetActiveAsync();

        return new PostJobViewModel
        {
            JobPostId = jobPost.JobPostId,
            JobTitle = jobPost.JobTitle,
            RequiredWorkerNbr = jobPost.RequiredWorkerNbr,
            DescriptionTxt = jobPost.DescriptionTxt,
            SelectedSkillIds = jobPost.JobSkills.Select(js => js.SkillId).ToList(),
            SkillOptions = ToSkillOptions(skills),
            JobTitleOptions = ToTitleOptions(skills),
            HourlyRateOptionsList = ToRateOptions(rateOptions),
            HourlyWageAmt = jobPost.HourlyWageAmt,
            StartDt = jobPost.StartDt,
            DurationHourNbr = jobPost.DurationHourNbr,
            StartTime = jobPost.StartTime,
            LocationAddressTxt = jobPost.LocationAddressTxt,
            LatitudeNbr = jobPost.LatitudeNbr,
            LongitudeNbr = jobPost.LongitudeNbr,
            ContactNbr = jobPost.ContactNbr ?? string.Empty,
            StatusCd = jobPost.StatusCd,
        };
    }

    public async Task<bool> IsEmployerApprovedAsync(int employerUserAccountId)
    {
        var loaded = await _employerProfileDao.GetProfileAsync(employerUserAccountId);
        return loaded?.User.IsApprovedFl ?? false;
    }

    public async Task<ServiceResult> CreateJobAsync(int employerUserAccountId, PostJobViewModel model, string? ipAddress = null)
    {
        if (!await IsEmployerApprovedAsync(employerUserAccountId))
            return new ServiceResult(false, "Your account is pending Admin approval. You will be able to post jobs once approved.");

        var validation = Validate(model);
        if (validation != null) return new ServiceResult(false, validation);

        var jobPost = new JobPost
        {
            EmployerUserAccountId = employerUserAccountId,
            JobTitle = model.JobTitle.Trim(),
            DescriptionTxt = model.DescriptionTxt?.Trim(),
            RequiredWorkerNbr = model.RequiredWorkerNbr,
            HourlyWageAmt = model.HourlyWageAmt,
            StartDt = model.StartDt,
            DurationHourNbr = model.DurationHourNbr,
            StartTime = model.StartTime,
            LocationAddressTxt = model.LocationAddressTxt!.Trim(),
            LatitudeNbr = model.LatitudeNbr,
            LongitudeNbr = model.LongitudeNbr,
            ContactNbr = model.ContactNbr.Trim(),
            StatusCd = "ACTIVE",
            CreatedBy = "JOB_POST_CREATE",
            CreatedIpAddr = ipAddress,
            CreatedTs = DateTime.UtcNow,
        };

        await _dao.CreateJobPostAsync(jobPost, model.SelectedSkillIds);

        // Queue WhatsApp notifications to matching employees — handled by a
        // background worker (JobNotificationBackgroundService) so "Post Job"
        // returns to the employer immediately even when many employees
        // match, instead of blocking on the fan-out here.
        try
        {
            _notificationQueue.QueueJobPost(jobPost.JobPostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue job-match notifications for JobPostId={Id}", jobPost.JobPostId);
        }

        _logger.LogInformation("Job post created: JobPostId={Id} by EmployerUserAccountId={EmployerId}",
            jobPost.JobPostId, employerUserAccountId);

        return new ServiceResult(true, "Your job has been posted and matching workers are being notified.");
    }

    public async Task<ServiceResult> UpdateJobAsync(int employerUserAccountId, PostJobViewModel model, string? ipAddress = null)
    {
        if (model.JobPostId == null)
            return new ServiceResult(false, "Job post not specified.");

        if (!await IsEmployerApprovedAsync(employerUserAccountId))
            return new ServiceResult(false, "Your account is pending Admin approval. You will be able to post jobs once approved.");

        var validation = Validate(model);
        if (validation != null) return new ServiceResult(false, validation);

        var existing = await _dao.GetJobPostForEmployerAsync(model.JobPostId.Value, employerUserAccountId);
        if (existing == null)
            return new ServiceResult(false, "Job post not found.");

        var jobPost = new JobPost
        {
            JobPostId = model.JobPostId.Value,
            EmployerUserAccountId = employerUserAccountId,
            JobTitle = model.JobTitle.Trim(),
            DescriptionTxt = model.DescriptionTxt?.Trim(),
            RequiredWorkerNbr = model.RequiredWorkerNbr,
            HourlyWageAmt = model.HourlyWageAmt,
            StartDt = model.StartDt,
            DurationHourNbr = model.DurationHourNbr,
            StartTime = model.StartTime,
            LocationAddressTxt = model.LocationAddressTxt!.Trim(),
            LatitudeNbr = model.LatitudeNbr,
            LongitudeNbr = model.LongitudeNbr,
            ContactNbr = model.ContactNbr.Trim(),
        };

        await _dao.UpdateJobPostAsync(jobPost, model.SelectedSkillIds);

        _logger.LogInformation("Job post updated: JobPostId={Id}", model.JobPostId);
        return new ServiceResult(true, "Job post updated successfully.");
    }

    public async Task<ServiceResult> ToggleStatusAsync(int jobPostId, int employerUserAccountId, string newStatusCd)
    {
        if (newStatusCd != "ACTIVE" && newStatusCd != "PAUSED" && newStatusCd != "CLOSED")
            return new ServiceResult(false, "Invalid status.");

        var existing = await _dao.GetJobPostForEmployerAsync(jobPostId, employerUserAccountId);
        if (existing == null)
            return new ServiceResult(false, "Job post not found.");

        await _dao.UpdateStatusAsync(jobPostId, employerUserAccountId, newStatusCd);

        var label = newStatusCd switch
        {
            "ACTIVE" => "activated",
            "PAUSED" => "paused",
            _ => "closed",
        };
        return new ServiceResult(true, $"Job post {label}.");
    }

    public Task<List<JobPostListItemViewModel>> GetMyJobPostsAsync(int employerUserAccountId) =>
        _dao.GetMyJobPostsAsync(employerUserAccountId);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? Validate(PostJobViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.JobTitle))
            return "Please select a job title.";

        if (string.IsNullOrWhiteSpace(model.LocationAddressTxt))
            return "Job location is required — it's used to match nearby workers.";

        if (model.StartDt.HasValue && model.StartDt.Value.Date < DateTime.Today)
            return "Job start date cannot be in the past.";

        if (!model.DurationHourNbr.HasValue || model.DurationHourNbr.Value < 1)
            return "Enter a valid job duration in hours.";

        return null;
    }

    /// <summary>Employer's saved Profile address, e.g. "12 MG Road, Alkapuri, Vadodara" — best-effort from whichever parts are filled in.</summary>
    private static string? BuildAddress(EmployerProfile? profile)
    {
        if (profile == null) return null;

        var parts = new[] { profile.AddressTxt, profile.AreaAddressTxt, profile.CityName }
            .Where(p => !string.IsNullOrWhiteSpace(p));

        var combined = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    private static List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> ToSkillOptions(List<Skill> skills) =>
        skills.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(s.SkillName, s.SkillId.ToString())).ToList();

    /// <summary>Job Title dropdown — same Skill list, but the option VALUE is the skill's name (that's what gets saved as JobTitle).</summary>
    private static List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> ToTitleOptions(List<Skill> skills) =>
        skills.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(s.SkillName, s.SkillName)).ToList();

    private static List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> ToRateOptions(List<HourlyRateOption> rates) =>
        rates.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
            r.RateLabelTxt, r.HourlyRateAmt.ToString(System.Globalization.CultureInfo.InvariantCulture))).ToList();
}
