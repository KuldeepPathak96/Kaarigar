using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data Access Object interface for the Forgot Password / Reset Password flow.
/// Keeps raw DB calls out of the service layer.
/// </summary>
public interface IPasswordResetDao
{
    /// <summary>Looks up an active user by registered email (case-insensitive).</summary>
    Task<UserAccount?> GetUserByEmailAsync(string emailId);

    /// <summary>Invalidates any previous, unused FORGOT_PASSWORD OTPs for this contact number.</summary>
    Task InvalidatePreviousOtpsAsync(string contactNbr);

    /// <summary>Persists a newly generated OTP record.</summary>
    Task<MobileVerificationOtp> CreateOtpAsync(MobileVerificationOtp otp);

    /// <summary>
    /// Fetches the most recent, not-yet-used, not-expired FORGOT_PASSWORD OTP
    /// for the given contact number.
    /// </summary>
    Task<MobileVerificationOtp?> GetLatestValidOtpAsync(string contactNbr, string otpCd);

    /// <summary>Marks the given OTP record as used.</summary>
    Task MarkOtpUsedAsync(int mobileVerificationOtpId);

    /// <summary>Updates the user's password hash.</summary>
    Task UpdatePasswordHashAsync(int userAccountId, string newPasswordHash);

    /// <summary>
    /// Simple throttling guard: counts how many OTPs were generated for this
    /// contact number within the given time window (e.g. last 15 minutes).
    /// </summary>
    Task<int> CountRecentOtpRequestsAsync(string contactNbr, TimeSpan window);
}
