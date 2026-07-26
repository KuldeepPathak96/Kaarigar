using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class JobApplicantService : IJobApplicantService
{
    private readonly IJobApplicantDao _dao;
    private readonly IEmployerProfileDao _employerProfileDao;
    private readonly IWhatsAppNotificationService _whatsAppService;
    private readonly ILogger<JobApplicantService> _logger;

    public JobApplicantService(
        IJobApplicantDao dao,
        IEmployerProfileDao employerProfileDao,
        IWhatsAppNotificationService whatsAppService,
        ILogger<JobApplicantService> logger)
    {
        _dao = dao;
        _employerProfileDao = employerProfileDao;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<JobDetailViewModel?> GetJobDetailAsync(int jobPostId, int employerUserAccountId)
    {
        var jobPost = await _dao.GetJobPostWithSkillsForEmployerAsync(jobPostId, employerUserAccountId);
        if (jobPost == null) return null;

        var applications = await _dao.GetApplicationsForJobAsync(jobPostId);
        var employeeIds = applications.Select(a => a.EmployeeUserAccountId).Distinct().ToList();

        var profiles = await _dao.GetEmployeeProfilesAsync(employeeIds);
        var skillsByEmployee = await _dao.GetEmployeeSkillNamesAsync(employeeIds);

        var applicantItems = new List<ApplicantListItemViewModel>();

        foreach (var a in applications)
        {
            profiles.TryGetValue(a.EmployeeUserAccountId, out var profile);
            skillsByEmployee.TryGetValue(a.EmployeeUserAccountId, out var skills);

            var activeJobStartOtp = await _dao.GetActiveOtpAsync(a.EmployeeUserAccountId, a.JobPostId, OtpType.JobStart);
            var activeSatisfactionOtp = await _dao.GetActiveOtpAsync(a.EmployeeUserAccountId, a.JobPostId, OtpType.Satisfaction);
            var rating = a.StatusCd == "COMPLETED" ? await _dao.GetRatingForApplicationAsync(a.JobApplicationId) : null;

            applicantItems.Add(new ApplicantListItemViewModel
            {
                JobApplicationId = a.JobApplicationId,
                EmployeeUserAccountId = a.EmployeeUserAccountId,
                Name = $"{a.EmployeeUserAccount.FirstName} {a.EmployeeUserAccount.LastName}".Trim(),
                SkillNames = skills ?? new List<string>(),
                Location = FormatLocation(profile?.AreaAddressTxt, profile?.CityName),
                DistanceKm = DistanceKm(jobPost.LatitudeNbr, jobPost.LongitudeNbr, profile?.LatitudeNbr, profile?.LongitudeNbr),
                StatusCd = a.StatusCd,
                AppliedTs = a.AppliedTs,
                RevealedContactNbr = a.EmployeeUserAccount.ContactNbr,
                ActiveJobStartOtpCd = activeJobStartOtp?.OtpCd,
                ActiveJobStartOtpExpiresTs = activeJobStartOtp?.ExpiresTs,
                ActiveSatisfactionOtpCd = activeSatisfactionOtp?.OtpCd,
                ActiveSatisfactionOtpExpiresTs = activeSatisfactionOtp?.ExpiresTs,
                RatingNbr = rating?.RatingNbr,
                CancelReasonCd = a.CancelReasonCd,
                CancelReasonTxt = a.CancelReasonTxt,
            });
        }

        applicantItems = applicantItems.OrderBy(a => a.DistanceKm ?? double.MaxValue).ToList();

        return new JobDetailViewModel
        {
            JobPostId = jobPost.JobPostId,
            JobTitle = jobPost.JobTitle,
            DescriptionTxt = jobPost.DescriptionTxt,
            RequiredWorkerNbr = jobPost.RequiredWorkerNbr,
            HourlyWageAmt = jobPost.HourlyWageAmt,
            StartDt = jobPost.StartDt,
            DurationHourNbr = jobPost.DurationHourNbr,
            StartTime = jobPost.StartTime,
            StartDateTime = jobPost.StartDateTime,
            EndDateTime = jobPost.EndDateTime,
            LocationAddressTxt = jobPost.LocationAddressTxt,
            ContactNbr = jobPost.ContactNbr,
            StatusCd = jobPost.StatusCd,
            RequiredSkillNames = jobPost.JobSkills.Select(js => js.Skill?.SkillName ?? string.Empty)
                                                    .Where(n => n.Length > 0).ToList(),
            Applicants = applicantItems,
            FilledWorkerNbr = applicantItems.Count(a => a.StatusCd != "CANCELLED"),
        };
    }

    public async Task<ApplicantProfileViewModel?> GetApplicantProfileAsync(int jobApplicationId, int employerUserAccountId)
    {
        var application = await _dao.GetApplicationForEmployerAsync(jobApplicationId, employerUserAccountId);
        if (application == null) return null;

        var profiles = await _dao.GetEmployeeProfilesAsync(new List<int> { application.EmployeeUserAccountId });
        var skillsByEmployee = await _dao.GetEmployeeSkillNamesAsync(new List<int> { application.EmployeeUserAccountId });

        profiles.TryGetValue(application.EmployeeUserAccountId, out var profile);
        skillsByEmployee.TryGetValue(application.EmployeeUserAccountId, out var skills);

        // Viewing the profile counts as the employer having looked at this applicant.
        await _dao.AdvanceStatusAsync(jobApplicationId, "EMPLOYER_VIEWED");

        return new ApplicantProfileViewModel
        {
            JobApplicationId = application.JobApplicationId,
            JobPostId = application.JobPostId,
            JobTitle = application.JobPost.JobTitle,
            EmployeeUserAccountId = application.EmployeeUserAccountId,
            Name = $"{application.EmployeeUserAccount.FirstName} {application.EmployeeUserAccount.LastName}".Trim(),
            SkillNames = skills ?? new List<string>(),
            Location = FormatLocation(profile?.AreaAddressTxt, profile?.CityName),
            ExperienceNoteTxt = null, // Not yet captured on EMPLOYEE_PROFILE — shown as "Not provided" in the view.
            StatusCd = application.StatusCd,
        };
    }

    public async Task<ContactRevealResult> ContactEmployeeAsync(int jobApplicationId, int employerUserAccountId)
    {
        var application = await _dao.GetApplicationForEmployerAsync(jobApplicationId, employerUserAccountId);
        if (application == null)
            return new ContactRevealResult(false, "Applicant not found.");

        // The employee has already "expressed interest" simply by applying —
        // that's what the JOB_APPLICATION row represents — so contact is allowed
        // for any existing application.
        await _dao.AdvanceStatusAsync(jobApplicationId, "EMPLOYER_CONTACTED");

        // Notify the employee over WhatsApp with the business name, job
        // timing, amount and location — and send the employer a confirmation
        // notification with their own contact/name, timing and job details.
        try
        {
            var employerProfile = await _employerProfileDao.GetProfileAsync(employerUserAccountId);
            var employerUser = employerProfile?.User ?? application.JobPost.EmployerUserAccount;

            if (employerUser != null)
            {
                await _whatsAppService.SendContactNotificationAsync(
                    application.JobPost,
                    application.EmployeeUserAccount,
                    employerUser,
                    employerProfile?.Profile?.CompanyName,
                    employerProfile?.Profile?.ContactPersonName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send Contact Employee WhatsApp notifications for JobApplicationId={Id}", jobApplicationId);
        }

        _logger.LogInformation("Employer {EmployerId} revealed contact for JobApplicationId={Id}",
            employerUserAccountId, jobApplicationId);

        return new ContactRevealResult(true, "Contact number revealed.", application.EmployeeUserAccount.ContactNbr);
    }

    public async Task<ServiceResult> GenerateOtpAsync(int jobApplicationId, int employerUserAccountId, string otpTypeCd, string? ipAddress)
    {
        if (otpTypeCd is not (OtpType.JobStart or OtpType.Satisfaction))
            return new ServiceResult(false, "Unknown OTP type.");

        var application = await _dao.GetApplicationForEmployerAsync(jobApplicationId, employerUserAccountId);
        if (application == null)
            return new ServiceResult(false, "Applicant not found.");

        if (otpTypeCd == OtpType.JobStart && application.StatusCd is "JOB_STARTED" or "COMPLETED")
            return new ServiceResult(false, "Job Starting OTP has already been used for this Kaarigar.");

        if (otpTypeCd == OtpType.Satisfaction && application.StatusCd is not "JOB_STARTED")
            return new ServiceResult(false, "The Satisfaction OTP can only be generated after the Kaarigar has started the job.");

        // Only one live OTP per type, per employee+job, at a time.
        await _dao.InvalidatePreviousOtpsAsync(application.EmployeeUserAccountId, application.JobPostId, otpTypeCd);

        var otpCd = GenerateOtpCode();
        var otp = new OtpRecord
        {
            EmployeeUserAccountId = application.EmployeeUserAccountId,
            EmployerUserAccountId = employerUserAccountId,
            JobPostId = application.JobPostId,
            OtpTypeCd = otpTypeCd,
            OtpCd = otpCd,
            GeneratedTs = DateTime.UtcNow,
            ExpiresTs = DateTime.UtcNow.AddMinutes(15),
            CreatedBy = "EMPLOYER_GENERATE_OTP",
            CreatedIpAddr = ipAddress,
        };

        await _dao.CreateOtpRecordAsync(otp);

        _logger.LogInformation(
            "Employer {EmployerId} generated {OtpType} OTP for JobApplicationId={Id}",
            employerUserAccountId, otpTypeCd, jobApplicationId);

        var label = otpTypeCd == OtpType.JobStart ? "Job Starting" : "Satisfaction";
        return new ServiceResult(true, $"{label} OTP generated. Valid for 15 minutes — share it only with this Kaarigar in person.");
    }

    private static string GenerateOtpCode() => Random.Shared.Next(0, 1_000_000).ToString("D6");

    public async Task<ServiceResult> SubmitRatingAsync(int jobApplicationId, int employerUserAccountId, byte ratingNbr, string? reviewTxt)
    {
        if (ratingNbr is < 1 or > 5)
            return new ServiceResult(false, "Rating must be between 1 and 5.");

        var application = await _dao.GetApplicationForEmployerAsync(jobApplicationId, employerUserAccountId);
        if (application == null) return new ServiceResult(false, "Applicant not found.");

        if (application.StatusCd != "COMPLETED")
            return new ServiceResult(false, "You can rate this Kaarigar only after the job is marked completed.");

        if (await _dao.GetRatingForApplicationAsync(jobApplicationId) != null)
            return new ServiceResult(false, "You've already rated this Kaarigar for this job.");

        await _dao.CreateRatingAsync(new KaarigarRating
        {
            JobApplicationId = jobApplicationId,
            JobPostId = application.JobPostId,
            EmployeeUserAccountId = application.EmployeeUserAccountId,
            EmployerUserAccountId = employerUserAccountId,
            RatingNbr = ratingNbr,
            ReviewTxt = reviewTxt,
            RatedTs = DateTime.UtcNow,
        });

        return new ServiceResult(true, "Thanks — your rating has been submitted.");
    }

    public async Task<KaarigarRating?> GetRatingForApplicationAsync(int jobApplicationId, int employerUserAccountId)
    {
        var application = await _dao.GetApplicationForEmployerAsync(jobApplicationId, employerUserAccountId);
        if (application == null) return null;
        return await _dao.GetRatingForApplicationAsync(jobApplicationId);
    }

    public async Task<ServiceResult> CancelApplicantAsync(int jobApplicationId, int employerUserAccountId, string cancelReasonCd, string? cancelReasonTxt)
    {
        if (!CancelReasonOptions.All.ContainsKey(cancelReasonCd))
            return new ServiceResult(false, "Please select a valid cancellation reason.");

        var application = await _dao.GetApplicationForEmployerAsync(jobApplicationId, employerUserAccountId);
        if (application == null) return new ServiceResult(false, "Applicant not found.");

        if (application.StatusCd is not ("PENDING" or "EMPLOYER_VIEWED" or "EMPLOYER_CONTACTED"))
            return new ServiceResult(false, "This Kaarigar can no longer be cancelled — the job has already started (or is completed/cancelled).");

        await _dao.CancelApplicationAsync(jobApplicationId, cancelReasonCd, cancelReasonTxt?.Trim());

        try
        {
            await _whatsAppService.SendCancellationNotificationAsync(
                application.JobPost, application.EmployeeUserAccount,
                CancelReasonOptions.Label(cancelReasonCd), cancelReasonTxt?.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send cancellation WhatsApp notification for JobApplicationId={Id}", jobApplicationId);
        }

        return new ServiceResult(true, "Kaarigar has been cancelled for this job. Both parties have been notified.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? FormatLocation(string? area, string? city)
    {
        if (string.IsNullOrWhiteSpace(area) && string.IsNullOrWhiteSpace(city)) return null;
        if (string.IsNullOrWhiteSpace(area)) return city;
        if (string.IsNullOrWhiteSpace(city)) return area;
        return $"{area}, {city}";
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
