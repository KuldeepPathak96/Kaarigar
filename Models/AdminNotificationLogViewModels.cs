namespace Kaarigar.Models;

/// <summary>Screen A-06 (top table): one WhatsApp notification log row.</summary>
public class NotificationLogListItemViewModel
{
    public int NotificationLogId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployerName { get; set; }
    public string? JobTitle { get; set; }
    public DateTime SentTs { get; set; }

    /// <summary>'WHATSAPP' | 'SMS'</summary>
    public string ChannelCd { get; set; } = string.Empty;

    /// <summary>'SENT' | 'FAILED'</summary>
    public string StatusCd { get; set; } = string.Empty;

    /// <summary>Full message body that was sent — shown in the "View" detail popup.</summary>
    public string? MessageTxt { get; set; }
}

/// <summary>Screen A-06 (bottom table): one App-OTP (job-site identity verification) log row.</summary>
public class OtpLogListItemViewModel
{
    public int OtpRecordId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public DateTime GeneratedTs { get; set; }
    public bool IsUsedFl { get; set; }
}

/// <summary>Screen A-06 page model: KPI summary + both logs + the filter state to re-populate the form.</summary>
public class NotificationLogsViewModel
{
    // KPI strip
    public int TotalSentToday { get; set; }
    public int TotalFailedToday { get; set; }
    public int OtpsGeneratedToday { get; set; }

    public List<NotificationLogListItemViewModel> NotificationLogs { get; set; } = new();
    public List<OtpLogListItemViewModel> OtpLogs { get; set; } = new();

    public string? SearchTerm { get; set; }
    /// <summary>"SENT" | "FAILED" | null (= all)</summary>
    public string? StatusFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
