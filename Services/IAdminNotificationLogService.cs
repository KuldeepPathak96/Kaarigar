using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IAdminNotificationLogService
{
    Task<NotificationLogsViewModel> SearchAsync(
        string? search, string? status, DateTime? fromDate, DateTime? toDate);
}
