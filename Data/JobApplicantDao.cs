using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class JobApplicantDao : IJobApplicantDao
{
    private readonly AppDbContext _db;

    // Forward-only status progression for JOB_APPLICATION.STATUS_CD.
    private static readonly List<string> StatusOrder = new()
    {
        "PENDING", "EMPLOYER_VIEWED", "EMPLOYER_CONTACTED", "JOB_STARTED", "COMPLETED",
    };

    public JobApplicantDao(AppDbContext db)
    {
        _db = db;
    }

    public Task<JobPost?> GetJobPostWithSkillsForEmployerAsync(int jobPostId, int employerUserAccountId) =>
        _db.JobPosts
           .AsNoTracking()
           .Include(jp => jp.JobSkills).ThenInclude(js => js.Skill)
           .FirstOrDefaultAsync(jp => jp.JobPostId == jobPostId && jp.EmployerUserAccountId == employerUserAccountId);

    public Task<List<JobApplication>> GetApplicationsForJobAsync(int jobPostId) =>
        _db.JobApplications
           .AsNoTracking()
           .Include(a => a.EmployeeUserAccount)
           .Where(a => a.JobPostId == jobPostId)
           .OrderByDescending(a => a.AppliedTs)
           .ToListAsync();

    public Task<JobApplication?> GetApplicationForEmployerAsync(int jobApplicationId, int employerUserAccountId) =>
        _db.JobApplications
           .AsNoTracking()
           .Include(a => a.EmployeeUserAccount)
           .Include(a => a.JobPost).ThenInclude(jp => jp.EmployerUserAccount)
           .FirstOrDefaultAsync(a => a.JobApplicationId == jobApplicationId &&
                                     a.JobPost.EmployerUserAccountId == employerUserAccountId);

    public async Task<Dictionary<int, EmployeeProfile>> GetEmployeeProfilesAsync(List<int> employeeUserAccountIds)
    {
        if (employeeUserAccountIds.Count == 0) return new Dictionary<int, EmployeeProfile>();

        return await _db.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => employeeUserAccountIds.Contains(ep.UserAccountId))
            .ToDictionaryAsync(ep => ep.UserAccountId);
    }

    public async Task<Dictionary<int, List<string>>> GetEmployeeSkillNamesAsync(List<int> employeeUserAccountIds)
    {
        if (employeeUserAccountIds.Count == 0) return new Dictionary<int, List<string>>();

        var rows = await _db.EmployeeSkills
            .AsNoTracking()
            .Where(es => employeeUserAccountIds.Contains(es.UserAccountId))
            .Join(_db.Skills.AsNoTracking(), es => es.SkillId, s => s.SkillId, (es, s) => new { es.UserAccountId, s.SkillName })
            .ToListAsync();

        return rows
            .GroupBy(r => r.UserAccountId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.SkillName).ToList());
    }

    public async Task AdvanceStatusAsync(int jobApplicationId, string newStatusCd)
    {
        var application = await _db.JobApplications.FirstOrDefaultAsync(a => a.JobApplicationId == jobApplicationId);
        if (application == null) return;

        var currentIndex = StatusOrder.IndexOf(application.StatusCd);
        var newIndex = StatusOrder.IndexOf(newStatusCd);
        if (newIndex <= currentIndex) return; // never move backwards

        application.StatusCd = newStatusCd;
        application.UpdatedBy = "JOB_APPLICANT_SCREEN";
        application.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public Task<OtpRecord?> GetValidOtpAsync(int employeeUserAccountId, int jobPostId, string otpTypeCd, string otpCd) =>
        _db.OtpRecords
           .Where(o => o.EmployeeUserAccountId == employeeUserAccountId &&
                       o.JobPostId == jobPostId &&
                       o.OtpTypeCd == otpTypeCd &&
                       o.OtpCd == otpCd &&
                       !o.IsUsedFl &&
                       o.ExpiresTs > DateTime.UtcNow)
           .OrderByDescending(o => o.GeneratedTs)
           .FirstOrDefaultAsync();

    public Task<OtpRecord?> GetActiveOtpAsync(int employeeUserAccountId, int jobPostId, string otpTypeCd) =>
        _db.OtpRecords
           .AsNoTracking()
           .Where(o => o.EmployeeUserAccountId == employeeUserAccountId &&
                       o.JobPostId == jobPostId &&
                       o.OtpTypeCd == otpTypeCd &&
                       !o.IsUsedFl &&
                       o.ExpiresTs > DateTime.UtcNow)
           .OrderByDescending(o => o.GeneratedTs)
           .FirstOrDefaultAsync();

    public async Task MarkOtpUsedAsync(int otpRecordId)
    {
        var otp = await _db.OtpRecords.FirstOrDefaultAsync(o => o.OtpRecordId == otpRecordId);
        if (otp == null) return;

        otp.IsUsedFl = true;
        otp.UpdatedBy = "JOB_APPLICANT_SCREEN";
        otp.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task InvalidatePreviousOtpsAsync(int employeeUserAccountId, int jobPostId, string otpTypeCd)
    {
        var previous = await _db.OtpRecords
            .Where(o => o.EmployeeUserAccountId == employeeUserAccountId &&
                        o.JobPostId == jobPostId &&
                        o.OtpTypeCd == otpTypeCd &&
                        !o.IsUsedFl)
            .ToListAsync();

        foreach (var otp in previous)
        {
            otp.IsUsedFl = true;
            otp.UpdatedBy = "EMPLOYER_GENERATE_OTP";
            otp.UpdatedTs = DateTime.UtcNow;
        }

        if (previous.Count > 0)
            await _db.SaveChangesAsync();
    }

    public async Task<OtpRecord> CreateOtpRecordAsync(OtpRecord otp)
    {
        _db.OtpRecords.Add(otp);
        await _db.SaveChangesAsync();
        return otp;
    }

    public Task<KaarigarRating?> GetRatingForApplicationAsync(int jobApplicationId) =>
        _db.KaarigarRatings.AsNoTracking().FirstOrDefaultAsync(r => r.JobApplicationId == jobApplicationId);

    public async Task<KaarigarRating> CreateRatingAsync(KaarigarRating rating)
    {
        _db.KaarigarRatings.Add(rating);
        await _db.SaveChangesAsync();
        return rating;
    }

    public async Task CancelApplicationAsync(int jobApplicationId, string cancelReasonCd, string? cancelReasonTxt)
    {
        var application = await _db.JobApplications.FirstOrDefaultAsync(a => a.JobApplicationId == jobApplicationId);
        if (application == null) return;

        application.StatusCd = "CANCELLED";
        application.CancelReasonCd = cancelReasonCd;
        application.CancelReasonTxt = cancelReasonTxt;
        application.CancelledTs = DateTime.UtcNow;
        application.UpdatedBy = "EMPLOYER_CANCEL_APPLICANT";
        application.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // A slot just freed up — reopen the job if it had been auto-closed for being full.
        var jobPost = await _db.JobPosts.FirstOrDefaultAsync(jp => jp.JobPostId == application.JobPostId);
        if (jobPost != null && jobPost.StatusCd == "CLOSED")
        {
            var activeApplicantCount = await _db.JobApplications
                .CountAsync(a => a.JobPostId == jobPost.JobPostId && a.StatusCd != "CANCELLED");

            if (activeApplicantCount < jobPost.RequiredWorkerNbr)
            {
                jobPost.StatusCd = "ACTIVE";
                jobPost.UpdatedBy = "JOB_POST_AUTO_REOPEN";
                jobPost.UpdatedTs = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }
}
