using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Kaarigar.Data;
using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kaarigar.Services;

/// <summary>
/// Sends WhatsApp notifications via the Meta WhatsApp Business Cloud API
/// (see Services/WhatsAppSettings.cs for configuration).
///
/// Wiring status: the HTTP integration below is fully implemented and ready
/// to go — the only thing missing is a real API key. Until "WhatsAppSettings"
/// in appsettings.json has an actual PhoneNumberId + AccessToken (i.e. while
/// WhatsAppSettings.IsConfigured is false), every call here safely falls
/// back to "record only" mode: the message is still written to
/// NOTIFICATION_LOG (so the Applications / notification-history UI has real
/// data to show and nothing else breaks), but no outbound HTTP request is
/// made. Drop the real PhoneNumberId/AccessToken into appsettings.json (or
/// user-secrets / an environment variable override) and messages will start
/// actually going out — no other code changes needed.
/// </summary>
public class WhatsAppNotificationService : IWhatsAppNotificationService
{
    private const string HttpClientName = "WhatsAppCloudApi";

    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppNotificationService> _logger;

    public WhatsAppNotificationService(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppNotificationService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task NotifyMatchingEmployeesAsync(JobPost jobPost, List<UserAccount> matchingEmployees)
    {
        if (matchingEmployees.Count == 0)
        {
            _logger.LogInformation("No matching employees found for JobPostId={Id} — no notifications sent.", jobPost.JobPostId);
            return;
        }

        var mapLineTxt = string.IsNullOrWhiteSpace(jobPost.GoogleMapsUrl)
            ? string.Empty
            : $" View map: {jobPost.GoogleMapsUrl}.";

        var messageTxt = $"New job: {jobPost.JobTitle} near {jobPost.LocationAddressTxt}.{mapLineTxt} " +
                          $"{jobPost.RequiredWorkerNbr} worker(s) needed. Open Kaarigar app to apply.";

        foreach (var employee in matchingEmployees)
        {
            await SendOneAsync(jobPost, employee, messageTxt);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Queued WhatsApp notifications to {Count} employees for JobPostId={Id}",
            matchingEmployees.Count, jobPost.JobPostId);
    }

    public async Task<bool> SendTakeWorkNotificationAsync(
        JobPost jobPost, UserAccount employee, UserAccount employer,
        string? businessName, string? employerContactPersonName)
    {
        var timingTxt = FormatJobTiming(jobPost);
        var wageTxt = jobPost.HourlyWageAmt.HasValue ? $"₹{jobPost.HourlyWageAmt.Value:N0}/hour" : "Not specified";
        var displayBusinessName = string.IsNullOrWhiteSpace(businessName) ? "The employer" : businessName;
        var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
        var employerDisplayName = string.IsNullOrWhiteSpace(employerContactPersonName)
            ? $"{employer.FirstName} {employer.LastName}".Trim()
            : employerContactPersonName;

        // To the employee: confirmation that they've taken this job, with
        // business name, job timing, amount and location, plus a tap-to-open
        // Google Maps link. Sent the moment they click "Take Work".
        var mapLineTxt = string.IsNullOrWhiteSpace(jobPost.GoogleMapsUrl)
            ? string.Empty
            : $" View map: {jobPost.GoogleMapsUrl}.";

        var employeeMessageTxt =
            $"You've taken the job \"{jobPost.JobTitle}\" with {displayBusinessName}. " +
            $"Timing: {timingTxt}. Amount: {wageTxt}. Location: {jobPost.LocationAddressTxt}.{mapLineTxt} " +
            "The employer will share a Job Starting OTP with you in person once you reach the location.";

        // To the employer: a notification that this Kaarigar has taken the job.
        var employerMessageTxt =
            $"{employeeName} has taken your job \"{jobPost.JobTitle}\". " +
            $"Timing: {timingTxt}. Location: {jobPost.LocationAddressTxt}. " +
            $"Your contact on file: {employerDisplayName}, {employer.ContactNbr}.";

        var employeeStatusCd = await SendViaProviderAsync(employee.ContactNbr, employeeMessageTxt);
        var employerStatusCd = await SendViaProviderAsync(employer.ContactNbr, employerMessageTxt);

        _db.NotificationLogs.Add(new NotificationLog
        {
            EmployeeUserAccountId = employee.UserAccountId,
            EmployerUserAccountId = jobPost.EmployerUserAccountId,
            JobPostId = jobPost.JobPostId,
            ChannelCd = "WHATSAPP",
            MessageTxt = employeeMessageTxt,
            SentTs = DateTime.UtcNow,
            StatusCd = employeeStatusCd,
            CreatedBy = "EMPLOYEE_TAKE_WORK",
        });

        _db.NotificationLogs.Add(new NotificationLog
        {
            EmployerUserAccountId = jobPost.EmployerUserAccountId,
            JobPostId = jobPost.JobPostId,
            ChannelCd = "WHATSAPP",
            MessageTxt = employerMessageTxt,
            SentTs = DateTime.UtcNow,
            StatusCd = employerStatusCd,
            CreatedBy = "EMPLOYEE_TAKE_WORK_CONFIRMATION",
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Sent WhatsApp Take Work notifications — EmployeeUserAccountId={EmployeeId} ({EmployeeStatus}), EmployerUserAccountId={EmployerId} ({EmployerStatus}), JobPostId={JobPostId}",
            employee.UserAccountId, employeeStatusCd, jobPost.EmployerUserAccountId, employerStatusCd, jobPost.JobPostId);

        return employeeStatusCd == "SENT";
    }

    public async Task<bool> SendCancellationNotificationAsync(
        JobPost jobPost, UserAccount employee, string cancelReasonLabel, string? cancelReasonTxt)
    {
        var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
        var reasonSuffixTxt = string.IsNullOrWhiteSpace(cancelReasonTxt) ? string.Empty : $" ({cancelReasonTxt})";

        var employeeMessageTxt =
            $"You have been cancelled by the employer for \"{jobPost.JobTitle}\". " +
            $"Reason: {cancelReasonLabel}{reasonSuffixTxt}. You can browse other jobs on Kaarigar anytime.";

        var employerMessageTxt =
            $"You cancelled {employeeName} for \"{jobPost.JobTitle}\". " +
            $"Reason: {cancelReasonLabel}{reasonSuffixTxt}. The position is open again if it isn't fully filled.";

        var employeeStatusCd = await SendViaProviderAsync(employee.ContactNbr, employeeMessageTxt);
        var employerAccount = await _db.UserAccounts.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserAccountId == jobPost.EmployerUserAccountId);
        var employerStatusCd = employerAccount != null
            ? await SendViaProviderAsync(employerAccount.ContactNbr, employerMessageTxt)
            : "FAILED";

        _db.NotificationLogs.Add(new NotificationLog
        {
            EmployeeUserAccountId = employee.UserAccountId,
            EmployerUserAccountId = jobPost.EmployerUserAccountId,
            JobPostId = jobPost.JobPostId,
            ChannelCd = "WHATSAPP",
            MessageTxt = employeeMessageTxt,
            SentTs = DateTime.UtcNow,
            StatusCd = employeeStatusCd,
            CreatedBy = "EMPLOYER_CANCEL_APPLICANT",
        });

        _db.NotificationLogs.Add(new NotificationLog
        {
            EmployerUserAccountId = jobPost.EmployerUserAccountId,
            JobPostId = jobPost.JobPostId,
            ChannelCd = "WHATSAPP",
            MessageTxt = employerMessageTxt,
            SentTs = DateTime.UtcNow,
            StatusCd = employerStatusCd,
            CreatedBy = "EMPLOYER_CANCEL_APPLICANT_CONFIRMATION",
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Sent WhatsApp cancellation notifications — EmployeeUserAccountId={EmployeeId} ({EmployeeStatus}), EmployerUserAccountId={EmployerId} ({EmployerStatus}), JobPostId={JobPostId}",
            employee.UserAccountId, employeeStatusCd, jobPost.EmployerUserAccountId, employerStatusCd, jobPost.JobPostId);

        return employeeStatusCd == "SENT";
    }

    // ── Provider integration ─────────────────────────────────────────────────

    /// <summary>
    /// Sends one WhatsApp text message via the Meta Cloud API and returns
    /// "SENT" or "FAILED". While WhatsAppSettings.IsConfigured is false (no
    /// real API key yet), this is a no-op that always returns "SENT" so the
    /// rest of the app behaves exactly as it did before this was wired in.
    /// </summary>
    private async Task<string> SendViaProviderAsync(string toContactNbr, string messageTxt)
    {
        if (!_settings.IsConfigured)
        {
            _logger.LogInformation(
                "WhatsApp API not configured yet (PhoneNumberId/AccessToken missing) — message recorded in NOTIFICATION_LOG only, not actually sent.");
            return "SENT"; // optimistic placeholder until a real API key is dropped in
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var url = $"{_settings.ApiBaseUrl.TrimEnd('/')}/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages";

            var payload = new WhatsAppTextMessageRequest
            {
                To = NormalizeToE164(toContactNbr),
                Text = new WhatsAppTextMessageBody { Body = messageTxt },
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return "SENT";

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "WhatsApp Cloud API call failed for {ToContactNbr}: {StatusCode} — {Body}",
                toContactNbr, (int)response.StatusCode, errorBody);
            return "FAILED";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp Cloud API call threw an exception for {ToContactNbr}.", toContactNbr);
            return "FAILED";
        }
    }

    /// <summary>
    /// Meta's Cloud API expects full international digits with no leading
    /// '+' (e.g. "919876543210"). CONTACT_NBR is stored as a plain 10-digit
    /// Indian mobile number, so prefix WhatsAppSettings.DefaultCountryCode
    /// unless the number already looks like it has a country code on it.
    /// </summary>
    private string NormalizeToE164(string contactNbr)
    {
        var digitsOnly = new string(contactNbr.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length == 10)
            return _settings.DefaultCountryCode + digitsOnly;

        return digitsOnly;
    }

    private static string FormatJobTiming(JobPost jobPost)
    {
        var start = jobPost.StartDateTime;
        var end = jobPost.EndDateTime;

        if (start == null) return "Not specified";

        var startTxt = start.Value.ToString("dd MMM yyyy, hh:mm tt");
        if (end == null || end == start) return startTxt;

        return $"{startTxt} - {end.Value.ToString("dd MMM yyyy, hh:mm tt")}";
    }

    private async Task SendOneAsync(JobPost jobPost, UserAccount employee, string messageTxt)
    {
        var statusCd = await SendViaProviderAsync(employee.ContactNbr, messageTxt);

        _db.NotificationLogs.Add(new NotificationLog
        {
            EmployeeUserAccountId = employee.UserAccountId,
            EmployerUserAccountId = jobPost.EmployerUserAccountId,
            JobPostId = jobPost.JobPostId,
            ChannelCd = "WHATSAPP",
            MessageTxt = messageTxt,
            SentTs = DateTime.UtcNow,
            StatusCd = statusCd,
            CreatedBy = "JOB_POST_NOTIFICATION",
        });
    }

    // ── Meta Cloud API request DTOs ──────────────────────────────────────────

    private class WhatsAppTextMessageRequest
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; set; } = "whatsapp";

        [JsonPropertyName("recipient_type")]
        public string RecipientType { get; set; } = "individual";

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public WhatsAppTextMessageBody Text { get; set; } = new();
    }

    private class WhatsAppTextMessageBody
    {
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("preview_url")]
        public bool PreviewUrl { get; set; } = true;
    }
}
