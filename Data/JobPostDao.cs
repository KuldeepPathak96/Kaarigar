using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class JobPostDao : IJobPostDao
{
    private readonly AppDbContext _db;

    public JobPostDao(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Skill>> GetActiveSkillsAsync() =>
        _db.Skills.AsNoTracking().Where(s => s.IsActiveFl).OrderBy(s => s.SkillName).ToListAsync();

    public async Task<JobPost> CreateJobPostAsync(JobPost jobPost, List<int> skillIds)
    {
        _db.JobPosts.Add(jobPost);
        await _db.SaveChangesAsync(); // need JobPostId before inserting JobSkill rows

        foreach (var skillId in skillIds.Distinct())
        {
            _db.JobSkills.Add(new JobSkill
            {
                JobPostId = jobPost.JobPostId,
                SkillId = skillId,
                CreatedBy = jobPost.CreatedBy,
                CreatedTs = DateTime.UtcNow,
            });
        }

        if (skillIds.Count > 0)
            await _db.SaveChangesAsync();

        return jobPost;
    }

    public Task<JobPost?> GetJobPostForEmployerAsync(int jobPostId, int employerUserAccountId) =>
        _db.JobPosts
           .Include(jp => jp.JobSkills)
           .FirstOrDefaultAsync(jp => jp.JobPostId == jobPostId && jp.EmployerUserAccountId == employerUserAccountId);

    public Task<JobPost?> GetJobPostByIdAsync(int jobPostId) =>
        _db.JobPosts.AsNoTracking().FirstOrDefaultAsync(jp => jp.JobPostId == jobPostId);

    public async Task UpdateJobPostAsync(JobPost jobPost, List<int> skillIds)
    {
        var existing = await _db.JobPosts
            .Include(jp => jp.JobSkills)
            .FirstOrDefaultAsync(jp => jp.JobPostId == jobPost.JobPostId &&
                                       jp.EmployerUserAccountId == jobPost.EmployerUserAccountId);
        if (existing == null) return;

        existing.JobTitle = jobPost.JobTitle;
        existing.DescriptionTxt = jobPost.DescriptionTxt;
        existing.RequiredWorkerNbr = jobPost.RequiredWorkerNbr;
        existing.HourlyWageAmt = jobPost.HourlyWageAmt;
        existing.StartDt = jobPost.StartDt;
        existing.StartTime = jobPost.StartTime;
        existing.DurationHourNbr = jobPost.DurationHourNbr;
        existing.LocationAddressTxt = jobPost.LocationAddressTxt;
        existing.LatitudeNbr = jobPost.LatitudeNbr;
        existing.LongitudeNbr = jobPost.LongitudeNbr;
        existing.ContactNbr = jobPost.ContactNbr;
        existing.UpdatedBy = "JOB_POST_EDIT";
        existing.UpdatedTs = DateTime.UtcNow;

        // Replace skill rows wholesale — simplest correct approach for a
        // small multi-select list; avoids diffing add/remove sets.
        _db.JobSkills.RemoveRange(existing.JobSkills);
        foreach (var skillId in skillIds.Distinct())
        {
            _db.JobSkills.Add(new JobSkill
            {
                JobPostId = existing.JobPostId,
                SkillId = skillId,
                CreatedBy = "JOB_POST_EDIT",
                CreatedTs = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int jobPostId, int employerUserAccountId, string statusCd)
    {
        var jobPost = await _db.JobPosts
            .FirstOrDefaultAsync(jp => jp.JobPostId == jobPostId && jp.EmployerUserAccountId == employerUserAccountId);
        if (jobPost == null) return;

        jobPost.StatusCd = statusCd;
        jobPost.UpdatedBy = "JOB_POST_STATUS_CHANGE";
        jobPost.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<JobPostListItemViewModel>> GetMyJobPostsAsync(int employerUserAccountId)
    {
        var jobPosts = await _db.JobPosts
            .AsNoTracking()
            .Include(jp => jp.JobSkills).ThenInclude(js => js.Skill)
            .Where(jp => jp.EmployerUserAccountId == employerUserAccountId)
            .OrderByDescending(jp => jp.CreatedTs)
            .ToListAsync();

        var jobPostIds = jobPosts.Select(jp => jp.JobPostId).ToList();

        var applicationCounts = await _db.JobApplications
            .AsNoTracking()
            .Where(a => jobPostIds.Contains(a.JobPostId))
            .GroupBy(a => a.JobPostId)
            .Select(g => new { JobPostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobPostId, x => x.Count);

        var filledCounts = await _db.JobApplications
            .AsNoTracking()
            .Where(a => jobPostIds.Contains(a.JobPostId) && a.StatusCd != "CANCELLED")
            .GroupBy(a => a.JobPostId)
            .Select(g => new { JobPostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobPostId, x => x.Count);

        return jobPosts.Select(jp => new JobPostListItemViewModel
        {
            JobPostId = jp.JobPostId,
            JobTitle = jp.JobTitle,
            RequiredSkillNames = jp.JobSkills.Select(js => js.Skill?.SkillName ?? string.Empty)
                                              .Where(n => n.Length > 0).ToList(),
            DatePosted = jp.CreatedTs,
            ApplicationsCount = applicationCounts.TryGetValue(jp.JobPostId, out var count) ? count : 0,
            RequiredWorkerNbr = jp.RequiredWorkerNbr,
            FilledWorkerNbr = filledCounts.TryGetValue(jp.JobPostId, out var filled) ? filled : 0,
            StatusCd = jp.StatusCd,
        }).ToList();
    }

    public async Task<List<UserAccount>> FindMatchingEmployeesAsync(JobPost jobPost)
    {
        var requiredSkillIds = await _db.JobSkills
            .Where(js => js.JobPostId == jobPost.JobPostId)
            .Select(js => js.SkillId)
            .ToListAsync();

        // Only consider employees who could actually act on the alert:
        // Admin-approved, active accounts, who haven't opted out (Screen W-02).
        // Notifying pending/deactivated/opted-out employees is just noise —
        // ExpressInterestAsync would block them anyway.
        var candidates = await _db.EmployeeProfiles
            .AsNoTracking()
            .Include(ep => ep.UserAccount)
            .Where(ep => ep.UserAccount != null &&
                         ep.UserAccount.IsActiveFl &&
                         ep.UserAccount.IsApprovedFl &&
                         ep.IsNotificationEnabledFl)
            .ToListAsync();

        // Skill filter — a job with no required skills listed is treated as
        // open to everyone (same rule as Screen W-03 Browse).
        if (requiredSkillIds.Count > 0)
        {
            var matchingUserIds = await _db.EmployeeSkills
                .Where(es => requiredSkillIds.Contains(es.SkillId) &&
                             candidates.Select(c => c.UserAccountId).Contains(es.UserAccountId))
                .Select(es => es.UserAccountId)
                .Distinct()
                .ToListAsync();

            candidates = candidates.Where(c => matchingUserIds.Contains(c.UserAccountId)).ToList();
        }

        // Location filter — real Haversine distance against each employee's
        // own saved PreferredRadiusNbr (5/10/25/50 km, or null = "Any"),
        // same as the live Browse/W-03 matching. Falls back to the old
        // city-name substring match only when either side is missing
        // coordinates (e.g. employer typed the address without using
        // "Use My Current Location"), so jobs still reach people even
        // without a precise pin.
        var matched = candidates.Where(c => IsLocationMatch(jobPost, c)).ToList();

        return matched.Select(c => c.UserAccount!).Where(u => u != null).ToList();
    }

    private static bool IsLocationMatch(JobPost jobPost, EmployeeProfile employeeProfile)
    {
        var hasJobCoords = jobPost.LatitudeNbr != null && jobPost.LongitudeNbr != null;
        var hasEmployeeCoords = employeeProfile.LatitudeNbr != null && employeeProfile.LongitudeNbr != null;

        if (hasJobCoords && hasEmployeeCoords)
        {
            if (employeeProfile.PreferredRadiusNbr == null)
                return true; // "Any distance"

            var distanceKm = DistanceKm(
                jobPost.LatitudeNbr, jobPost.LongitudeNbr,
                employeeProfile.LatitudeNbr, employeeProfile.LongitudeNbr);

            return distanceKm.HasValue && distanceKm.Value <= employeeProfile.PreferredRadiusNbr.Value;
        }

        // Coordinate-less fallback: best-effort city-name substring match.
        return !string.IsNullOrWhiteSpace(jobPost.LocationAddressTxt) &&
               !string.IsNullOrWhiteSpace(employeeProfile.CityName) &&
               jobPost.LocationAddressTxt.Contains(employeeProfile.CityName, StringComparison.OrdinalIgnoreCase);
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
