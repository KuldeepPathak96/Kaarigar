using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IBusinessCategoryService
{
    Task<List<BusinessCategory>> GetAllAsync();

    Task<ServiceResult> AddAsync(string categoryName, string? adminUser = null, string? ipAddress = null);

    /// <summary>
    /// Removes a category. If it's currently used by any employer profile,
    /// it is soft-deleted (deactivated) instead of hard-deleted so existing
    /// employer records don't dangle; otherwise it's removed outright.
    /// </summary>
    Task<ServiceResult> RemoveAsync(int businessCategoryId);

    Task<ServiceResult> ReactivateAsync(int businessCategoryId);
}
