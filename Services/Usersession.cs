using Kaarigar.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Kaarigar.Services
{
    /// <summary>
    /// MVC helper that wraps cookie-based authentication.
    /// 
    /// In MVC there is no long-lived scoped service holding user state across
    /// requests — instead the signed-in user is carried by the cookie/ClaimsPrincipal.
    /// 
    /// Usage in a controller:
    ///   // Sign in
    ///   await UserSession.SignInAsync(HttpContext, user);
    ///
    ///   // Read current user
    ///   var session = UserSession.FromContext(HttpContext);
    ///   if (session.IsLoggedIn) { ... }
    ///
    ///   // Sign out
    ///   await UserSession.SignOutAsync(HttpContext);
    /// </summary>
    public class UserSession
    {
        // ── Claim type constants ─────────────────────────────────────────────────
        private const string ClaimId = "UserAccountId";
        private const string ClaimRole = ClaimTypes.Role;
        private const string ClaimName = ClaimTypes.Name;
        private const string ClaimContact = "ContactNbr";

        // ── Properties ───────────────────────────────────────────────────────────
        public bool IsLoggedIn { get; private set; }
        public int UserAccountId { get; private set; }
        public string? FullName { get; private set; }
        public string? RoleCd { get; private set; }
        public string? ContactNbr { get; private set; }

        public bool IsEmployer => RoleCd == "EMPLOYER";
        public bool IsEmployee => RoleCd == "EMPLOYEE";
        public bool IsAdmin => RoleCd == "ADMIN";

        // ── Factory: build from the current HttpContext ──────────────────────────
        /// <summary>
        /// Reads the current cookie principal and returns a populated UserSession.
        /// Returns an IsLoggedIn=false session if the user is not authenticated.
        /// </summary>
        public static UserSession FromContext(HttpContext context)
        {
            var principal = context.User;
            var session = new UserSession();

            if (principal?.Identity?.IsAuthenticated != true)
                return session;

            session.IsLoggedIn = true;
            session.FullName = principal.FindFirstValue(ClaimName);
            session.RoleCd = principal.FindFirstValue(ClaimRole);
            session.ContactNbr = principal.FindFirstValue(ClaimContact);

            if (int.TryParse(principal.FindFirstValue(ClaimId), out var id))
                session.UserAccountId = id;

            return session;
        }

        // ── Sign-in: write the cookie ────────────────────────────────────────────
        /// <summary>
        /// Creates the claims identity from a UserAccount and issues the auth cookie.
        /// Call this from your AccountController after a successful LoginAsync().
        /// </summary>
        public static async Task SignInAsync(HttpContext context, UserAccount user, bool rememberMe = false)
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            var claims = new List<Claim>
        {
            new Claim(ClaimId,      user.UserAccountId.ToString()),
            new Claim(ClaimName,    fullName),
            new Claim(ClaimRole,    user.RoleCd ?? string.Empty),
            new Claim(ClaimContact, user.ContactNbr ?? string.Empty),
        };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8),
            };

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties);
        }

        // ── Sign-out: clear the cookie ───────────────────────────────────────────
        /// <summary>
        /// Clears the auth cookie. Call from your AccountController logout action.
        /// </summary>
        public static async Task SignOutAsync(HttpContext context)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}