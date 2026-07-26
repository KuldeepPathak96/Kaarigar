using Kaarigar.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Services;

/// <summary>
/// Authenticated-request guard filter.
///
/// NOTE: Single-session enforcement (comparing a rotating session token
/// stored on UserAccount against the one in the auth cookie claims) is
/// NOT implemented — UserAccount has no ActiveSessionToken (or equivalent)
/// column in the current schema. That comparison has been removed rather
/// than invented, per project convention of not adding unmapped properties.
///
/// This filter now simply verifies that authenticated requests carry the
/// expected identity claims (UserAccountId), and invalidates/signs out any
/// session that is authenticated but missing those claims.
///
/// Register in Program.cs:
///   builder.Services.AddScoped<SessionGuardFilter>();
///   builder.Services.AddControllersWithViews(opt =>
///       opt.Filters.Add<SessionGuardFilter>());
/// </summary>
public class SessionGuardFilter : IAsyncActionFilter
{
    private readonly AppDbContext _db;

    public SessionGuardFilter(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        // Only enforce on authenticated users
        if (user?.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var accountIdStr = user.FindFirst("UserAccountId")?.Value;

        if (!int.TryParse(accountIdStr, out var accountId))
        {
            await InvalidateAsync(context);
            return;
        }

        // Confirm the account still exists and is active.
        var isActive = await _db.UserAccounts
            .AsNoTracking()
            .Where(u => u.UserAccountId == accountId)
            .Select(u => u.IsActiveFl)
            .FirstOrDefaultAsync();

        if (!isActive)
        {
            await InvalidateAsync(context, "Your account is no longer active. Please contact support.");
            return;
        }

        await next();
    }

    private static async Task InvalidateAsync(ActionExecutingContext ctx, string? message = null)
    {
        await UserSession.SignOutAsync(ctx.HttpContext);

        if (message != null)
            ctx.HttpContext.Session.SetString("SessionKicked", message);

        ctx.Result = new RedirectToActionResult("Login", "Account", null);
    }
}