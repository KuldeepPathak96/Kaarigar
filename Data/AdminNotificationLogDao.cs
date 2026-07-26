using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class AdminNotificationLogDao : IAdminNotificationLogDao
{
    private readonly AppDbContext _db;

    public AdminNotificationLogDao(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<NotificationLogListItemViewModel>> SearchNotificationLogsAsync(
        string? search, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var query =
            from log in _db.NotificationLogs.AsNoTracking()
            join emp in _db.UserAccounts.AsNoTracking() on log.EmployeeUserAccountId equals emp.UserAccountId into empJoin
            from emp in empJoin.DefaultIfEmpty()
            join empr in _db.UserAccounts.AsNoTracking() on log.EmployerUserAccountId equals empr.UserAccountId into emprJoin
            from empr in emprJoin.DefaultIfEmpty()
            join job in _db.JobPosts.AsNoTracking() on log.JobPostId equals job.JobPostId into jobJoin
            from job in jobJoin.DefaultIfEmpty()
            select new { log, emp, empr, job };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                (x.emp != null && (x.emp.FirstName + " " + x.emp.LastName).ToLower().Contains(term)) ||
                (x.job != null && x.job.JobTitle.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.log.StatusCd == status);

        if (fromDate.HasValue)
            query = query.Where(x => x.log.SentTs >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(x => x.log.SentTs < toDate.Value.Date.AddDays(1));

        var rows = await query.OrderByDescending(x => x.log.SentTs).ToListAsync();

        return rows.Select(x => new NotificationLogListItemViewModel
        {
            NotificationLogId = x.log.NotificationLogId,
            EmployeeName = x.emp != null ? $"{x.emp.FirstName} {x.emp.LastName}".Trim() : "—",
            EmployerName = x.empr != null ? $"{x.empr.FirstName} {x.empr.LastName}".Trim() : null,
            JobTitle = x.job?.JobTitle,
            SentTs = x.log.SentTs,
            ChannelCd = x.log.ChannelCd,
            StatusCd = x.log.StatusCd,
            MessageTxt = x.log.MessageTxt,
        }).ToList();
    }

    public async Task<List<OtpLogListItemViewModel>> SearchOtpLogsAsync(
        string? search, DateTime? fromDate, DateTime? toDate)
    {
        var query =
            from otp in _db.OtpRecords.AsNoTracking()
            join emp in _db.UserAccounts.AsNoTracking() on otp.EmployeeUserAccountId equals emp.UserAccountId
            join job in _db.JobPosts.AsNoTracking() on otp.JobPostId equals job.JobPostId into jobJoin
            from job in jobJoin.DefaultIfEmpty()
            select new { otp, emp, job };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                (x.emp.FirstName + " " + x.emp.LastName).ToLower().Contains(term) ||
                (x.job != null && x.job.JobTitle.ToLower().Contains(term)));
        }

        if (fromDate.HasValue)
            query = query.Where(x => x.otp.GeneratedTs >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(x => x.otp.GeneratedTs < toDate.Value.Date.AddDays(1));

        var rows = await query.OrderByDescending(x => x.otp.GeneratedTs).ToListAsync();

        return rows.Select(x => new OtpLogListItemViewModel
        {
            OtpRecordId = x.otp.OtpRecordId,
            EmployeeName = $"{x.emp.FirstName} {x.emp.LastName}".Trim(),
            JobTitle = x.job?.JobTitle,
            GeneratedTs = x.otp.GeneratedTs,
            IsUsedFl = x.otp.IsUsedFl,
        }).ToList();
    }

    public Task<int> GetSentTodayCountAsync() =>
        _db.NotificationLogs.AsNoTracking()
           .CountAsync(n => n.StatusCd == "SENT" && n.SentTs.Date == DateTime.UtcNow.Date);

    public Task<int> GetFailedTodayCountAsync() =>
        _db.NotificationLogs.AsNoTracking()
           .CountAsync(n => n.StatusCd == "FAILED" && n.SentTs.Date == DateTime.UtcNow.Date);

    public Task<int> GetOtpsGeneratedTodayCountAsync() =>
        _db.OtpRecords.AsNoTracking()
           .CountAsync(o => o.GeneratedTs.Date == DateTime.UtcNow.Date);
}
