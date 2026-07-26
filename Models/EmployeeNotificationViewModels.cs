namespace Kaarigar.Models;

/// <summary>A single WhatsApp alert shown to the employee on their own /notifications page.</summary>
public class EmployeeNotificationListItemViewModel
{
    public int NotificationLogId { get; set; }
    public string? JobTitle { get; set; }
    public string MessageTxt { get; set; } = string.Empty;
    public DateTime SentTs { get; set; }
    public string StatusCd { get; set; } = string.Empty;

    /// <summary>Clickable Google Maps link for the job site, when GPS coordinates were captured.</summary>
    public string? MapUrl { get; set; }
}

public class EmployeeNotificationsViewModel
{
    public List<EmployeeNotificationListItemViewModel> Notifications { get; set; } = new();
}
