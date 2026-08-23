using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data access for the OTP-based Login flow. Reuses the same
/// MOBILE_VERIFICATION_OTP table as Forgot Password (PurposeCd = "LOGIN"
/// instead of "FORGOT_PASSWORD"), so no new table is needed.
/// </summary>
public interface ILoginOtpDao
{
    /// <summary>Looks up an active user by registered contact number.</summary>
    Task<UserAccount?> GetUserByContactNbrAsync(string contactNbr);

    /// <summary>Invalidates any previous, unused LOGIN OTPs for this contact number.</summary>
    Task InvalidatePreviousOtpsAsync(string contactNbr);

    Task<MobileVerificationOtp> CreateOtpAsync(MobileVerificationOtp otp);

    /// <summary>Fetches the most recent, not-yet-used, not-expired LOGIN OTP for the given contact number.</summary>
    Task<MobileVerificationOtp?> GetLatestValidOtpAsync(string contactNbr, string otpCd);

    Task MarkOtpUsedAsync(int mobileVerificationOtpId);

    /// <summary>Throttling guard: counts LOGIN OTPs generated for this contact number within the given time window.</summary>
    Task<int> CountRecentOtpRequestsAsync(string contactNbr, TimeSpan window);
}
