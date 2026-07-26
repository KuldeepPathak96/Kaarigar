namespace Kaarigar.Services;

/// <summary>
/// Bound from the "WhatsAppSettings" section of appsettings.json.
/// Once real Meta WhatsApp Cloud API credentials are available, replace
/// PhoneNumberId / AccessToken / BusinessAccountId below (or the values in
/// appsettings.json / an environment-specific override / user-secrets) —
/// no code changes are needed for that step.
/// </summary>
public class WhatsAppSettings
{
    /// <summary>Informational only — which provider these settings target.</summary>
    public string Provider { get; set; } = "META_CLOUD_API";

    /// <summary>Meta Graph API base URL. Rarely needs changing.</summary>
    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com";

    /// <summary>Graph API version segment, e.g. "v20.0".</summary>
    public string ApiVersion { get; set; } = "v20.0";

    /// <summary>The WhatsApp Business phone number ID (from Meta Business Manager / WhatsApp Manager).</summary>
    public string? PhoneNumberId { get; set; }

    /// <summary>Permanent (or long-lived) access token generated for the WhatsApp Business app.</summary>
    public string? AccessToken { get; set; }

    /// <summary>WhatsApp Business Account ID — not required for sending messages, kept for reference/future use.</summary>
    public string? BusinessAccountId { get; set; }

    /// <summary>
    /// Country code (no leading '+') prefixed onto the 10-digit CONTACT_NBR values
    /// stored in USER_ACCOUNT/JOB_POST when calling the WhatsApp API, since Meta
    /// expects full E.164-style digits (e.g. "919876543210").
    /// </summary>
    public string DefaultCountryCode { get; set; } = "91";

    /// <summary>
    /// True once real credentials have been filled in. While this is false, the
    /// app keeps working exactly as before: notifications are still recorded in
    /// NOTIFICATION_LOG, but no outbound HTTP call is made — so nothing breaks
    /// before an API key/token is available.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PhoneNumberId) && PhoneNumberId != "REPLACE_ME" &&
        !string.IsNullOrWhiteSpace(AccessToken) && AccessToken != "REPLACE_ME";
}
