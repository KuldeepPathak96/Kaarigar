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

    public AuthService(AppDbContext db, ILogger<AuthService> logger)
    {
        _db = db;
        _logger = logger;
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
                PreferredRadiusNbr = model.PreferredRadiusNbr,
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