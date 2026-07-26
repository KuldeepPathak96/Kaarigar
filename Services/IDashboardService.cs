using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IDashboardService
{
    Task<EmployerDashboardViewModel>  GetEmployerDashboardAsync(int userAccountId);
    Task<EmployeeDashboardViewModel>  GetEmployeeDashboardAsync(int userAccountId);
    Task<AdminDashboardViewModel>     GetAdminDashboardAsync();
}
