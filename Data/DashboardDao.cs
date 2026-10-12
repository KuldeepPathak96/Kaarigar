using Kaarigar.Data;
using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

/// <summary>
/// EF Core implementation of IDashboardDao.
/// All queries are read-only (AsNoTracking) for performance.
///
/// Column/status mapping notes (see 01_Database_Schema.sql):
///  - JOB_POST's employer FK column is EMPLOYER_USER_ACCOUNT_ID, not
///    USER_ACCOUNT_ID.
///  - JOB_APPLICATION has no employer FK column at all — the employer is
///    only reachable via JobPost.EmployerUserAccountId, so employer-scoped
///    application queries join through JobPost.
///  - JOB_APPLICATION.STATUS_CD is constrained to: PENDING, EMPLOYER_VIEWED,
///    EMPLOYER_CONTACTED, JOB_STARTED, COMPLETED.
///  - NOTIFICATION_LOG has no read/unread flag — only a SENT/FAILED delivery
///    STATUS_CD. "Unread" is therefore approximated as STATUS_CD == "SENT"
///    (i.e. successfully delivered) since a true read/unread flag does not
///    exist in the schema. Add an IS_READ_FL column if real read-tracking
///    is required.
///  - The OTP table is OTP_RECORD (job-site verification OTP), not OTP_LOG.
/// </summary>
public class DashboardDao : IDashboardDao
{
    private readonly AppDbContext _db;

    public DashboardDao(AppDbContext db)
    {
        _db = db;
    }

    // ── EMPLOYER ──────────────────────────────────────────────────────────────

    public Task<int> GetTotalJobsPostedAsync(int userAccountId) =>
        _db.JobPosts
           .AsNoTracking()
           .CountAsync(j => j.EmployerUserAccountId == userAccountId);

    public Task<int> GetActiveJobsCountAsync(int userAccountId) =>
        _db.JobPosts
           .AsNoTracking()
           .CountAsync(j => j.EmployerUserAccountId == userAccountId && j.StatusCd == "ACTIVE");

    public Task<int> GetTotalApplicationsAsync(int userAccountId) =>
        _db.JobApplications
           .AsNoTracking()
           .CountAsync(a => a.JobPost.EmployerUserAccountId == userAccountId);

    public Task<int> GetJobsFilledAsync(int userAccountId) =>
        _db.JobPosts
           .AsNoTracking()
           .CountAsync(j => j.EmployerUserAccountId == userAccountId && j.StatusCd == "CLOSED");

    // "Confirmed" = the employer has taken a hiring action on this application
    // (contacted, started, or completed) — there is no dedicated CONFIRMED
    // status in JOB_APPLICATION.STATUS_CD, so this set is the closest
    // equivalent to "worker is locked in for the shift".
    private static readonly string[] ConfirmedStatuses = { "EMPLOYER_CONTACTED", "JOB_STARTED", "COMPLETED" };

