using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data Access Object interface for Screens E-03 (Post a Job) and E-04
/// (My Job Posts) — both operate on JOB_POST/JOB_SKILL, so they share one DAO.
/// </summary>
public interface IJobPostDao
{
    /// <summary>Active skills for the "Required Skills" multi-select.</summary>
    Task<List<Skill>> GetActiveSkillsAsync();

    Task<JobPost> CreateJobPostAsync(JobPost jobPost, List<int> skillIds);

    /// <summary>Loads a job post for editing, only if it belongs to the given employer.</summary>
    Task<JobPost?> GetJobPostForEmployerAsync(int jobPostId, int employerUserAccountId);

    /// <summary>Loads a job post by id regardless of owner — used by the background notification worker, which runs outside any employer's request scope.</summary>
    Task<JobPost?> GetJobPostByIdAsync(int jobPostId);

    /// <summary>Replaces the job's required-skill rows and updates its other fields.</summary>
    Task UpdateJobPostAsync(JobPost jobPost, List<int> skillIds);

    Task UpdateStatusAsync(int jobPostId, int employerUserAccountId, string statusCd);

    /// <summary>Screen E-04: all job posts for this employer, newest first, with application counts.</summary>
    Task<List<JobPostListItemViewModel>> GetMyJobPostsAsync(int employerUserAccountId);

    /// <summary>
    /// Finds employees who are Admin-approved, active, haven't opted out of
    /// job alerts, whose skills match the job's required skills (or all
    /// employees if none are required), and whose location falls within
    /// their own preferred radius of the job (Haversine distance on saved
    /// Lat/Lng, falling back to a city-name substring match when either
    /// side is missing coordinates).
    /// </summary>
    Task<List<UserAccount>> FindMatchingEmployeesAsync(JobPost jobPost);
}
