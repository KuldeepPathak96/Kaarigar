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

    public async Task<List<ActivityFeedItem>> GetRecentActivityAsync(int userAccountId, int take = 5)
    {
        // Join JobApplications → JobPosts (employer filter) → UserAccounts (employee name)
        var rows = await _db.JobApplications
            .AsNoTracking()
            .Where(a => a.JobPost.EmployerUserAccountId == userAccountId)
            .OrderByDescending(a => a.UpdatedTs ?? a.CreatedTs)
            .Take(take)
            .Select(a => new
            {
                a.StatusCd,
                a.UpdatedTs,
                a.CreatedTs,
                EmployeeFirstName = a.EmployeeUserAccount.FirstName,
                EmployeeLastName = a.EmployeeUserAccount.LastName,
                JobTitle = a.JobPost.JobTitle,
            })
            .ToListAsync();

        return rows.Select(r => new ActivityFeedItem
        {
            EmployeeName = $"{r.EmployeeFirstName} {r.EmployeeLastName}".Trim(),
            JobTitle = r.JobTitle,
            Action = MapStatusToAction(r.StatusCd),
            Timestamp = r.UpdatedTs ?? r.CreatedTs,
            AvatarInitial = r.EmployeeFirstName.Length > 0 ? r.EmployeeFirstName[..1].ToUpper() : "?",
            StatusCd = r.StatusCd,
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string MapStatusToAction(string statusCd) => statusCd switch
    {
        "PENDING" => "expressed interest",
        "EMPLOYER_VIEWED" => "profile viewed by employer",
        "EMPLOYER_CONTACTED" => "contacted by employer",
        "JOB_STARTED" => "job started (OTP verified)",
        "COMPLETED" => "job completed",
        _ => "updated application",
    };
}