    public async Task<List<LiveShiftItem>> GetLiveShiftsAsync(int userAccountId)
    {
        var confirmedCounts = await _db.JobApplications
            .AsNoTracking()
            .Where(a => a.JobPost.EmployerUserAccountId == userAccountId &&
                        a.JobPost.StatusCd == "ACTIVE" &&
                        ConfirmedStatuses.Contains(a.StatusCd))
            .GroupBy(a => a.JobPostId)
            .Select(g => new { JobPostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobPostId, x => x.Count);

        var jobs = await _db.JobPosts
            .AsNoTracking()
            .Where(j => j.EmployerUserAccountId == userAccountId && j.StatusCd == "ACTIVE")
            .Select(j => new { j.JobPostId, j.JobTitle, j.RequiredWorkerNbr, j.StartDt, j.StartTime })
            .ToListAsync();

        return jobs
            .Select(j => new LiveShiftItem
            {
                JobPostId = j.JobPostId,
                JobTitle = j.JobTitle,
                RequiredWorkerNbr = j.RequiredWorkerNbr,
                ConfirmedWorkerNbr = confirmedCounts.TryGetValue(j.JobPostId, out var c) ? c : 0,
                StartDateTime = j.StartDt.HasValue ? j.StartDt.Value.Date + (j.StartTime ?? TimeSpan.Zero) : null,
            })
            .OrderBy(s => s.StartDateTime ?? DateTime.MaxValue)
            .ToList();
    }

    public async Task<List<ApplicationDetailItem>> GetRecentApplicationsAsync(int userAccountId, int take = 10)
    {
        var rows = await _db.JobApplications
            .AsNoTracking()
            .Where(a => a.JobPost.EmployerUserAccountId == userAccountId)
            .OrderByDescending(a => a.AppliedTs)
            .Take(take)
            .Select(a => new
            {
                a.JobApplicationId,
                a.StatusCd,
                a.AppliedTs,
                EmployeeFirstName = a.EmployeeUserAccount.FirstName,
                EmployeeLastName = a.EmployeeUserAccount.LastName,
                JobTitle = a.JobPost.JobTitle,
            })
            .ToListAsync();

        return rows.Select(r => new ApplicationDetailItem
        {
            JobApplicationId = r.JobApplicationId,
            EmployeeName = $"{r.EmployeeFirstName} {r.EmployeeLastName}".Trim(),
            JobTitle = r.JobTitle,
            StatusCd = r.StatusCd,
            AppliedTs = r.AppliedTs,
        }).ToList();
    }

    public async Task<List<FilledPositionItem>> GetRecentlyFilledAsync(int userAccountId, int take = 10)
    {
        var confirmedCounts = await _db.JobApplications
            .AsNoTracking()
            .Where(a => a.JobPost.EmployerUserAccountId == userAccountId &&
                        a.JobPost.StatusCd == "CLOSED" &&
                        ConfirmedStatuses.Contains(a.StatusCd))
            .GroupBy(a => a.JobPostId)
            .Select(g => new { JobPostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobPostId, x => x.Count);

        var jobs = await _db.JobPosts
            .AsNoTracking()
            .Where(j => j.EmployerUserAccountId == userAccountId && j.StatusCd == "CLOSED")
            .OrderByDescending(j => j.UpdatedTs ?? j.CreatedTs)
            .Take(take)
            .Select(j => new { j.JobPostId, j.JobTitle, j.RequiredWorkerNbr, j.UpdatedTs, j.CreatedTs })
            .ToListAsync();

        return jobs.Select(j => new FilledPositionItem
        {
            JobPostId = j.JobPostId,
            JobTitle = j.JobTitle,
            RequiredWorkerNbr = j.RequiredWorkerNbr,
            ConfirmedWorkerNbr = confirmedCounts.TryGetValue(j.JobPostId, out var c) ? c : 0,
            FilledTs = j.UpdatedTs ?? j.CreatedTs,
        }).ToList();
    }

    public async Task<List<TodaysWorkerItem>> GetTodaysWorkersAsync(int userAccountId)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var rows = await _db.JobApplications
            .AsNoTracking()
            .Where(a => a.JobPost.EmployerUserAccountId == userAccountId &&
                        a.JobStartedTs != null &&
                        a.JobStartedTs.Value.Date == todayUtc)
            .OrderByDescending(a => a.JobStartedTs)
            .Select(a => new
            {
                a.JobApplicationId,
                a.JobPostId,
                a.JobPost.JobTitle,
                CheckInTs = a.JobStartedTs!.Value,
                EmployeeFirstName = a.EmployeeUserAccount.FirstName,
                EmployeeLastName = a.EmployeeUserAccount.LastName,
            })
            .ToListAsync();

        return rows.Select(r => new TodaysWorkerItem
        {
            JobPostId = r.JobPostId,
            JobApplicationId = r.JobApplicationId,
            WorkerName = $"{r.EmployeeFirstName} {r.EmployeeLastName}".Trim(),
            CategoryTxt = r.JobTitle,
            CheckInTs = r.CheckInTs,
        }).ToList();
    }

    public async Task<string?> GetCompanyNameAsync(int userAccountId) =>
        await _db.EmployerProfiles
                 .AsNoTracking()
                 .Where(p => p.UserAccountId == userAccountId)
                 .Select(p => p.CompanyName)
                 .FirstOrDefaultAsync();

    public Task<bool> IsEmployerApprovedAsync(int userAccountId) =>
        _db.UserAccounts
           .AsNoTracking()
           .Where(u => u.UserAccountId == userAccountId)
           .Select(u => u.IsApprovedFl)
           .FirstOrDefaultAsync();

    public Task<bool> IsEmployeeApprovedAsync(int userAccountId) =>
        _db.UserAccounts
           .AsNoTracking()
           .Where(u => u.UserAccountId == userAccountId)
           .Select(u => u.IsApprovedFl)
           .FirstOrDefaultAsync();

    // ── EMPLOYEE ──────────────────────────────────────────────────────────────

    public Task<int> GetApplicationsSentAsync(int userAccountId) =>
        _db.JobApplications
           .AsNoTracking()
           .CountAsync(a => a.EmployeeUserAccountId == userAccountId);

    public Task<int> GetContactsReceivedAsync(int userAccountId) =>
        _db.JobApplications
           .AsNoTracking()
           .CountAsync(a => a.EmployeeUserAccountId == userAccountId &&
                            (a.StatusCd == "EMPLOYER_CONTACTED" || a.StatusCd == "JOB_STARTED"));

    public Task<int> GetJobsCompletedAsync(int userAccountId) =>
        _db.JobApplications
           .AsNoTracking()
           .CountAsync(a => a.EmployeeUserAccountId == userAccountId && a.StatusCd == "COMPLETED");

    // NOTE: NOTIFICATION_LOG has no read/unread flag. This counts
    // successfully-delivered (STATUS_CD == "SENT") notifications for the
    // employee as the closest available proxy — it is NOT a true unread count.
    public Task<int> GetUnreadNotificationsAsync(int userAccountId) =>
        _db.NotificationLogs
           .AsNoTracking()
           .CountAsync(n => n.EmployeeUserAccountId == userAccountId && n.StatusCd == "SENT");

    public async Task<List<string>> GetNotificationSummaryAsync(int userAccountId, int take = 5)
    {
        var items = await _db.NotificationLogs
            .AsNoTracking()
            .Where(n => n.EmployeeUserAccountId == userAccountId && n.StatusCd == "SENT")
            .OrderByDescending(n => n.SentTs)
            .Take(take)
            .Select(n => n.MessageTxt ?? string.Empty)
            .ToListAsync();

        return items;
    }

    // ── ADMIN ─────────────────────────────────────────────────────────────────

    public Task<int> GetTotalEmployersAsync() =>
        _db.UserAccounts.AsNoTracking().CountAsync(u => u.RoleCd == "EMPLOYER" && u.IsActiveFl);

    public Task<int> GetTotalEmployeesAsync() =>
        _db.UserAccounts.AsNoTracking().CountAsync(u => u.RoleCd == "EMPLOYEE" && u.IsActiveFl);

    public Task<int> GetTotalJobsPostedAllAsync() =>
        _db.JobPosts.AsNoTracking().CountAsync();

    public Task<int> GetActiveJobsAllAsync() =>
        _db.JobPosts.AsNoTracking().CountAsync(j => j.StatusCd == "ACTIVE");

    public Task<int> GetOtpsGeneratedTodayAsync()
    {
        var today = DateTime.UtcNow.Date;
        return _db.OtpRecords
                  .AsNoTracking()
                  .CountAsync(o => o.GeneratedTs >= today);
    }

    public async Task<List<WeeklyJobData>> GetWeeklyJobChartAsync(int weeks = 4)
    {
        var result = new List<WeeklyJobData>();
        var now = DateTime.UtcNow.Date;

        // Walk back week-by-week
        for (int i = weeks - 1; i >= 0; i--)
        {
            var weekStart = now.AddDays(-(i + 1) * 7 - (int)now.DayOfWeek + 1);
            var weekEnd = weekStart.AddDays(7);

            var count = await _db.JobPosts
                .AsNoTracking()
                .CountAsync(j => j.CreatedTs >= weekStart && j.CreatedTs < weekEnd);

            result.Add(new WeeklyJobData
            {
                WeekLabel = $"{weekStart:MMM d}–{weekEnd.AddDays(-1):d}",
                JobsPosted = count,
            });
        }

        return result;
    }
}