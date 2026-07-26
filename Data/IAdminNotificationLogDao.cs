using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>Data Access Object interface for Screen A-06 (Notification Logs).</summary>
public interface IAdminNotificationLogDao
{
    /// <summary>
    /// Searches/filters WhatsApp NOTIFICATION_LOG rows. All filters are optional and combine with AND.
    /// search matches Employee Name or Job Title (case-insensitive, partial).
    /// status is "SENT" | "FAILED" | null (= no status filter).
    /// fromDate/toDate filter on NOTIFICATION_LOG.SENT_TS (inclusive), either may be null.
    /// </summary>
    Task<List<NotificationLogListItemViewModel>> SearchNotificationLogsAsync(
        string? search, string? status, DateTime? fromDate, DateTime? toDate);

    /// <summary>
    /// Searches/filters OTP_RECORD rows (job-site App OTP, not login OTP).
    /// search matches Employee Name or Job Title (case-insensitive, partial).
    /// </summary>
    Task<List<OtpLogListItemViewModel>> SearchOtpLogsAsync(
        string? search, DateTime? fromDate, DateTime? toDate);

    /// <summary>KPI: count of NOTIFICATION_LOG rows with STATUS_CD = 'SENT' and SENT_TS = today (UTC).</summary>
    Task<int> GetSentTodayCountAsync();

    /// <summary>KPI: count of NOTIFICATION_LOG rows with STATUS_CD = 'FAILED' and SENT_TS = today (UTC).</summary>
    Task<int> GetFailedTodayCountAsync();

    /// <summary>KPI: count of OTP_RECORD rows generated today (UTC) — matches the A-02 Dashboard "OTPs Generated Today" card.</summary>
    Task<int> GetOtpsGeneratedTodayCountAsync();
}
