using Kaarigar.Services;
using Kaarigar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kaarigar.Controllers
{
    [EnableRateLimiting("AuthEndpoints")]
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly IEmployerProfileService _employerProfileService;
        private readonly IEmployeeProfileService _employeeProfileService;
        private readonly IGeocodingService _geocodingService;

        public AccountController(
            IAuthService authService,
            IPasswordResetService passwordResetService,
            IEmployerProfileService employerProfileService,
            IEmployeeProfileService employeeProfileService,
            IGeocodingService geocodingService)
        {
            _authService = authService;
            _passwordResetService = passwordResetService;
            _employerProfileService = employerProfileService;
            _employeeProfileService = employeeProfileService;
            _geocodingService = geocodingService;

        }

        // ── LOGOUT ────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await UserSession.SignOutAsync(HttpContext);
            return RedirectToAction(nameof(Login));
        }

        // ── LOGIN ─────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var loginModel = new LoginModel
            {
                ContactNbr = vm.ContactNbr,
                Password = vm.Password
            };

            var (result, user) = await _authService.LoginAsync(loginModel);

            if (result.Success && user != null)
            {
                await UserSession.SignInAsync(HttpContext, user);
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ErrorMessage = result.Message;
            return View(vm);
        }

        // ── ADMIN LOGIN (Screen A-01) ────────────────────────────────────────
        // Deliberately separate from the public /account/login flow above:
        // not linked from any public page/nav, uses AdminLoginAsync (which
        // rejects non-ADMIN accounts even with a correct password), and on
        // success always lands on /dashboard/admin rather than the generic
        // role router.

        [HttpGet("/admin/login")]
        public IActionResult AdminLogin()
        {
            return View(new LoginModel());
        }

        [HttpPost("/admin/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(LoginModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var (result, user) = await _authService.AdminLoginAsync(vm);

            if (result.Success && user != null)
            {
                await UserSession.SignInAsync(HttpContext, user);
                return RedirectToAction("Admin", "Dashboard");
            }

            ViewBag.ErrorMessage = result.Message;
            return View(vm);
        }

        // ── REGISTER ──────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Register([FromQuery] string? role)
        {
            var currentRole = role?.ToUpper() == "EMPLOYEE" ? "EMPLOYEE" : "EMPLOYER";
            ViewBag.CurrentRole = currentRole;

            return View(new RegisterModel { RoleCd = currentRole });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel vm)
        {
            if (vm.RoleCd == "EMPLOYER")
            {
                ModelState.Remove(nameof(vm.PreferredRadiusNbr));
                ModelState.Remove(nameof(vm.DocumentSubtypeCd));
                ModelState.Remove(nameof(vm.IdLastFourDigitTxt));
            }
            else
            {
                ModelState.Remove(nameof(vm.CompanyName));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CurrentRole = vm.RoleCd ?? "EMPLOYER";
                return View(vm);
            }

            var registerModel = new RegisterModel
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                ContactNbr = vm.ContactNbr,
                EmailId = vm.EmailId,
                RoleCd = vm.RoleCd,
                Password = vm.Password,
                ConfirmPassword = vm.ConfirmPassword,
                CompanyName = vm.CompanyName,
                CityName = vm.CityName,
                AreaAddressTxt = vm.AreaAddressTxt,
                PreferredRadiusNbr = vm.PreferredRadiusNbr,
                SelectedSkillIds = vm.SelectedSkillIds,
                DocumentSubtypeCd = vm.DocumentSubtypeCd,
                IdLastFourDigitTxt = vm.IdLastFourDigitTxt,
                AgreeToTerms = vm.AgreeToTerms
            };

            var result = await _authService.RegisterAsync(registerModel);

            if (result.Success)
            {
                ViewBag.Registered = true;
                ViewBag.CurrentRole = vm.RoleCd;
                ViewBag.SubmittedPhone = vm.ContactNbr;
                return View(vm);
            }

            ViewBag.ErrorMessage = result.Message;
            ViewBag.CurrentRole = vm.RoleCd ?? "EMPLOYER";
            return View(vm);
        }

        // ── FORGOT PASSWORD — Step 1: enter registered email ────────────────────

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var result = await _passwordResetService.RequestOtpAsync(vm);

            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                return View(vm);
            }

            TempData["InfoMessage"] = result.Message;
            TempData["ResetEmail"] = vm.EmailId.Trim().ToLowerInvariant();

            return RedirectToAction(nameof(VerifyOtp));
        }

        // ── FORGOT PASSWORD — Step 2: enter OTP received by email ───────────────

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData.Peek("ResetEmail") as string;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction(nameof(ForgotPassword));

            if (TempData["InfoMessage"] is string info)
                ViewBag.InfoMessage = info;

            return View(new VerifyOtpModel { EmailId = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var result = await _passwordResetService.VerifyOtpAsync(vm);

            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                return View(vm);
            }

            TempData["ResetEmail"] = vm.EmailId.Trim().ToLowerInvariant();
            TempData["ResetOtp"] = vm.OtpCd.Trim();

            return RedirectToAction(nameof(ResetPassword));
        }

        // ── FORGOT PASSWORD — Step 3: set new password ───────────────────────────

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = TempData.Peek("ResetEmail") as string;
            var otp = TempData.Peek("ResetOtp") as string;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
                return RedirectToAction(nameof(ForgotPassword));

            return View(new ResetPasswordModel { EmailId = email, OtpCd = otp });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var result = await _passwordResetService.ResetPasswordAsync(vm);

            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                return View(vm);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Login));
        }

        // ── EMPLOYER PROFILE (Screen E-02) ───────────────────────────────────────

        [HttpGet("/account/profile")]
        [Authorize(Roles = "EMPLOYER,EMPLOYEE")]
        public async Task<IActionResult> Profile()
        {
            var session = UserSession.FromContext(HttpContext);

            if (session.IsEmployee)
            {
                var employeeVm = await _employeeProfileService.GetProfileAsync(session.UserAccountId);
                if (employeeVm == null)
                    return RedirectToAction(nameof(Login));

                return View("EmployeeProfile", employeeVm);
            }

            var vm = await _employerProfileService.GetProfileAsync(session.UserAccountId);

            if (vm == null)
                return RedirectToAction(nameof(Login));

            return View(vm);
        }

        [HttpPost("/account/profile")]
        [Authorize(Roles = "EMPLOYER")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EmployerProfileViewModel vm)
        {
            var session = UserSession.FromContext(HttpContext);

            if (!ModelState.IsValid)
            {
                await RepopulateDropdownAsync(vm);
                return View(vm);
            }

            var result = await _employerProfileService.UpdateProfileAsync(
                session.UserAccountId, vm, HttpContext.Connection.RemoteIpAddress?.ToString());

            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                await RepopulateDropdownAsync(vm);
                return View(vm);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Profile));
        }

        // ── EMPLOYEE PROFILE (Screen W-02) ────────────────────────────────────
        // Separate POST route from the Employer one above (both GETs share
        // /account/profile and branch by role instead — see Profile() above).

        [HttpPost("/account/employee-profile")]
        [Authorize(Roles = "EMPLOYEE")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeProfile(EmployeeProfileViewModel vm)
        {
            var session = UserSession.FromContext(HttpContext);

            if (!ModelState.IsValid)
            {
                await RepopulateEmployeeOptionsAsync(vm);
                return View("EmployeeProfile", vm);
            }

            var result = await _employeeProfileService.UpdateProfileAsync(
                session.UserAccountId, vm, HttpContext.Connection.RemoteIpAddress?.ToString());

            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                await RepopulateEmployeeOptionsAsync(vm);
                return View("EmployeeProfile", vm);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Profile));
        }

        private async Task RepopulateEmployeeOptionsAsync(EmployeeProfileViewModel vm)
        {
            // On a validation failure we still need the skill list and
            // existing file info re-populated, since the posted form doesn't
            // carry them.
            var session = UserSession.FromContext(HttpContext);
            var fresh = await _employeeProfileService.GetProfileAsync(session.UserAccountId);
            if (fresh == null) return;

            vm.SkillOptions = fresh.SkillOptions;
            vm.ExistingIdProofFileName = fresh.ExistingIdProofFileName;
            vm.IdProofReviewStatusCd = fresh.IdProofReviewStatusCd;
            vm.ExistingCvFileName = fresh.ExistingCvFileName;
            vm.CvReviewStatusCd = fresh.CvReviewStatusCd;
            vm.ProfileCompletenessPercent = fresh.ProfileCompletenessPercent;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private async Task RepopulateDropdownAsync(EmployerProfileViewModel vm)
        {
            // On a validation failure we still need the dropdown options and
            // existing file info re-populated, since the posted form doesn't
            // carry them.
            var session = UserSession.FromContext(HttpContext);
            var fresh = await _employerProfileService.GetProfileAsync(session.UserAccountId);
            if (fresh == null) return;

            vm.BusinessCategoryOptions = fresh.BusinessCategoryOptions;
            vm.ExistingLogoPath = fresh.ExistingLogoPath;
            vm.ExistingProofFileName = fresh.ExistingProofFileName;
            vm.ProofReviewStatusCd = fresh.ProofReviewStatusCd;
        }

        [HttpGet("/account/profile/reverse-geocode")]
        [Authorize(Roles = "EMPLOYER,EMPLOYEE")]
        public async Task<IActionResult> ReverseGeocode(decimal lat, decimal lng)
        {
            if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
                return BadRequest(new { message = "Invalid coordinates." });

            var result = await _geocodingService.ReverseGeocodeAsync(lat, lng);

            // Even if Google can't resolve a city name, the lat/lng itself was
            // already captured client-side and will still be saved with the
            // profile — city lookup failing is not a hard failure here.
            return Json(new { cityName = result?.CityName });
        }

    }
}
