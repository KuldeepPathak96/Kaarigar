using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class EmployeeJobDao : IEmployeeJobDao
{
    private readonly AppDbContext _db;

    public EmployeeJobDao(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> IsEmployeeApprovedAsync(int employeeUserAccountId) =>
        _db.UserAccounts.AsNoTracking()
           .Where(u => u.UserAccountId == employeeUserAccountId)
           .Select(u => u.IsApprovedFl)
           .FirstOrDefaultAsync();

    public Task<EmployeeProfile?> GetEmployeeProfileAsync(int employeeUserAccountId) =>
        _db.EmployeeProfiles.AsNoTracking()
           .FirstOrDefaultAsync(p => p.UserAccountId == employeeUserAccountId);

    public Task<List<int>> GetEmployeeSkillIdsAsync(int employeeUserAccountId) =>
        _db.EmployeeSkills.AsNoTracking()
           .Where(s => s.UserAccountId == employeeUserAccountId)
           .Select(s => s.SkillId)
           .ToListAsync();

    public Task<List<JobPost>> GetActiveJobPostsAsync() =>
        _db.JobPosts.AsNoTracking()
           .Where(j => j.StatusCd == "ACTIVE")
           .Include(j => j.JobSkills).ThenInclude(js => js.Skill)
           .OrderByDescending(j => j.CreatedTs)
           .ToListAsync();

    public async Task<Dictionary<int, string?>> GetEmployerBusinessCategoryNamesAsync(IEnumerable<int> employerUserAccountIds)
    {
        var ids = employerUserAccountIds.Distinct().ToList();

        return await _db.EmployerProfiles.AsNoTracking()
            .Where(p => ids.Contains(p.UserAccountId))
            .Include(p => p.BusinessCategory)
            .ToDictionaryAsync(p => p.UserAccountId, p => p.BusinessCategory?.CategoryName);
    }

    public async Task<HashSet<int>> GetAppliedJobPostIdsAsync(int employeeUserAccountId)
    {
        var ids = await _db.JobApplications.AsNoTracking()
            .Where(a => a.EmployeeUserAccountId == employeeUserAccountId)
            .Select(a => a.JobPostId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public Task<List<(int SkillId, string SkillName)>> GetActiveSkillOptionsAsync() =>
        _db.Skills.AsNoTracking()
           .Where(s => s.IsActiveFl)
           .OrderBy(s => s.SkillName)
           .Select(s => new ValueTuple<int, string>(s.SkillId, s.SkillName))
           .ToListAsync();

    public Task<JobPost?> GetActiveJobPostByIdAsync(int jobPostId) =>
        _db.JobPosts.AsNoTracking()
           .FirstOrDefaultAsync(j => j.JobPostId == jobPostId && j.StatusCd == "ACTIVE");

    public Task<bool> HasAppliedAsync(int employeeUserAccountId, int jobPostId) =>
        _db.JobApplications.AsNoTracking()
           .AnyAsync(a => a.EmployeeUserAccountId == employeeUserAccountId && a.JobPostId == jobPostId);

    public async Task InsertApplicationAsync(int employeeUserAccountId, int jobPostId, string? ipAddress)
    {
        var application = new JobApplication
        {
            JobPostId = jobPostId,
            EmployeeUserAccountId = employeeUserAccountId,
            StatusCd = "PENDING",
            AppliedTs = DateTime.UtcNow,
            CreatedBy = "EMPLOYEE",
            CreatedIpAddr = ipAddress,
        };

        _db.JobApplications.Add(application);
        await _db.SaveChangesAsync();
    }

    // ── W-05: MY APPLICATIONS ────────────────────────────────────────────────

    public Task<List<JobApplication>> GetApplicationsForEmployeeAsync(int employeeUserAccountId) =>
        _db.JobApplications.AsNoTracking()
           .Include(a => a.JobPost)
           .Where(a => a.EmployeeUserAccountId == employeeUserAccountId)
           .OrderByDescending(a => a.AppliedTs)
           .ToListAsync();

    public Task<JobApplication?> GetApplicationForEmployeeAsync(int jobApplicationId, int employeeUserAccountId) =>
        _db.JobApplications
           .Include(a => a.JobPost)
           .Include(a => a.EmployeeUserAccount)
           .FirstOrDefaultAsync(a => a.JobApplicationId == jobApplicationId &&
                                     a.EmployeeUserAccountId == employeeUserAccountId);

    // ── W-06: ENTER OTP (employer-generated, Kaarigar-entered) ───────────────

    // Forward-only status progression for JOB_APPLICATION.STATUS_CD (mirrors JobApplicantDao.StatusOrder).
    private static readonly List<string> StatusOrder = new()
    {
        "PENDING", "EMPLOYER_VIEWED", "EMPLOYER_CONTACTED", "JOB_STARTED", "COMPLETED",
    };

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

    public async Task MarkOtpUsedAsync(int otpRecordId)
    {
        var otp = await _db.OtpRecords.FirstOrDefaultAsync(o => o.OtpRecordId == otpRecordId);
        if (otp == null) return;

        otp.IsUsedFl = true;
        otp.UpdatedBy = "EMPLOYEE_ENTER_OTP";
        otp.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task AdvanceStatusAsync(int jobApplicationId, string newStatusCd)
    {
        var application = await _db.JobApplications.FirstOrDefaultAsync(a => a.JobApplicationId == jobApplicationId);
        if (application == null) return;

        var currentIndex = StatusOrder.IndexOf(application.StatusCd);
        var newIndex = StatusOrder.IndexOf(newStatusCd);
        if (newIndex <= currentIndex) return; // never move backwards

        application.StatusCd = newStatusCd;
        application.UpdatedBy = "EMPLOYEE_ENTER_OTP";
        application.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
