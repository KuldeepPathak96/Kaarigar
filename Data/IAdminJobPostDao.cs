using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>Data Access Object interface for Screen A-05 (Manage Job Posts).</summary>
public interface IAdminJobPostDao
{
    /// <summary>
    /// Searches/filters job posts. All filters are optional and combine with AND.
    /// search matches JobTitle or Employer Name (case-insensitive, partial).
    /// city matches the employer's EMPLOYER_PROFILE.CITY_NAME exactly.
    /// status is "ACTIVE" | "PAUSED" | "CLOSED" | null (= no status filter).
    /// fromDate/toDate filter on JOB_POST.CREATED_TS (inclusive), either may be null.
    /// </summary>
    Task<List<AdminJobPostListItemViewModel>> SearchJobPostsAsync(
        string? search, string? city, string? status, DateTime? fromDate, DateTime? toDate);

    /// <summary>Distinct, non-null employer city names, for the city filter dropdown.</summary>
    Task<List<string>> GetDistinctCitiesAsync();

    Task<AdminJobPostDetailViewModel?> GetJobPostDetailAsync(int jobPostId);

    /// <summary>Sets JOB_POST.STATUS_CD to 'CLOSED' — the Admin "Close Job" action. Skips the employer-ownership check that the Employer-side ToggleStatus uses, since Admin can act on any job.</summary>
    Task CloseJobPostAsync(int jobPostId);

    /// <summary>How many JOB_APPLICATION rows exist for this job (delete is blocked if &gt; 0, since FK_JOB_APPLICATION_JOB_POST has no cascade).</summary>
    Task<int> GetApplicantsCountAsync(int jobPostId);

    /// <summary>Hard-deletes the JOB_POST row (and its JOB_SKILL rows, via ON DELETE CASCADE). Caller must have already checked GetApplicantsCountAsync == 0.</summary>
    Task DeleteJobPostAsync(int jobPostId);
}
