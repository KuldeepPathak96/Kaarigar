using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardDao _dao;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IDashboardDao dao, ILogger<DashboardService> logger)
    {
        _dao = dao;
        _logger = logger;
    }

    public async Task<EmployerDashboardViewModel> GetEmployerDashboardAsync(int userAccountId)
    {
        // NOTE: these must run sequentially, not concurrently via Task.WhenAll.
        // All DAO calls share a single scoped AppDbContext instance, and
        // DbContext is not thread-safe for overlapping operations — firing
        // them concurrently caused "a second operation was started on this
        // context before a previous operation completed".
        var total = await _dao.GetTotalJobsPostedAsync(userAccountId);
        var active = await _dao.GetActiveJobsCountAsync(userAccountId);
        var apps = await _dao.GetTotalApplicationsAsync(userAccountId);
        var filled = await _dao.GetJobsFilledAsync(userAccountId);
        var activity = await _dao.GetRecentActivityAsync(userAccountId, take: 5);
        var company = await _dao.GetCompanyNameAsync(userAccountId);
        var isApproved = await _dao.IsEmployerApprovedAsync(userAccountId);

        return new EmployerDashboardViewModel
        {
            TotalJobsPosted = total,
            ActiveJobs = active,
            TotalApplications = apps,
            JobsFilled = filled,
            RecentActivity = activity,
            CompanyName = company,
            IsApproved = isApproved,
        };
    }

    public async Task<EmployeeDashboardViewModel> GetEmployeeDashboardAsync(int userAccountId)
    {
        var sent = await _dao.GetApplicationsSentAsync(userAccountId);
        var contacted = await _dao.GetContactsReceivedAsync(userAccountId);
        var completed = await _dao.GetJobsCompletedAsync(userAccountId);
        var unread = await _dao.GetUnreadNotificationsAsync(userAccountId);
        var notifs = await _dao.GetNotificationSummaryAsync(userAccountId, take: 5);
        var isApproved = await _dao.IsEmployeeApprovedAsync(userAccountId);

        return new EmployeeDashboardViewModel
        {
            ApplicationsSent = sent,
            ContactsReceived = contacted,
            JobsCompleted = completed,
            UnreadNotifications = unread,
            NotificationSummary = notifs,
            IsApproved = isApproved,
        };
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
    {
        var employers = await _dao.GetTotalEmployersAsync();
        var employees = await _dao.GetTotalEmployeesAsync();
        var total = await _dao.GetTotalJobsPostedAllAsync();
        var active = await _dao.GetActiveJobsAllAsync();
        var otps = await _dao.GetOtpsGeneratedTodayAsync();
        var chart = await _dao.GetWeeklyJobChartAsync(weeks: 4);

        return new AdminDashboardViewModel
        {
            TotalEmployers = employers,
            TotalEmployees = employees,
            TotalJobsPosted = total,
            ActiveJobs = active,
            OtpsGeneratedToday = otps,
            WeeklyJobChart = chart,
        };
    }
}