using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IPasswordResetService
{
    /// <summary>Step 1: validates the email exists, generates + emails a 6-digit OTP.</summary>
    Task<ServiceResult> RequestOtpAsync(ForgotPasswordModel model);

    /// <summary>Step 2: validates the OTP is correct, unused, and not expired.</summary>
    Task<ServiceResult> VerifyOtpAsync(VerifyOtpModel model);

    /// <summary>Step 3: re-validates the OTP, then updates the password and consumes the OTP.</summary>
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordModel model);
}
