namespace Kaarigar.Services;

/// <summary>
/// TEMPORARY stub. No SMS gateway is configured yet (MSG91 / Twilio /
/// Gupshup / etc. — pick one and wire it in here). Until then this just
/// logs the message so OTPs can be read from the server log during testing.
///
/// TODO (pending — needs an SMS provider account + API key):
///   1. Add the provider's SDK / HTTP client here.
///   2. Replace the body of SendAsync with an actual API call.
///   3. Add the provider's API key to appsettings.json (same pattern as
///      GoogleMaps:ApiKey) and inject IConfiguration here to read it.
///   4. No other file needs to change — AuthService and LoginOtpDao only
///      depend on ISmsSenderService, not on this class directly.
/// </summary>
public class SmsSenderService : ISmsSenderService
{
    private readonly ILogger<SmsSenderService> _logger;

    public SmsSenderService(ILogger<SmsSenderService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toContactNbr, string message)
    {
        // ── SMS gateway not configured yet — logging instead of sending. ──
        _logger.LogWarning(
            "[SMS NOT SENT — gateway not configured] To: {ContactNbr} | Message: {Message}",
            toContactNbr, message);

        return Task.CompletedTask;
    }
}
