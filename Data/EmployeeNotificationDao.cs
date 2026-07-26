using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public interface IEmployeeNotificationDao
{
    Task<List<EmployeeNotificationListItemViewModel>> GetNotificationsAsync(int employeeUserAccountId, int take = 50);
}

public class EmployeeNotificationDao : IEmployeeNotificationDao
{
    private readonly AppDbContext _db;

    public EmployeeNotificationDao(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployeeNotificationListItemViewModel>> GetNotificationsAsync(int employeeUserAccountId, int take = 50)
    {
        var rows = await _db.NotificationLogs.AsNoTracking()
            .Where(n => n.EmployeeUserAccountId == employeeUserAccountId)
            .Include(n => n.JobPost)
            .OrderByDescending(n => n.SentTs)
            .Take(take)
            .ToListAsync();

        return rows.Select(n => new EmployeeNotificationListItemViewModel
        {
            NotificationLogId = n.NotificationLogId,
            JobTitle = n.JobPost?.JobTitle,
            MessageTxt = n.MessageTxt ?? string.Empty,
            SentTs = n.SentTs,
            StatusCd = n.StatusCd,
            MapUrl = n.JobPost?.GoogleMapsUrl,
        }).ToList();
    }
}
