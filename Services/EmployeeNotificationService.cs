using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IEmployeeNotificationService
{
    Task<EmployeeNotificationsViewModel> GetForEmployeeAsync(int employeeUserAccountId);
}

public class EmployeeNotificationService : IEmployeeNotificationService
{
    private readonly IEmployeeNotificationDao _dao;

    public EmployeeNotificationService(IEmployeeNotificationDao dao)
    {
        _dao = dao;
    }

    public async Task<EmployeeNotificationsViewModel> GetForEmployeeAsync(int employeeUserAccountId)
    {
        var notifications = await _dao.GetNotificationsAsync(employeeUserAccountId);
        return new EmployeeNotificationsViewModel { Notifications = notifications };
    }
}
