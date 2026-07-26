using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>Data Access Object interface for Screen A-03 (Manage Employers).</summary>
public interface IAdminEmployerDao
{
    /// <summary>
    /// Searches/filters employers. All filters are optional and combine with AND.
    /// search matches Name, ContactNbr, or EmailId (case-insensitive, partial).
    /// status is "ACTIVE" | "BLOCKED" | null (= no status filter).
    /// </summary>
    Task<List<EmployerListItemViewModel>> SearchEmployersAsync(string? search, string? city, string? status);

    /// <summary>Distinct, non-null employer city names, for the city filter dropdown.</summary>
    Task<List<string>> GetDistinctCitiesAsync();

    Task<EmployerDetailViewModel?> GetEmployerDetailAsync(int userAccountId);

    /// <summary>Sets USER_ACCOUNT.IS_ACTIVE_FL — the Block/Unblock action.</summary>
    Task SetActiveStatusAsync(int userAccountId, bool isActive);

    /// <summary>
    /// Sets USER_ACCOUNT.IS_APPROVED_FL — the Admin Approve/Revoke Approval action.
    /// Until this is true, the employer cannot post jobs or view their job posts
    /// (see IJobPostService.IsEmployerApprovedAsync, enforced in JobController).
    /// </summary>
    Task SetApprovedStatusAsync(int userAccountId, bool isApproved);

    /// <summary>How many JOB_POST rows this employer owns (delete is blocked if &gt; 0, since FK_JOB_POST_EMPLOYER has no cascade).</summary>
    Task<int> GetJobsPostedCountAsync(int userAccountId);

    /// <summary>Hard-deletes the USER_ACCOUNT row (and its EMPLOYER_PROFILE, via ON DELETE CASCADE). Caller must have already checked GetJobsPostedCountAsync == 0.</summary>
    Task DeleteEmployerAsync(int userAccountId);

    /// <summary>Sets EMPLOYER_PROFILE.BUSINESS_PROOF_REVIEW_STATUS_CD to APPROVED/REJECTED. Returns false if the employer profile doesn't exist.</summary>
    Task<bool> SetBusinessProofReviewStatusAsync(int userAccountId, string reviewStatusCd);
}
