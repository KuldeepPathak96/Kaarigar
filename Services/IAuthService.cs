using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IAuthService
{
    Task<ServiceResult> RegisterAsync(RegisterModel model, string? ipAddress = null);
    Task<(ServiceResult Result, UserAccount? User)> LoginAsync(LoginModel model);

    /// <summary>Step 1 of OTP login: generates a 4-digit OTP and sends it via SMS to the given (already-registered) mobile number.</summary>
    Task<ServiceResult> RequestLoginOtpAsync(string contactNbr);

    /// <summary>Step 2 of OTP login: verifies the OTP and, if valid, signs the user in — no password involved.</summary>
    Task<(ServiceResult Result, UserAccount? User)> LoginWithOtpAsync(string contactNbr, string otpCd);

    /// <summary>
    /// Screen A-01: Admin-only login. Identical credential check to LoginAsync,
    /// but additionally rejects any account whose ROLE_CD isn't 'ADMIN' — so an
    /// Employer/Employee's correct password still can't get them into /admin/login.
    /// </summary>
    Task<(ServiceResult Result, UserAccount? User)> AdminLoginAsync(LoginModel model);
}

public record ServiceResult(bool Success, string Message);