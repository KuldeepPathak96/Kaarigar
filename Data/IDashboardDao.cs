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
    Task<string?> GetCompanyNameAsync(int userAccountId);

    /// <summary>ACTIVE job posts with fill progress ("5/6 Confirmed"), for the expanded "Live Shift Status" card. Ordered soonest-starting first.</summary>
    Task<List<LiveShiftItem>> GetLiveShiftsAsync(int userAccountId);

    /// <summary>Most recent applications received across all of this employer's jobs, for the expanded "Worker Applications" card.</summary>
    Task<List<ApplicationDetailItem>> GetRecentApplicationsAsync(int userAccountId, int take = 10);

    /// <summary>CLOSED job posts, most recently filled first, for the expanded "Positions Filled" card.</summary>
    Task<List<FilledPositionItem>> GetRecentlyFilledAsync(int userAccountId, int take = 10);

    /// <summary>Workers who checked in (JOB_STARTED) today, across any of this employer's jobs. Newest check-in first.</summary>
    Task<List<TodaysWorkerItem>> GetTodaysWorkersAsync(int userAccountId);

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
