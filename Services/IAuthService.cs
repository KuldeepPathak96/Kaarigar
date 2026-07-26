using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IAuthService
{
    Task<ServiceResult> RegisterAsync(RegisterModel model, string? ipAddress = null);
    Task<(ServiceResult Result, UserAccount? User)> LoginAsync(LoginModel model);

    /// <summary>
    /// Screen A-01: Admin-only login. Identical credential check to LoginAsync,
    /// but additionally rejects any account whose ROLE_CD isn't 'ADMIN' — so an
    /// Employer/Employee's correct password still can't get them into /admin/login.
    /// </summary>
    Task<(ServiceResult Result, UserAccount? User)> AdminLoginAsync(LoginModel model);
}

public record ServiceResult(bool Success, string Message);