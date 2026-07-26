using Kaarigar.Models;
using Kaarigar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaarigar.Controllers;

/// <summary>
/// Handles all three dashboard routes.
/// Single-session enforcement: the cookie carries the UserAccountId;
/// the SessionGuardFilter (registered globally) rejects a second concurrent
/// session from a different device/browser by validating a rotating session
/// token stored in the DB against the one in the cookie.
/// </summary>
[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _svc;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService svc, ILogger<DashboardController> logger)
    {
        _svc    = svc;
        _logger = logger;
    }

    // ── POST-LOGIN ROUTER ─────────────────────────────────────────────────────

    /// <summary>
    /// Called immediately after login. Redirects to the correct dashboard
    /// based on the authenticated user's role claim.
    /// </summary>
    [HttpGet("/dashboard")]
    public IActionResult Index()
    {
        var session = UserSession.FromContext(HttpContext);

        return session.RoleCd switch
        {
            "EMPLOYER" => RedirectToAction(nameof(Employer)),
            "EMPLOYEE" => RedirectToAction(nameof(Employee)),
            "ADMIN"    => RedirectToAction(nameof(Admin)),
            _          => RedirectToAction("Login", "Account"),
        };
    }

    // ── EMPLOYER DASHBOARD (Screen E-01) ─────────────────────────────────────

    [HttpGet("/dashboard/employer")]
    [Authorize(Roles = "EMPLOYER")]
    public async Task<IActionResult> Employer()
    {
        var session = UserSession.FromContext(HttpContext);
        var vm      = await _svc.GetEmployerDashboardAsync(session.UserAccountId);
        vm.EmployerName = session.FullName ?? string.Empty;
        return View(vm);
    }

    // ── EMPLOYEE DASHBOARD (Screen W-01) ─────────────────────────────────────

    [HttpGet("/dashboard/employee")]
    [Authorize(Roles = "EMPLOYEE")]
    public async Task<IActionResult> Employee()
    {
        var session = UserSession.FromContext(HttpContext);
        var vm      = await _svc.GetEmployeeDashboardAsync(session.UserAccountId);
        vm.EmployeeName = session.FullName ?? string.Empty;
        return View(vm);
    }

    // ── ADMIN DASHBOARD (Screen A-02) ────────────────────────────────────────

    [HttpGet("/dashboard/admin")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Admin()
    {
        var vm = await _svc.GetAdminDashboardAsync();
        return View(vm);
    }
}
