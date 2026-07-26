using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IEmployeeProfileService
{
    Task<EmployeeProfileViewModel?> GetProfileAsync(int userAccountId);

    Task<ServiceResult> UpdateProfileAsync(int userAccountId, EmployeeProfileViewModel model, string? ipAddress = null);
}
