using System.Net;
using System.Net.Mail;

namespace Kaarigar.Services;

/// <summary>
/// Sends email via SMTP using settings from appsettings.json:
///
///   "SmtpSettings": {
///     "Host": "smtp.your-provider.com",
///     "Port": 587,
///     "EnableSsl": true,
///     "UserName": "no-reply@kaarigar.com",
///     "Password": "***",
///     "FromEmail": "no-reply@kaarigar.com",
///     "FromDisplayName": "Kaarigar"
///   }
/// </summary>
public class EmailSenderService : IEmailSenderService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(IConfiguration config, ILogger<EmailSenderService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var section = _config.GetSection("SmtpSettings");
        var host = section["Host"];
        var port = int.Parse(section["Port"] ?? "587");
        var enableSsl = bool.Parse(section["EnableSsl"] ?? "true");
        var userName = section["UserName"];
        var password = section["Password"];
        var fromEmail = section["FromEmail"] ?? userName ?? "no-reply@kaarigar.com";
        var fromDisplayName = section["FromDisplayName"] ?? "Kaarigar";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(userName, password),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromDisplayName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // Don't let SMTP outages leak internal errors to the user;
            // the caller (PasswordResetService) decides what to say to them.
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            throw;
        }
    }
}
