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

    /// <summary>
    /// Sends a PDF (or other document) as a WhatsApp document message with a
    /// caption — used for invoices/statements. documentUrl must be a public
    /// HTTPS URL (Meta's Cloud API fetches the file from that link; it does
    /// not accept local file paths). Returns "SENT" or "FAILED" and logs to
    /// NOTIFICATION_LOG the same way the other Send* methods do.
    /// </summary>
    Task<string> SendDocumentAsync(string toContactNbr, string documentUrl, string filename, string caption);
}
