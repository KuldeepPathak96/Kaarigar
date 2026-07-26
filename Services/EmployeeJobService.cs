using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class EmployeeJobService : IEmployeeJobService
{
    private readonly IEmployeeJobDao _dao;
    private readonly IWhatsAppNotificationService _whatsAppService;
    private readonly ILogger<EmployeeJobService> _logger;

    public EmployeeJobService(
        IEmployeeJobDao dao, IWhatsAppNotificationService whatsAppService, ILogger<EmployeeJobService> logger)
    {
        _dao = dao;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<JobBrowseViewModel> BrowseAsync(
        int employeeUserAccountId, string? skillFilter, string? cityFilter, decimal? minWage, decimal? maxWage,
        bool matchesMyProfileOnly = false)
    {
        var isApproved = await _dao.IsEmployeeApprovedAsync(employeeUserAccountId);
        var profile = await _dao.GetEmployeeProfileAsync(employeeUserAccountId);
        var mySkillIds = (await _dao.GetEmployeeSkillIdsAsync(employeeUserAccountId)).ToHashSet();
        var appliedIds = await _dao.GetAppliedJobPostIdsAsync(employeeUserAccountId);
        var skillOptions = await _dao.GetActiveSkillOptionsAsync();

        var allActiveJobs = await _dao.GetActiveJobPostsAsync();

        var categoryNames = await _dao.GetEmployerBusinessCategoryNamesAsync(
            allActiveJobs.Select(j => j.EmployerUserAccountId));

        var hasLocation = profile?.LatitudeNbr != null && profile?.LongitudeNbr != null;
        int? radiusKm = profile?.PreferredRadiusNbr; // null = "Any"

        int.TryParse(skillFilter, out var skillFilterId);

        // Job seekers see every active job post by default — skill/location
        // matching is informational (badges + default sort order) rather
        // than a hard filter, so no relevant job is ever hidden from them.
        // "Match My Profile" (matchesMyProfileOnly=true) turns the same
        // signals into an actual filter for anyone who wants it.
        var candidates = allActiveJobs
            .Select(j => new
            {
                Job = j,
                DistanceKm = DistanceKm(j.LatitudeNbr, j.LongitudeNbr, profile?.LatitudeNbr, profile?.LongitudeNbr),
                MatchesSkills = !j.JobSkills.Any() || j.JobSkills.Any(js => mySkillIds.Contains(js.SkillId)),
            })
            .ToList();

        if (matchesMyProfileOnly)
        {
            candidates = candidates
                .Where(x => x.MatchesSkills)
                .Where(x => !hasLocation || radiusKm == null || (x.DistanceKm.HasValue && x.DistanceKm.Value <= radiusKm.Value))
                .ToList();
        }

        // Optional user-chosen filters on top of the (now non-exclusionary) automatic matching.
        if (skillFilterId > 0)
            candidates = candidates.Where(x => x.Job.JobSkills.Any(js => js.SkillId == skillFilterId)).ToList();

        if (!string.IsNullOrWhiteSpace(cityFilter))
            candidates = candidates.Where(x =>
                x.Job.LocationAddressTxt != null &&
                x.Job.LocationAddressTxt.Contains(cityFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        if (minWage.HasValue)
            candidates = candidates.Where(x => x.Job.HourlyWageAmt == null || x.Job.HourlyWageAmt >= minWage.Value).ToList();

        if (maxWage.HasValue)
            candidates = candidates.Where(x => x.Job.HourlyWageAmt == null || x.Job.HourlyWageAmt <= maxWage.Value).ToList();

        var jobs = candidates
            .OrderByDescending(x => x.MatchesSkills)
            .ThenBy(x => x.DistanceKm ?? double.MaxValue)
            .ThenByDescending(x => x.Job.CreatedTs)
            .Select(x =>
            {
                categoryNames.TryGetValue(x.Job.EmployerUserAccountId, out var categoryName);

                return new JobBrowseListItemViewModel
                {
                    JobPostId = x.Job.JobPostId,
                    JobTitle = x.Job.JobTitle,
                    EmployerDisplayName = string.IsNullOrWhiteSpace(categoryName)
                        ? "Registered Employer"
                        : $"{categoryName} Employer",
                    RequiredSkillNames = x.Job.JobSkills.Select(js => js.Skill?.SkillName ?? string.Empty)
                                                          .Where(n => n.Length > 0).ToList(),
                    HourlyWageAmt = x.Job.HourlyWageAmt,
                    StartDt = x.Job.StartDt,
                    DurationHourNbr = x.Job.DurationHourNbr,
                    LocationLabel = ExtractCityLabel(x.Job.LocationAddressTxt),
                    DistanceKm = x.DistanceKm,
                    PostedTs = x.Job.CreatedTs,
                    HasApplied = appliedIds.Contains(x.Job.JobPostId),
                    MatchesSkills = x.MatchesSkills,
                };
            })
            .ToList();

        return new JobBrowseViewModel
        {
            Jobs = jobs,
            SkillOptions = skillOptions,
            SkillFilter = skillFilter,
            CityFilter = cityFilter,
            MinWage = minWage,
            MaxWage = maxWage,
            MatchesMyProfileOnly = matchesMyProfileOnly,
            PreferredRadiusKm = radiusKm,
            HasLocation = hasLocation,
            IsApproved = isApproved,
        };
    }

    public async Task<ServiceResult> ExpressInterestAsync(int employeeUserAccountId, int jobPostId, string? ipAddress)
    {
        if (!await _dao.IsEmployeeApprovedAsync(employeeUserAccountId))
            return new ServiceResult(false, "Your account is pending Admin approval. You'll be able to apply to jobs once approved.");

        var job = await _dao.GetActiveJobPostByIdAsync(jobPostId);
        if (job == null)
            return new ServiceResult(false, "This job is no longer available.");

        if (await _dao.HasAppliedAsync(employeeUserAccountId, jobPostId))
            return new ServiceResult(false, "You've already expressed interest in this job.");

        await _dao.InsertApplicationAsync(employeeUserAccountId, jobPostId, ipAddress);

        _logger.LogInformation("Employee {EmployeeId} expressed interest in JobPostId={JobPostId}",
            employeeUserAccountId, jobPostId);

        return new ServiceResult(true, "Interest expressed! The employer can now see your profile.");
    }

    // ── W-05: MY APPLICATIONS ────────────────────────────────────────────────

    public async Task<EmployeeApplicationsViewModel> GetMyApplicationsAsync(int employeeUserAccountId)
    {
        var applications = await _dao.GetApplicationsForEmployeeAsync(employeeUserAccountId);

        var categoryNames = await _dao.GetEmployerBusinessCategoryNamesAsync(
            applications.Select(a => a.JobPost.EmployerUserAccountId));

        var items = applications.Select(a =>
        {
            categoryNames.TryGetValue(a.JobPost.EmployerUserAccountId, out var categoryName);

            return new EmployeeApplicationListItemViewModel
            {
                JobApplicationId = a.JobApplicationId,
                JobPostId = a.JobPostId,
                JobTitle = a.JobPost.JobTitle,
                EmployerDisplayName = string.IsNullOrWhiteSpace(categoryName)
                    ? "Registered Employer"
                    : $"{categoryName} Employer",
                HourlyWageAmt = a.JobPost.HourlyWageAmt,
                LocationLabel = ExtractCityLabel(a.JobPost.LocationAddressTxt),
                MapUrl = a.JobPost.GoogleMapsUrl,
                AppliedTs = a.AppliedTs,
                StatusCd = a.StatusCd,
            };
        }).ToList();

        return new EmployeeApplicationsViewModel { Applications = items };
    }

    // ── W-06: ENTER OTP (employer-generated, Kaarigar-entered) ───────────────

    public async Task<ServiceResult> VerifyOtpAsync(int employeeUserAccountId, int jobApplicationId, string otpCd)
    {
        if (string.IsNullOrWhiteSpace(otpCd))
            return new ServiceResult(false, "Please enter the OTP shared by the employer.");

        var application = await _dao.GetApplicationForEmployeeAsync(jobApplicationId, employeeUserAccountId);
        if (application == null)
            return new ServiceResult(false, "Application not found.");

        if (application.StatusCd == "COMPLETED")
            return new ServiceResult(false, "This job has already been marked complete.");

        // JOB_STARTED not yet reached → expecting the Job Starting OTP; once
        // JOB_STARTED → expecting the Satisfaction OTP.
        var expectingType = application.StatusCd == "JOB_STARTED" ? OtpType.Satisfaction : OtpType.JobStart;
        var nextStatus = expectingType == OtpType.JobStart ? "JOB_STARTED" : "COMPLETED";

        var otp = await _dao.GetValidOtpAsync(employeeUserAccountId, application.JobPostId, expectingType, otpCd.Trim());
        if (otp == null)
            return new ServiceResult(false, "Invalid or expired OTP. Please ask the employer to generate it again.");

        await _dao.MarkOtpUsedAsync(otp.OtpRecordId);
        await _dao.AdvanceStatusAsync(jobApplicationId, nextStatus);

        _logger.LogInformation(
            "Kaarigar {EmployeeId} entered {OtpType} OTP for JobApplicationId={JobApplicationId}",
            employeeUserAccountId, expectingType, jobApplicationId);

        return new ServiceResult(
            true,
            nextStatus == "JOB_STARTED"
                ? "Job started! Have a safe and productive day."
                : "Job marked complete. Thank you!");
    }

    /// <summary>Best-effort "city" label from the free-text job address — takes the last comma-separated segment.</summary>
    private static string? ExtractCityLabel(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : address;
    }

    /// <summary>Great-circle (Haversine) distance in km between the job site and the employee's saved location.</summary>
    private static double? DistanceKm(decimal? lat1, decimal? lng1, decimal? lat2, decimal? lng2)
    {
        if (lat1 == null || lng1 == null || lat2 == null || lng2 == null) return null;

        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians((double)(lat2.Value - lat1.Value));
        var dLng = ToRadians((double)(lng2.Value - lng1.Value));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians((double)lat1.Value)) * Math.Cos(ToRadians((double)lat2.Value)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(earthRadiusKm * c, 1);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
