using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IWhatsAppNotificationService
{
    /// <summary>
    /// Sends a WhatsApp notification about a new job post to each matching
    /// employee, and logs each attempt to NOTIFICATION_LOG.
    /// </summary>
    Task NotifyMatchingEmployeesAsync(JobPost jobPost, List<UserAccount> matchingEmployees);

    /// <summary>
    /// Sent when the Employee clicks "Take Work" (Screen W-03/W-04) to apply
    /// for the job. Pushes a WhatsApp message to the employee with the
    /// business name, job timing, amount, location and a map link, and a
    /// notification WhatsApp message to the employer that this Kaarigar has
    /// taken the job. Both are logged to NOTIFICATION_LOG.
    /// </summary>
    Task<bool> SendTakeWorkNotificationAsync(
        JobPost jobPost, UserAccount employee, UserAccount employer,
        string? businessName, string? employerContactPersonName);

    /// <summary>Notifies both the Kaarigar and the employer (confirmation) that the employer cancelled this applicant.</summary>
    Task<bool> SendCancellationNotificationAsync(
        JobPost jobPost, UserAccount employee, string cancelReasonLabel, string? cancelReasonTxt);
}
