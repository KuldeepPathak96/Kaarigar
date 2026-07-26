using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class AdminJobPostDao : IAdminJobPostDao
{
    private readonly AppDbContext _db;

    public AdminJobPostDao(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AdminJobPostListItemViewModel>> SearchJobPostsAsync(
        string? search, string? city, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.JobPosts
            .AsNoTracking()
            .Join(_db.UserAccounts.AsNoTracking(), jp => jp.EmployerUserAccountId, u => u.UserAccountId,
                  (jp, u) => new { JobPost = jp, Employer = u })
            .Join(_db.EmployerProfiles.AsNoTracking(), x => x.Employer.UserAccountId, p => p.UserAccountId,
                  (x, p) => new { x.JobPost, x.Employer, Profile = p });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.JobPost.JobTitle.ToLower().Contains(term) ||
                (x.Employer.FirstName + " " + x.Employer.LastName).ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(x => x.Profile.CityName == city);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.JobPost.StatusCd == status);

        if (fromDate.HasValue)
            query = query.Where(x => x.JobPost.CreatedTs >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(x => x.JobPost.CreatedTs < toDate.Value.Date.AddDays(1));

        var rows = await query.OrderByDescending(x => x.JobPost.CreatedTs).ToListAsync();

        var jobPostIds = rows.Select(r => r.JobPost.JobPostId).ToList();
        var applicantCounts = await _db.JobApplications
            .AsNoTracking()
            .Where(a => jobPostIds.Contains(a.JobPostId))
            .GroupBy(a => a.JobPostId)
            .Select(g => new { JobPostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobPostId, x => x.Count);

        return rows.Select(x => new AdminJobPostListItemViewModel
        {
            JobPostId = x.JobPost.JobPostId,
            JobTitle = x.JobPost.JobTitle,
            EmployerName = $"{x.Employer.FirstName} {x.Employer.LastName}".Trim(),
            EmployerUserAccountId = x.Employer.UserAccountId,
            CityName = x.Profile.CityName,
            StatusCd = x.JobPost.StatusCd,
            DatePosted = x.JobPost.CreatedTs,
            ApplicantsCount = applicantCounts.TryGetValue(x.JobPost.JobPostId, out var c) ? c : 0,
        }).ToList();
    }

    public Task<List<string>> GetDistinctCitiesAsync() =>
        _db.EmployerProfiles
           .AsNoTracking()
           .Where(p => p.CityName != null && p.CityName != "")
           .Select(p => p.CityName!)
           .Distinct()
           .OrderBy(c => c)
           .ToListAsync();

    public async Task<AdminJobPostDetailViewModel?> GetJobPostDetailAsync(int jobPostId)
    {
        var row = await _db.JobPosts
            .AsNoTracking()
            .Include(jp => jp.JobSkills).ThenInclude(js => js.Skill)
            .Where(jp => jp.JobPostId == jobPostId)
            .Join(_db.UserAccounts.AsNoTracking(), jp => jp.EmployerUserAccountId, u => u.UserAccountId,
                  (jp, u) => new { JobPost = jp, Employer = u })
            .FirstOrDefaultAsync();

        if (row == null) return null;

        var employerProfile = await _db.EmployerProfiles
            .AsNoTracking()
            .Where(p => p.UserAccountId == row.Employer.UserAccountId)
            .Select(p => new { p.CityName })
            .FirstOrDefaultAsync();

        var applications = await _db.JobApplications
            .AsNoTracking()
            .Include(a => a.EmployeeUserAccount)
            .Where(a => a.JobPostId == jobPostId)
            .OrderByDescending(a => a.AppliedTs)
            .ToListAsync();

        return new AdminJobPostDetailViewModel
        {
            JobPostId = row.JobPost.JobPostId,
            JobTitle = row.JobPost.JobTitle,
            DescriptionTxt = row.JobPost.DescriptionTxt,
            RequiredWorkerNbr = row.JobPost.RequiredWorkerNbr,
            HourlyWageAmt = row.JobPost.HourlyWageAmt,
            StartDt = row.JobPost.StartDt,
            DurationHourNbr = row.JobPost.DurationHourNbr,
            LocationAddressTxt = row.JobPost.LocationAddressTxt,
            ContactNbr = row.JobPost.ContactNbr,
            StatusCd = row.JobPost.StatusCd,
            DatePosted = row.JobPost.CreatedTs,
            RequiredSkillNames = row.JobPost.JobSkills.Select(js => js.Skill?.SkillName ?? string.Empty)
                                                        .Where(n => n.Length > 0).ToList(),
            EmployerUserAccountId = row.Employer.UserAccountId,
            EmployerName = $"{row.Employer.FirstName} {row.Employer.LastName}".Trim(),
            EmployerContactNbr = row.Employer.ContactNbr,
            EmployerCityName = employerProfile?.CityName,
            Applicants = applications.Select(a => new AdminApplicantSummaryViewModel
            {
                Name = $"{a.EmployeeUserAccount.FirstName} {a.EmployeeUserAccount.LastName}".Trim(),
                StatusCd = a.StatusCd,
                AppliedTs = a.AppliedTs,
            }).ToList(),
        };
    }

    public async Task CloseJobPostAsync(int jobPostId)
    {
        var jobPost = await _db.JobPosts.FirstOrDefaultAsync(jp => jp.JobPostId == jobPostId);
        if (jobPost == null) return;

        jobPost.StatusCd = "CLOSED";
        jobPost.UpdatedBy = "ADMIN_MANAGE_JOBS";
        jobPost.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public Task<int> GetApplicantsCountAsync(int jobPostId) =>
        _db.JobApplications.AsNoTracking().CountAsync(a => a.JobPostId == jobPostId);

    public async Task DeleteJobPostAsync(int jobPostId)
    {
        var jobPost = await _db.JobPosts.FirstOrDefaultAsync(jp => jp.JobPostId == jobPostId);
        if (jobPost == null) return;

        // JOB_SKILL has ON DELETE CASCADE from JOB_POST, so removing the
        // JobPost is enough — EF/SQL Server will cascade the skill rows.
        _db.JobPosts.Remove(jobPost);
        await _db.SaveChangesAsync();
    }
}
