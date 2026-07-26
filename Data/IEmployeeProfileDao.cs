using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data Access Object interface for Screen W-02 (Employee Profile).
/// Mirrors IEmployerProfileDao's shape for the equivalent employee-side screen.
/// </summary>
public interface IEmployeeProfileDao
{
    /// <summary>Loads the UserAccount + EmployeeProfile pair for the given user.</summary>
    Task<(UserAccount User, EmployeeProfile? Profile)?> GetProfileAsync(int userAccountId);

    Task<HashSet<int>> GetSkillIdsAsync(int userAccountId);

    Task<EmployeeDocument?> GetDocumentAsync(int userAccountId, string documentTypeCd);

    /// <summary>True if another (different) active user already has this mobile number.</summary>
    Task<bool> IsContactNbrTakenByOthersAsync(string contactNbr, int excludeUserAccountId);

    /// <summary>True if another (different) active user already has this email.</summary>
    Task<bool> IsEmailTakenByOthersAsync(string emailId, int excludeUserAccountId);

    /// <summary>Persists changes to USER_ACCOUNT's editable Name/Mobile/Email fields.</summary>
    Task UpdateUserFieldsAsync(int userAccountId, string firstName, string? lastName, string contactNbr, string? emailId);

    /// <summary>Creates or updates the EMPLOYEE_PROFILE row for this user.</summary>
    Task UpsertProfileAsync(EmployeeProfile profile);

    /// <summary>Replaces this employee's EMPLOYEE_SKILL rows with the given set (delete-all-then-insert — simplest correct approach for an edit form).</summary>
    Task ReplaceSkillsAsync(int userAccountId, HashSet<int> skillIds);

    /// <summary>Active skills only, grouped for the checkbox list.</summary>
    Task<List<Skill>> GetActiveSkillsAsync();

    /// <summary>
    /// Creates or replaces the single EMPLOYEE_DOCUMENT row for this
    /// (userAccountId, documentTypeCd) pair. Re-uploading resets REVIEW_STATUS_CD
    /// back to PENDING.
    /// </summary>
    Task UpsertDocumentAsync(EmployeeDocument document);
}
