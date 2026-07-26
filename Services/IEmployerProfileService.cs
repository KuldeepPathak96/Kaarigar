using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IEmployerProfileService
{
    /// <summary>Loads the current profile, pre-populated for the edit form (including dropdown options).</summary>
    Task<EmployerProfileViewModel?> GetProfileAsync(int userAccountId);

    /// <summary>Validates and saves profile changes, including optional logo/proof file uploads.</summary>
    Task<ServiceResult> UpdateProfileAsync(int userAccountId, EmployerProfileViewModel model, string? ipAddress = null);
}
