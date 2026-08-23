using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Kaarigar.Data;
using Kaarigar.Models;
using Kaarigar.Services;

namespace Kaarigar.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuthService> _logger;
    private readonly ILoginOtpDao _loginOtpDao;
    private readonly ISmsSenderService _smsSender;
    private readonly string _webOtpDomain;

    private const int LoginOtpLength = 4;
    private static readonly TimeSpan LoginOtpValidity = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LoginOtpThrottleWindow = TimeSpan.FromMinutes(15);
    private const int MaxLoginOtpRequestsPerWindow = 5;

    public AuthService(AppDbContext db, ILogger<AuthService> logger, ILoginOtpDao loginOtpDao, ISmsSenderService smsSender, IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _loginOtpDao = loginOtpDao;
        _smsSender = smsSender;
        _webOtpDomain = configuration["WebOtp:Domain"] ?? "kaarigar.co";
    }

    /// <summary>
    /// Registers a new user.
    /// Creates: USER_ACCOUNT + EMPLOYER_PROFILE (if EMPLOYER) or EMPLOYEE_PROFILE + EMPLOYEE_SKILL rows (if EMPLOYEE).
    /// Account starts with IS_APPROVED_FL = 0 — Admin must approve before login is possible.
    /// </summary>
    public async Task<ServiceResult> RegisterAsync(RegisterModel model, string? ipAddress = null)
    {
        // ── Validate uniqueness ──────────────────────────────────────────────
        if (await _db.UserAccounts.AnyAsync(u => u.ContactNbr == model.ContactNbr))
            return new ServiceResult(false, "This mobile number is already registered. Please log in or use a different number.");

        if (!string.IsNullOrWhiteSpace(model.EmailId) &&
            await _db.UserAccounts.AnyAsync(u => u.EmailId == model.EmailId))
            return new ServiceResult(false, "This email address is already registered.");

        // ── Hash password ────────────────────────────────────────────────────
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 12);

        // ── Create USER_ACCOUNT ──────────────────────────────────────────────
        var user = new UserAccount
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName?.Trim(),
            ContactNbr = model.ContactNbr.Trim(),
            EmailId = string.IsNullOrWhiteSpace(model.EmailId) ? null : model.EmailId.Trim().ToLowerInvariant(),
            PasswordHashTxt = passwordHash,
            RoleCd = model.RoleCd,   // 'EMPLOYER' or 'EMPLOYEE'
            IsActiveFl = true,
            IsApprovedFl = false,           // Admin approval required
            IsMobileVerifiedFl = false,
            CreatedBy = "REGISTRATION_FORM",
            CreatedIpAddr = ipAddress,
            CreatedTs = DateTime.UtcNow,
        };

        _db.UserAccounts.Add(user);
        await _db.SaveChangesAsync(); // get UserAccountId

        // ── Create role-specific profile ─────────────────────────────────────
        if (model.RoleCd == "EMPLOYER")
        {
            var employerProfile = new EmployerProfile
            {
                UserAccountId = user.UserAccountId,
                CompanyName = model.CompanyName?.Trim(),
                BusinessCategoryId = model.BusinessCategoryId,
                CityName = model.CityName?.Trim(),
                AreaAddressTxt = model.AreaAddressTxt?.Trim(),
                CreatedBy = "REGISTRATION_FORM",
                CreatedIpAddr = ipAddress,
                CreatedTs = DateTime.UtcNow,
            };
            _db.EmployerProfiles.Add(employerProfile);
        }
        else if (model.RoleCd == "EMPLOYEE")
        {
            var employeeProfile = new EmployeeProfile
            {
                UserAccountId = user.UserAccountId,
                CityName = model.CityName?.Trim(),
                AreaAddressTxt = model.AreaAddressTxt?.Trim(),
                WorkCategoryId = model.WorkCategoryId,
                CreatedBy = "REGISTRATION_FORM",
                CreatedIpAddr = ipAddress,
                CreatedTs = DateTime.UtcNow,
            };
            _db.EmployeeProfiles.Add(employeeProfile);

            // ── EMPLOYEE_SKILL rows ──────────────────────────────────────────
            foreach (var skillId in model.SelectedSkillIds)
            {
                _db.EmployeeSkills.Add(new EmployeeSkill
                {
                    UserAccountId = user.UserAccountId,
                    SkillId = skillId,
                    CreatedBy = "REGISTRATION_FORM",
                    CreatedIpAddr = ipAddress,
                    CreatedTs = DateTime.UtcNow,
                });
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "New {Role} registered: {ContactNbr} (UserAccountId={Id}). Pending admin approval.",
            model.RoleCd, model.ContactNbr, user.UserAccountId);

        return new ServiceResult(true,
            "Registration successful. Your account is pending Admin approval.");
    }

    /// <summary>
    /// Authenticates a user. Returns the UserAccount on success so the MVC
    /// controller can build the ClaimsPrincipal and call HttpContext.SignInAsync().
    /// </summary>
    public async Task<(ServiceResult Result, UserAccount? User)> LoginAsync(LoginModel model)
    {
        var user = await _db.UserAccounts
            .FirstOrDefaultAsync(u => u.ContactNbr == model.ContactNbr);

        if (user == null)
            return (new ServiceResult(false, "No account found with this mobile number."), null);

        if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHashTxt))
            return (new ServiceResult(false, "Incorrect password. Please try again."), null);

        if (!user.IsActiveFl)
            return (new ServiceResult(false, "Your account has been deactivated. Contact support."), null);

        if (user.RoleCd == "ADMIN")
            return (new ServiceResult(false, "No account found with this mobile number."), null);

        //if (!user.IsApprovedFl)
        //    return (new ServiceResult(false, "Your account is pending Admin approval. You will be notified via SMS once approved."), null);

        // Update last login timestamp
        user.LastLoginTs = DateTime.UtcNow;
        user.UpdatedBy = "LOGIN";
        user.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (new ServiceResult(true, "Login successful."), user);
    }

    // ── OTP LOGIN ────────────────────────────────────────────────────────────

    /// <summary>Step 1: generate + "send" (see ISmsSenderService) a 4-digit OTP for login.</summary>
    public async Task<ServiceResult> RequestLoginOtpAsync(string contactNbr)
    {
        contactNbr = contactNbr.Trim();

        var user = await _loginOtpDao.GetUserByContactNbrAsync(contactNbr);
        if (user == null)
            return new ServiceResult(false, "No account found with this mobile number.");

        if (user.RoleCd == "ADMIN")
            return new ServiceResult(false, "No account found with this mobile number.");

        var recentCount = await _loginOtpDao.CountRecentOtpRequestsAsync(contactNbr, LoginOtpThrottleWindow);
        if (recentCount >= MaxLoginOtpRequestsPerWindow)
            return new ServiceResult(false, "Too many OTP requests. Please wait a few minutes and try again.");

        await _loginOtpDao.InvalidatePreviousOtpsAsync(contactNbr);

        var otpCd = GenerateLoginOtp();
        var otp = new MobileVerificationOtp
        {
            ContactNbr = contactNbr,
            OtpCd = otpCd,
            PurposeCd = "LOGIN",
            GeneratedTs = DateTime.UtcNow,
            ExpiresTs = DateTime.UtcNow.Add(LoginOtpValidity),
            IsUsedFl = false,
            CreatedBy = "LOGIN_OTP_REQUEST",
        };
        await _loginOtpDao.CreateOtpAsync(otp);

        try
        {
            await _smsSender.SendAsync(
                toContactNbr: contactNbr,
                // The trailing "@domain #code" line is required by the browser
                // WebOTP API (navigator.credentials.get) for Android Chrome to
                // auto-detect and offer to autofill this SMS's OTP. The domain
                // must exactly match the site's origin (no https://, no path).
                // iOS Safari's QuickType OTP suggestion also benefits from a
                // clearly-labelled numeric code and doesn't require this line,
                // but it doesn't hurt it either.
                message: $"Your Kaarigar login OTP is {otpCd}. Valid for 10 minutes. Do not share this with anyone.\n@{_webOtpDomain} #{otpCd}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send login OTP SMS to {ContactNbr}", contactNbr);
            return new ServiceResult(false, "We couldn't send the OTP right now. Please try again shortly.");
        }

        return new ServiceResult(true, "OTP sent to your mobile number.");
    }

    /// <summary>Step 2: verify the OTP and sign in — no password check.</summary>
    public async Task<(ServiceResult Result, UserAccount? User)> LoginWithOtpAsync(string contactNbr, string otpCd)
    {
        contactNbr = contactNbr.Trim();
        otpCd = (otpCd ?? string.Empty).Trim();

        var user = await _loginOtpDao.GetUserByContactNbrAsync(contactNbr);
        if (user == null)
            return (new ServiceResult(false, "No account found with this mobile number."), null);

        if (user.RoleCd == "ADMIN")
            return (new ServiceResult(false, "No account found with this mobile number."), null);

        var otp = await _loginOtpDao.GetLatestValidOtpAsync(contactNbr, otpCd);
        if (otp == null)
            return (new ServiceResult(false, "Invalid or expired OTP. Please request a new one."), null);

        if (!user.IsActiveFl)
            return (new ServiceResult(false, "Your account has been deactivated. Contact support."), null);

        await _loginOtpDao.MarkOtpUsedAsync(otp.MobileVerificationOtpId);

        user.LastLoginTs = DateTime.UtcNow;
        user.UpdatedBy = "LOGIN_OTP";
        user.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("OTP login: UserAccountId={Id}", user.UserAccountId);

        return (new ServiceResult(true, "Login successful."), user);
    }

    private static string GenerateLoginOtp()
    {
        // Cryptographically-random 4-digit OTP (0000–9999, zero-padded).
        return System.Security.Cryptography.RandomNumberGenerator
            .GetInt32(0, 10_000)
            .ToString("D4");
    }

    /// <summary>Screen A-01: Admin-only login — see IAuthService for the role restriction rationale.</summary>
    public async Task<(ServiceResult Result, UserAccount? User)> AdminLoginAsync(LoginModel model)
    {
        var user = await _db.UserAccounts
            .FirstOrDefaultAsync(u => u.ContactNbr == model.ContactNbr);

        // Deliberately generic message — don't reveal whether the mobile number
        // exists or exists-but-isn't-an-admin, to avoid helping anyone probe
        // for valid admin accounts.
        const string invalidCredentialsMessage = "Invalid admin credentials.";

        if (user == null || user.RoleCd != "ADMIN")
            return (new ServiceResult(false, invalidCredentialsMessage), null);

        if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHashTxt))
            return (new ServiceResult(false, invalidCredentialsMessage), null);

        if (!user.IsActiveFl)
            return (new ServiceResult(false, "This admin account has been deactivated."), null);

        user.LastLoginTs = DateTime.UtcNow;
        user.UpdatedBy = "ADMIN_LOGIN";
        user.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin login: UserAccountId={Id}", user.UserAccountId);

        return (new ServiceResult(true, "Login successful."), user);
    }
}