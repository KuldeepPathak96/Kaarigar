using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class AdminNotificationLogService : IAdminNotificationLogService
{
    private readonly IAdminNotificationLogDao _dao;

    public AdminNotificationLogService(IAdminNotificationLogDao dao)
    {
        _dao = dao;
    }

    public async Task<NotificationLogsViewModel> SearchAsync(
        string? search, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var notificationLogs = await _dao.SearchNotificationLogsAsync(search, status, fromDate, toDate);
        var otpLogs = await _dao.SearchOtpLogsAsync(search, fromDate, toDate);

        return new NotificationLogsViewModel
        {
            TotalSentToday = await _dao.GetSentTodayCountAsync(),
            TotalFailedToday = await _dao.GetFailedTodayCountAsync(),
            OtpsGeneratedToday = await _dao.GetOtpsGeneratedTodayCountAsync(),
            NotificationLogs = notificationLogs,
            OtpLogs = otpLogs,
            SearchTerm = search,
            StatusFilter = status,
            FromDate = fromDate,
            ToDate = toDate,
        };
    }
}
