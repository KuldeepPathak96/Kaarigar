using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data Access Object interface for the Admin-only Skills management screen
/// (add/remove entries in the SKILL master list — used both by Employee's
/// "My Skills" and Job Post's "Required Skills" / Job Title dropdown).
/// </summary>
public interface IAdminSkillDao
{
    /// <summary>All skills, active and inactive, for the admin list view.</summary>
    Task<List<Skill>> GetAllAsync();

    Task<bool> NameExistsAsync(string skillName);

    Task<Skill> AddAsync(Skill skill);

    Task<Skill?> GetByIdAsync(int skillId);

    /// <summary>True if any EMPLOYEE_SKILL or JOB_SKILL row currently references this skill.</summary>
    Task<bool> IsInUseAsync(int skillId);

    Task DeleteAsync(int skillId);

    Task DeactivateAsync(int skillId);

    Task ReactivateAsync(int skillId);
}
