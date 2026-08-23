using Kaarigar.Data;

namespace Kaarigar.Services;

/// <summary>
/// Drains IJobNotificationQueue and performs the actual employee matching +
/// WhatsApp send outside the employer's "Post Job" request/response cycle,
/// so posting a job stays fast regardless of how many employees match.
///
/// Each queued JobPostId gets its own DI scope (AppDbContext is scoped and
/// not thread-safe / not safe to reuse across the request that queued it),
/// so we resolve IJobPostDao / IWhatsAppNotificationService fresh per item.
/// </summary>
public class JobNotificationBackgroundService : BackgroundService
{
    private readonly IJobNotificationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobNotificationBackgroundService> _logger;

    /// <summary>How long "Repeat Staff" workers get first, before the shift opens up to everyone else.</summary>
    private static readonly TimeSpan RepeatStaffPriorityWindow = TimeSpan.FromMinutes(15);

    public JobNotificationBackgroundService(
        IJobNotificationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<JobNotificationBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobPostId in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var jobPostDao = scope.ServiceProvider.GetRequiredService<IJobPostDao>();
                var notificationService = scope.ServiceProvider.GetRequiredService<IWhatsAppNotificationService>();

                var jobPost = await jobPostDao.GetJobPostByIdAsync(jobPostId);
                if (jobPost == null)
                {
                    _logger.LogWarning("Queued JobPostId={Id} no longer exists — skipping notification.", jobPostId);
                    continue;
                }

                if (jobPost.IsRepeatStaffPriorityFl && jobPost.RepeatedFromJobPostId.HasValue)
                {
                    await HandleRepeatStaffPriorityAsync(jobPostId, jobPost.RepeatedFromJobPostId.Value, jobPostDao, notificationService, stoppingToken);
                }
                else
                {
                    var matches = await jobPostDao.FindMatchingEmployeesAsync(jobPost);
                    await notificationService.NotifyMatchingEmployeesAsync(jobPost, matches);
                }
            }
            catch (Exception ex)
            {
                // Never let one bad job post crash the worker loop — log and keep draining.
                _logger.LogError(ex, "Failed to send queued job-match notifications for JobPostId={Id}", jobPostId);
            }
        }
    }

    /// <summary>
    /// "Repeat Shift" + "Repeat Staff" checked: notify the workers who
    /// completed the original job FIRST (fast — this runs inline, so the
    /// main drain loop barely pauses). Then hands the 15-minute wait-and-
    /// maybe-broadcast-wider step off to a detached background task so this
    /// one repeat job's priority window doesn't block every other
    /// employer's notifications from being sent in the meantime.
    /// </summary>
    private async Task HandleRepeatStaffPriorityAsync(
        int jobPostId, int originalJobPostId,
        IJobPostDao jobPostDao, IWhatsAppNotificationService notificationService,
        CancellationToken stoppingToken)
    {
        var priorityWorkers = await jobPostDao.GetCompletedWorkersAsync(originalJobPostId);

        if (priorityWorkers.Count == 0)
        {
            // Nothing to prioritize (e.g. the original job never had anyone
            // mark COMPLETED) — fall back to the normal immediate broadcast.
            var jobPost = await jobPostDao.GetJobPostByIdAsync(jobPostId);
            if (jobPost == null) return;
            var matches = await jobPostDao.FindMatchingEmployeesAsync(jobPost);
            await notificationService.NotifyMatchingEmployeesAsync(jobPost, matches);
            return;
        }

        var priorityJobPost = await jobPostDao.GetJobPostByIdAsync(jobPostId);
        if (priorityJobPost == null) return;

        await notificationService.NotifyMatchingEmployeesAsync(priorityJobPost, priorityWorkers);

        _logger.LogInformation(
            "Repeat Staff: notified {Count} worker(s) who completed JobPostId={OriginalId} first, for new JobPostId={NewId}. Broadcasting wider in {Minutes} min if not fully filled.",
            priorityWorkers.Count, originalJobPostId, jobPostId, RepeatStaffPriorityWindow.TotalMinutes);

        var priorityWorkerIds = priorityWorkers.Select(w => w.UserAccountId).ToHashSet();

        // Detached on purpose: awaiting this here would stall the shared
        // drain loop for 15 minutes for every other employer's job posts.
        // Uses its own DI scope since the outer "using var scope" in
        // ExecuteAsync will already be disposed by the time this runs.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(RepeatStaffPriorityWindow, stoppingToken);

                using var delayedScope = _scopeFactory.CreateScope();
                var delayedJobPostDao = delayedScope.ServiceProvider.GetRequiredService<IJobPostDao>();
                var delayedNotificationService = delayedScope.ServiceProvider.GetRequiredService<IWhatsAppNotificationService>();

                var jobPost = await delayedJobPostDao.GetJobPostByIdAsync(jobPostId);
                if (jobPost == null) return; // job was deleted in the meantime

                var currentApplicantCount = await delayedJobPostDao.GetDistinctApplicantCountAsync(jobPostId);
                if (currentApplicantCount >= jobPost.RequiredWorkerNbr)
                {
                    _logger.LogInformation(
                        "Repeat Staff priority window elapsed for JobPostId={Id}: already has {Count}/{Required} applicants — no wider broadcast needed.",
                        jobPostId, currentApplicantCount, jobPost.RequiredWorkerNbr);
                    return;
                }

                var wideMatches = await delayedJobPostDao.FindMatchingEmployeesAsync(jobPost);
                var stillToNotify = wideMatches.Where(u => !priorityWorkerIds.Contains(u.UserAccountId)).ToList();

                if (stillToNotify.Count > 0)
                {
                    await delayedNotificationService.NotifyMatchingEmployeesAsync(jobPost, stillToNotify);
                }

                _logger.LogInformation(
                    "Repeat Staff priority window elapsed for JobPostId={Id}: still {Count}/{Required} filled — broadcast to {WiderCount} more worker(s).",
                    jobPostId, currentApplicantCount, jobPost.RequiredWorkerNbr, stillToNotify.Count);
            }
            catch (OperationCanceledException)
            {
                // App shutting down mid-wait — acceptable, same trade-off as
                // the rest of this in-memory (non-durable) notification queue.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed during delayed wider-broadcast for repeat-staff JobPostId={Id}", jobPostId);
            }
        }, CancellationToken.None); // CancellationToken.None: don't let stoppingToken abort NotifyMatchingEmployeesAsync mid-send once the delay itself has already completed
    }
}
