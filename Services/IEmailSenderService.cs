namespace Kaarigar.Services;

/// <summary>
/// Thin abstraction over outbound email so PasswordResetService doesn't
/// depend directly on SmtpClient. Swap the implementation for SendGrid /
/// SES / etc. later without touching the Service or Dao layers.
/// </summary>
public interface IEmailSenderService
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
}
