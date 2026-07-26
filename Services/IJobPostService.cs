using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IJobPostService
{
    /// <summary>Builds a blank PostJobViewModel pre-populated with dropdown/multi-select options and the employer's saved contact number.</summary>
    Task<PostJobViewModel> GetNewJobFormAsync(int employerUserAccountId);

    /// <summary>Loads an existing job post for editing (options included), only if it belongs to this employer.</summary>
    Task<PostJobViewModel?> GetJobForEditAsync(int jobPostId, int employerUserAccountId);

    /// <summary>Creates a new job post and triggers WhatsApp notifications to matching employees.</summary>
    Task<ServiceResult> CreateJobAsync(int employerUserAccountId, PostJobViewModel model, string? ipAddress = null);

    Task<ServiceResult> UpdateJobAsync(int employerUserAccountId, PostJobViewModel model, string? ipAddress = null);

    /// <summary>Toggles a job's status between ACTIVE and PAUSED (Screen E-03's post-submit toggle, and E-04's quick action).</summary>
    Task<ServiceResult> ToggleStatusAsync(int jobPostId, int employerUserAccountId, string newStatusCd);

    Task<List<JobPostListItemViewModel>> GetMyJobPostsAsync(int employerUserAccountId);

    /// <summary>
    /// True only once Admin has approved this employer's account (USER_ACCOUNT.IS_APPROVED_FL).
    /// Until then, the employer cannot post jobs or view their job posts.
    /// </summary>
    Task<bool> IsEmployerApprovedAsync(int employerUserAccountId);
}
