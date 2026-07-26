using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IAdminSkillService
{
    Task<List<Skill>> GetAllAsync();

    Task<ServiceResult> AddAsync(string skillName, string? categoryName, string? adminUser = null, string? ipAddress = null);

    /// <summary>
    /// Removes a skill. If it's currently used by any employee or job post,
    /// it is soft-deleted (deactivated) instead of hard-deleted so existing
    /// records don't dangle; otherwise it's removed outright.
    /// </summary>
    Task<ServiceResult> RemoveAsync(int skillId);

    Task<ServiceResult> ReactivateAsync(int skillId);
}
