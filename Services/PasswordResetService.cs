using BCrypt.Net;
using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IPasswordResetDao _dao;
    private readonly IEmailSenderService _emailSender;
    private readonly ILogger<PasswordResetService> _logger;

    private const int OtpLength = 6;
    private static readonly TimeSpan OtpValidity = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromMinutes(15);
    private const int MaxRequestsPerWindow = 3;

    public PasswordResetService(
        IPasswordResetDao dao,
        IEmailSenderService emailSender,
        ILogger<PasswordResetService> logger)
    {
        _dao = dao;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<ServiceResult> RequestOtpAsync(ForgotPasswordModel model)
    {
        var email = model.EmailId.Trim().ToLowerInvariant();
        var user = await _dao.GetUserByEmailAsync(email);

        // Always return the same generic message whether or not the email
        // exists, so the form can't be used to enumerate registered emails.
        const string genericMessage =
            "If this email is registered, an OTP has been sent to it.";

        if (user == null || string.IsNullOrWhiteSpace(user.ContactNbr))
        {
            _logger.LogInformation("Forgot-password requested for unknown/invalid email {Email}", email);
            return new ServiceResult(true, genericMessage);
        }

        var recentCount = await _dao.CountRecentOtpRequestsAsync(user.ContactNbr, ThrottleWindow);
        if (recentCount >= MaxRequestsPerWindow)
        {
            return new ServiceResult(false,
                "Too many OTP requests. Please wait a few minutes and try again.");
        }

        // Invalidate any older, still-unused OTPs for this user first.
        await _dao.InvalidatePreviousOtpsAsync(user.ContactNbr);

        var otpCd = GenerateOtp();
        var otp = new MobileVerificationOtp
        {
            ContactNbr = user.ContactNbr,
            OtpCd = otpCd,
            PurposeCd = "FORGOT_PASSWORD",
            GeneratedTs = DateTime.UtcNow,
            ExpiresTs = DateTime.UtcNow.Add(OtpValidity),
            IsUsedFl = false,
            CreatedBy = "FORGOT_PASSWORD_REQUEST",
        };
        await _dao.CreateOtpAsync(otp);

        try
        {
            await _emailSender.SendAsync(
                toEmail: user.EmailId!,
                subject: "Kaarigar — Your password reset OTP",
                htmlBody: BuildOtpEmailBody(user.FirstName, otpCd));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to email OTP to {Email}", email);
            return new ServiceResult(false,
                "We couldn't send the OTP email right now. Please try again shortly.");
        }

        return new ServiceResult(true, genericMessage);
    }

    public async Task<ServiceResult> VerifyOtpAsync(VerifyOtpModel model)
    {
        var email = model.EmailId.Trim().ToLowerInvariant();
        var user = await _dao.GetUserByEmailAsync(email);
        if (user == null)
            return new ServiceResult(false, "Invalid or expired OTP.");

        var otp = await _dao.GetLatestValidOtpAsync(user.ContactNbr, model.OtpCd.Trim());
        if (otp == null)
            return new ServiceResult(false, "Invalid or expired OTP. Please request a new one.");

        // Intentionally NOT marking the OTP as used here — it's only fully
        // consumed once the new password is actually saved in
        // ResetPasswordAsync, so a user who abandons the reset can retry
        // Step 3 with the same OTP until it expires.
        return new ServiceResult(true, "OTP verified. You can now set a new password.");
    }

    public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordModel model)
    {
        var email = model.EmailId.Trim().ToLowerInvariant();
        var user = await _dao.GetUserByEmailAsync(email);
        if (user == null)
            return new ServiceResult(false, "Invalid or expired OTP.");

        var otp = await _dao.GetLatestValidOtpAsync(user.ContactNbr, model.OtpCd.Trim());
        if (otp == null)
            return new ServiceResult(false, "Invalid or expired OTP. Please restart the password reset process.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, workFactor: 12);
        await _dao.UpdatePasswordHashAsync(user.UserAccountId, newHash);
        await _dao.MarkOtpUsedAsync(otp.MobileVerificationOtpId);

        _logger.LogInformation("Password reset completed for UserAccountId={Id}", user.UserAccountId);

        return new ServiceResult(true, "Your password has been reset successfully. Please log in.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GenerateOtp()
    {
        // Cryptographically-random 6-digit OTP (000000–999999, zero-padded).
        return System.Security.Cryptography.RandomNumberGenerator
            .GetInt32(0, 1_000_000)
            .ToString("D6");
    }

    private static string BuildOtpEmailBody(string firstName, string otpCd) => $@"
        <div style='font-family:Segoe UI,Arial,sans-serif;max-width:480px;margin:0 auto'>
            <h2 style='color:#1a5632'>Kaarigar Password Reset</h2>
            <p>Hi {System.Net.WebUtility.HtmlEncode(firstName)},</p>
            <p>Use the OTP below to reset your password. It is valid for 10 minutes.</p>
            <div style='font-size:28px;font-weight:bold;letter-spacing:6px;
                        background:#f2f2f2;padding:16px;text-align:center;border-radius:8px'>
                {otpCd}
            </div>
            <p style='color:#777;font-size:13px;margin-top:24px'>
                If you didn't request this, you can safely ignore this email.
            </p>
        </div>";
}
