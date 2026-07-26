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
    /// Pushes a job-site identity-verification OTP (Screen W-06) to the
    /// employee over WhatsApp, and logs the attempt to NOTIFICATION_LOG so
    /// it shows up in the Admin "Notification Logs" screen (A-06) alongside
    /// the OTP_RECORD row itself.
    /// </summary>
    Task<bool> SendOtpAsync(JobPost jobPost, UserAccount employee, string otpCd);

    /// <summary>
    /// Sent when the Employer clicks "Contact Employee" (Screen E-05).
    /// Pushes a WhatsApp message to the employee with the business name, job
    /// timing, amount and location, and a confirmation WhatsApp message to
    /// the employer with their own contact/name, timing and job details.
    /// Both are logged to NOTIFICATION_LOG.
    /// </summary>
    Task<bool> SendContactNotificationAsync(
        JobPost jobPost, UserAccount employee, UserAccount employer,
        string? businessName, string? employerContactPersonName);

    /// <summary>Notifies both the Kaarigar and the employer (confirmation) that the employer cancelled this applicant.</summary>
    Task<bool> SendCancellationNotificationAsync(
        JobPost jobPost, UserAccount employee, string cancelReasonLabel, string? cancelReasonTxt);
}
