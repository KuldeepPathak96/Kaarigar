using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data Access Object interface for Screen E-02 (Employer Profile).
/// Keeps raw DB calls out of the service layer.
/// </summary>
public interface IEmployerProfileDao
{
    /// <summary>Loads the UserAccount + EmployerProfile pair for the given user.</summary>
    Task<(UserAccount User, EmployerProfile? Profile)?> GetProfileAsync(int userAccountId);

    /// <summary>True if another (different) active user already has this mobile number.</summary>
    Task<bool> IsContactNbrTakenByOthersAsync(string contactNbr, int excludeUserAccountId);

    /// <summary>True if another (different) active user already has this email.</summary>
    Task<bool> IsEmailTakenByOthersAsync(string emailId, int excludeUserAccountId);

    /// <summary>Persists changes to USER_ACCOUNT's editable contact fields.</summary>
    Task UpdateUserContactFieldsAsync(int userAccountId, string contactNbr, string? emailId);

    /// <summary>
    /// Creates or updates the EMPLOYER_PROFILE row for this user with the
    /// given field values (logo/proof file paths are set separately by the
    /// service after the physical file has been saved).
    /// </summary>
    Task<EmployerProfile> UpsertProfileAsync(EmployerProfile profile);

    /// <summary>Active business categories only, for populating the dropdown.</summary>
    Task<List<BusinessCategory>> GetActiveBusinessCategoriesAsync();

    /// <summary>Validates that the given BusinessCategoryId exists and is active.</summary>
    Task<bool> IsBusinessCategoryValidAsync(int businessCategoryId);
}
