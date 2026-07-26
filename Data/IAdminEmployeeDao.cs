using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>Data Access Object interface for Screen A-04 (Manage Employees).</summary>
public interface IAdminEmployeeDao
{
    /// <summary>
    /// Searches/filters employees. All filters are optional and combine with AND.
    /// search matches Name, ContactNbr, or EmailId (case-insensitive, partial).
    /// skill matches an exact SKILL.SKILL_NAME the employee has.
    /// status is "ACTIVE" | "BLOCKED" | null (= no status filter).
    /// </summary>
    Task<List<EmployeeListItemViewModel>> SearchEmployeesAsync(string? search, string? city, string? skill, string? status);

    /// <summary>Distinct, non-null employee city names, for the city filter dropdown.</summary>
    Task<List<string>> GetDistinctCitiesAsync();

    /// <summary>Active skill names, for the skill filter dropdown.</summary>
    Task<List<string>> GetDistinctSkillsAsync();

    Task<EmployeeDetailViewModel?> GetEmployeeDetailAsync(int userAccountId);

    /// <summary>Sets USER_ACCOUNT.IS_ACTIVE_FL — the Block/Unblock action.</summary>
    Task SetActiveStatusAsync(int userAccountId, bool isActive);

    /// <summary>
    /// Sets USER_ACCOUNT.IS_APPROVED_FL — the Admin Approve/Revoke Approval action.
    /// Until this is true, the employee cannot apply to jobs (once that flow is built).
    /// </summary>
    Task SetApprovedStatusAsync(int userAccountId, bool isApproved);

    /// <summary>How many JOB_APPLICATION rows this employee has (delete is blocked if &gt; 0, since FK_JOB_APPLICATION_EMPLOYEE has no cascade).</summary>
    Task<int> GetApplicationsCountAsync(int userAccountId);

    /// <summary>Hard-deletes the USER_ACCOUNT row (and its EMPLOYEE_PROFILE/EMPLOYEE_SKILL rows, via ON DELETE CASCADE). Caller must have already checked GetApplicationsCountAsync == 0.</summary>
    Task DeleteEmployeeAsync(int userAccountId);

    /// <summary>Sets EMPLOYEE_DOCUMENT.REVIEW_STATUS_CD to APPROVED/REJECTED and records who reviewed it. Returns false if the document doesn't exist.</summary>
    Task<bool> SetDocumentReviewStatusAsync(int employeeDocumentId, string reviewStatusCd, int reviewedByUserAccountId, string? rejectionReason);
}
