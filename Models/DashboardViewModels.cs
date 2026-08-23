using System;
using System.Collections.Generic;

namespace Kaarigar.Models;

// ── SHARED ───────────────────────────────────────────────────────────────────

// NOTE: ActivityFeedItem was used by the Employer dashboard's old "Recent
// Applicant Activity" feed, which has been removed in favour of the
// per-KPI-card expanded detail lists below (LiveShiftItem /
// ApplicationDetailItem / FilledPositionItem). Left here in case another
// screen picks it up later; currently unused.
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

    // Expanded detail lists — one per KPI card except Total Shift Posted,
    // which redirects to /job/my-jobs instead of expanding in place.
    public List<LiveShiftItem> LiveShifts               { get; set; } = new();
    public List<ApplicationDetailItem> RecentApplications { get; set; } = new();
    public List<FilledPositionItem> RecentlyFilled       { get; set; } = new();

    // Greeting
    public string EmployerName         { get; set; } = string.Empty;
    public string? CompanyName         { get; set; }

    /// <summary>True once Admin has approved this employer's account. Gates the "Post New Job" actions.</summary>
    public bool IsApproved             { get; set; }
}

/// <summary>One row in the expanded "Live Shift Status" card — one ACTIVE job post with its fill progress.</summary>
public class LiveShiftItem
{
    public int JobPostId          { get; set; }
    public string JobTitle        { get; set; } = string.Empty;
    public int ConfirmedWorkerNbr { get; set; }
    public int RequiredWorkerNbr  { get; set; }
    public DateTime? StartDateTime { get; set; }

    public int StillNeededNbr => Math.Max(0, RequiredWorkerNbr - ConfirmedWorkerNbr);
    public bool IsFullyStaffed => StillNeededNbr == 0;

    /// <summary>Human-readable "starts in 42 min" / "starts in 3 hr" / "Started" / "Today, 6:00 PM", computed once at render time.</summary>
    public string StartsInText
    {
        get
        {
            if (StartDateTime is null) return "No start time set";
            var diff = StartDateTime.Value - DateTime.UtcNow;
            if (diff.TotalMinutes <= 0) return "Shift has started";
            if (diff.TotalMinutes < 60) return $"Shift starts in {(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24) return $"Shift starts in {(int)diff.TotalHours} hr {(int)diff.Minutes} min";
            return $"Shift starts {StartDateTime.Value:ddd, d MMM 'at' h:mm tt}";
        }
    }
}

/// <summary>One row in the expanded "Worker Applications" card.</summary>
public class ApplicationDetailItem
{
    public int JobApplicationId  { get; set; }
    public string EmployeeName   { get; set; } = string.Empty;
    public string JobTitle       { get; set; } = string.Empty;
    public string StatusCd       { get; set; } = string.Empty;
    public DateTime AppliedTs    { get; set; }
}

/// <summary>One row in the expanded "Positions Filled" card — a CLOSED job post.</summary>
public class FilledPositionItem
{
    public int JobPostId         { get; set; }
    public string JobTitle       { get; set; } = string.Empty;
    public int RequiredWorkerNbr { get; set; }
    public int ConfirmedWorkerNbr { get; set; }
    public DateTime? FilledTs    { get; set; }
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
