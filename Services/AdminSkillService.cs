using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class AdminSkillService : IAdminSkillService
{
    private readonly IAdminSkillDao _dao;
    private readonly ILogger<AdminSkillService> _logger;

    public AdminSkillService(IAdminSkillDao dao, ILogger<AdminSkillService> logger)
    {
        _dao = dao;
        _logger = logger;
    }

    public Task<List<Skill>> GetAllAsync() => _dao.GetAllAsync();

    public async Task<ServiceResult> AddAsync(string skillName, string? categoryName, string? adminUser = null, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return new ServiceResult(false, "Please enter a skill name.");

        skillName = skillName.Trim();
        if (skillName.Length > 100)
            return new ServiceResult(false, "Skill name is too long (max 100 characters).");

        if (await _dao.NameExistsAsync(skillName))
            return new ServiceResult(false, $"\"{skillName}\" already exists in the list.");

        await _dao.AddAsync(new Skill
        {
            SkillName = skillName,
            CategoryName = string.IsNullOrWhiteSpace(categoryName) ? null : categoryName.Trim(),
            IsActiveFl = true,
            CreatedBy = adminUser ?? "ADMIN_SKILL",
            CreatedIpAddr = ipAddress,
            CreatedTs = DateTime.UtcNow,
        });

        _logger.LogInformation("Skill added: {SkillName}", skillName);
        return new ServiceResult(true, $"\"{skillName}\" added to the list.");
    }

    public async Task<ServiceResult> RemoveAsync(int skillId)
    {
        var skill = await _dao.GetByIdAsync(skillId);
        if (skill == null)
            return new ServiceResult(false, "Skill not found.");

        var inUse = await _dao.IsInUseAsync(skillId);
        if (inUse)
        {
            await _dao.DeactivateAsync(skillId);
            return new ServiceResult(true,
                $"\"{skill.SkillName}\" is used by existing employees or job posts, so it was hidden from the dropdown instead of deleted.");
        }

        await _dao.DeleteAsync(skillId);
        return new ServiceResult(true, $"\"{skill.SkillName}\" was deleted.");
    }

    public async Task<ServiceResult> ReactivateAsync(int skillId)
    {
        var skill = await _dao.GetByIdAsync(skillId);
        if (skill == null)
            return new ServiceResult(false, "Skill not found.");

        await _dao.ReactivateAsync(skillId);
        return new ServiceResult(true, $"\"{skill.SkillName}\" is visible in the dropdown again.");
    }
}
