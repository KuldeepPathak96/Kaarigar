using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data Access Object interface for all Dashboard queries.
/// Keeps raw DB calls out of the service layer.
/// </summary>
public interface IDashboardDao
{
    // ── Employer ─────────────────────────────────────────────────────────────
    Task<int>  GetTotalJobsPostedAsync(int userAccountId);
    Task<int>  GetActiveJobsCountAsync(int userAccountId);
    Task<int>  GetTotalApplicationsAsync(int userAccountId);
    Task<int>  GetJobsFilledAsync(int userAccountId);
    Task<List<ActivityFeedItem>> GetRecentActivityAsync(int userAccountId, int take = 5);
    Task<string?> GetCompanyNameAsync(int userAccountId);

    /// <summary>True once Admin has approved this employer's USER_ACCOUNT.</summary>
    Task<bool> IsEmployerApprovedAsync(int userAccountId);

    /// <summary>True once Admin has approved this employee's USER_ACCOUNT.</summary>
    Task<bool> IsEmployeeApprovedAsync(int userAccountId);

    // ── Employee ──────────────────────────────────────────────────────────────
    Task<int>  GetApplicationsSentAsync(int userAccountId);
    Task<int>  GetContactsReceivedAsync(int userAccountId);
    Task<int>  GetJobsCompletedAsync(int userAccountId);
    Task<int>  GetUnreadNotificationsAsync(int userAccountId);
    Task<List<string>> GetNotificationSummaryAsync(int userAccountId, int take = 5);

    // ── Admin ─────────────────────────────────────────────────────────────────
    Task<int>  GetTotalEmployersAsync();
    Task<int>  GetTotalEmployeesAsync();
    Task<int>  GetTotalJobsPostedAllAsync();
    Task<int>  GetActiveJobsAllAsync();
    Task<int>  GetOtpsGeneratedTodayAsync();
    Task<List<WeeklyJobData>> GetWeeklyJobChartAsync(int weeks = 4);
}
