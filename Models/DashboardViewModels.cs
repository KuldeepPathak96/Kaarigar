using System;
using System.Collections.Generic;

namespace Kaarigar.Models;

// ── SHARED ───────────────────────────────────────────────────────────────────

public class ActivityFeedItem
{
    public string EmployeeName  { get; set; } = string.Empty;
    public string JobTitle      { get; set; } = string.Empty;
    public string Action        { get; set; } = string.Empty; // "expressed interest", "OTP verified", etc.
    public DateTime Timestamp   { get; set; }
    public string AvatarInitial { get; set; } = string.Empty;
    public string StatusCd      { get; set; } = string.Empty; // PENDING | VIEWED | CONTACTED | VERIFIED
}

// ── EMPLOYER DASHBOARD (Screen E-01) ─────────────────────────────────────────

public class EmployerDashboardViewModel
{
    // KPI cards
    public int TotalJobsPosted         { get; set; }
    public int ActiveJobs              { get; set; }
    public int TotalApplications       { get; set; }
    public int JobsFilled              { get; set; }

    // Activity feed — last 5 applicant actions
    public List<ActivityFeedItem> RecentActivity { get; set; } = new();

    // Greeting
    public string EmployerName         { get; set; } = string.Empty;
    public string? CompanyName         { get; set; }

    /// <summary>True once Admin has approved this employer's account. Gates the "Post New Job" actions.</summary>
    public bool IsApproved             { get; set; }
}

// ── EMPLOYEE DASHBOARD (Screen W-01) ─────────────────────────────────────────

public class EmployeeDashboardViewModel
{
    // KPI cards
    public int ApplicationsSent        { get; set; }
    public int ContactsReceived        { get; set; }
    public int JobsCompleted           { get; set; }

    // Notification bell
    public int UnreadNotifications     { get; set; }
    public List<string> NotificationSummary { get; set; } = new();

    // Greeting
    public string EmployeeName         { get; set; } = string.Empty;

    /// <summary>True once Admin has approved this employee's account. Gates job-related actions once that flow exists.</summary>
    public bool IsApproved             { get; set; }
}

// ── ADMIN DASHBOARD (Screen A-02) ────────────────────────────────────────────

public class WeeklyJobData
{
    public string WeekLabel   { get; set; } = string.Empty; // e.g. "Jun 9–15"
    public int    JobsPosted  { get; set; }
}

public class AdminDashboardViewModel
{
    // KPI cards
    public int TotalEmployers          { get; set; }
    public int TotalEmployees          { get; set; }
    public int TotalJobsPosted         { get; set; }
    public int ActiveJobs              { get; set; }
    public int OtpsGeneratedToday      { get; set; }

    // Bar chart — last 4 weeks
    public List<WeeklyJobData> WeeklyJobChart { get; set; } = new();
}
