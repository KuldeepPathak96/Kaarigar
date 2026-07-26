using Kaarigar.Data;
using Kaarigar.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add MVC Services
builder.Services.AddControllersWithViews();

// Set up Cookie Authentication to replace Blazor's UserSession state management
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;

        // Hardening: block JS access to the auth cookie, require HTTPS in
        // production, and restrict cross-site sending to mitigate CSRF/XSS
        // cookie theft even though [ValidateAntiForgeryToken] already covers
        // CSRF on state-changing POSTs.
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Configure SQL Server Database Context
IServiceCollection dbContextServices = builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
    ));

// DI Services Activation
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDashboardDao, DashboardDao>();
builder.Services.AddScoped<IPasswordResetDao, PasswordResetDao>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IEmailSenderService, EmailSenderService>();
builder.Services.AddScoped<IEmployerProfileDao, EmployerProfileDao>();
builder.Services.AddScoped<IEmployerProfileService, EmployerProfileService>();
builder.Services.AddScoped<IBusinessCategoryDao, BusinessCategoryDao>();
builder.Services.AddScoped<IBusinessCategoryService, BusinessCategoryService>();
builder.Services.AddScoped<IAdminSkillDao, AdminSkillDao>();
builder.Services.AddScoped<IAdminSkillService, AdminSkillService>();
builder.Services.AddScoped<IAdminLocationDao, AdminLocationDao>();
builder.Services.AddScoped<IAdminLocationService, AdminLocationService>();
builder.Services.AddScoped<IHourlyRateOptionDao, HourlyRateOptionDao>();
builder.Services.AddScoped<IHourlyRateOptionService, HourlyRateOptionService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<ILearningVideoDao, LearningVideoDao>();
builder.Services.AddScoped<ILearningVideoService, LearningVideoService>();
builder.Services.AddHttpClient<IGeocodingService, GoogleGeocodingService>();
builder.Services.AddScoped<IJobPostDao, JobPostDao>();
builder.Services.AddScoped<IJobPostService, JobPostService>();
builder.Services.AddScoped<IJobApplicantDao, JobApplicantDao>();
builder.Services.AddScoped<IJobApplicantService, JobApplicantService>();
builder.Services.AddScoped<IAdminEmployerDao, AdminEmployerDao>();
builder.Services.AddScoped<IAdminEmployerService, AdminEmployerService>();
builder.Services.AddScoped<IAdminEmployeeDao, AdminEmployeeDao>();
builder.Services.AddScoped<IAdminEmployeeService, AdminEmployeeService>();
builder.Services.AddScoped<IAdminJobPostDao, AdminJobPostDao>();
builder.Services.AddScoped<IAdminJobPostService, AdminJobPostService>();
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsAppSettings"));
builder.Services.AddHttpClient("WhatsAppCloudApi");
builder.Services.AddScoped<IWhatsAppNotificationService, WhatsAppNotificationService>();
builder.Services.AddScoped<IAdminNotificationLogDao, AdminNotificationLogDao>();
builder.Services.AddScoped<IAdminNotificationLogService, AdminNotificationLogService>();
builder.Services.AddScoped<IEmployeeJobDao, EmployeeJobDao>();
builder.Services.AddScoped<IEmployeeJobService, EmployeeJobService>();
builder.Services.AddScoped<IEmployeeNotificationDao, EmployeeNotificationDao>();
builder.Services.AddScoped<IEmployeeNotificationService, EmployeeNotificationService>();
builder.Services.AddScoped<IEmployeeProfileDao, EmployeeProfileDao>();
builder.Services.AddScoped<IEmployeeProfileService, EmployeeProfileService>();
builder.Services.AddScoped<ILocationDao, LocationDao>();

// Background WhatsApp job-match notification dispatch (see IJobNotificationQueue).
builder.Services.AddSingleton<IJobNotificationQueue, JobNotificationQueue>();
builder.Services.AddHostedService<JobNotificationBackgroundService>();


builder.Services.AddHttpContextAccessor();

// Brute-force mitigation: throttle repeated hits to login/OTP endpoints per
// client IP. This is on top of (not instead of) the existing per-account OTP
// request throttling in PasswordResetService.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AuthEndpoints", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Baseline security response headers (defense in depth alongside
// [ValidateAntiForgeryToken], BCrypt hashing, and parameterized EF queries).
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseRouting();

app.UseRateLimiter();

// Authentication always executes before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();