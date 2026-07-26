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

                var matches = await jobPostDao.FindMatchingEmployeesAsync(jobPost);
                await notificationService.NotifyMatchingEmployeesAsync(jobPost, matches);
            }
            catch (Exception ex)
            {
                // Never let one bad job post crash the worker loop — log and keep draining.
                _logger.LogError(ex, "Failed to send queued job-match notifications for JobPostId={Id}", jobPostId);
            }
        }
    }
}
