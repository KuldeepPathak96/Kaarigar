namespace Kaarigar.Services;

/// <summary>
/// Thin abstraction over outbound SMS, mirroring IEmailSenderService. Swap
/// the implementation for MSG91 / Twilio / Gupshup / etc. later without
/// touching AuthService or LoginOtpDao.
/// </summary>
public interface ISmsSenderService
{
    Task SendAsync(string toContactNbr, string message);
}
