using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data Access Object interface for the Admin-only Business Category
/// management screen (add/remove entries in the E-02 dropdown).
/// </summary>
public interface IBusinessCategoryDao
{
    /// <summary>All categories, active and inactive, for the admin list view.</summary>
    Task<List<BusinessCategory>> GetAllAsync();

    Task<bool> NameExistsAsync(string categoryName);

    Task<BusinessCategory> AddAsync(BusinessCategory category);

    Task<BusinessCategory?> GetByIdAsync(int businessCategoryId);

    /// <summary>
    /// True if any EMPLOYER_PROFILE row currently references this category —
    /// used to decide between a hard delete and a soft (deactivate) delete.
    /// </summary>
    Task<bool> IsInUseAsync(int businessCategoryId);

    /// <summary>Hard-deletes a category that no employer currently uses.</summary>
    Task DeleteAsync(int businessCategoryId);

    /// <summary>Soft-deletes (deactivates) a category that's still referenced by existing employers.</summary>
    Task DeactivateAsync(int businessCategoryId);

    /// <summary>Re-activates a previously deactivated category.</summary>
    Task ReactivateAsync(int businessCategoryId);
}
