using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class AdminSkillDao : IAdminSkillDao
{
    private readonly AppDbContext _db;

    public AdminSkillDao(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Skill>> GetAllAsync() =>
        _db.Skills
           .AsNoTracking()
           .OrderBy(s => s.SkillName)
           .ToListAsync();

    public Task<bool> NameExistsAsync(string skillName) =>
        _db.Skills.AnyAsync(s => s.SkillName.ToLower() == skillName.ToLower());

    public async Task<Skill> AddAsync(Skill skill)
    {
        _db.Skills.Add(skill);
        await _db.SaveChangesAsync();
        return skill;
    }

    public Task<Skill?> GetByIdAsync(int skillId) =>
        _db.Skills.FirstOrDefaultAsync(s => s.SkillId == skillId);

    public async Task<bool> IsInUseAsync(int skillId)
    {
        var usedByEmployee = await _db.EmployeeSkills.AnyAsync(es => es.SkillId == skillId);
        if (usedByEmployee) return true;

        return await _db.JobSkills.AnyAsync(js => js.SkillId == skillId);
    }

    public async Task DeleteAsync(int skillId)
    {
        var skill = await _db.Skills.FindAsync(skillId);
        if (skill == null) return;

        _db.Skills.Remove(skill);
        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int skillId)
    {
        var skill = await _db.Skills.FindAsync(skillId);
        if (skill == null) return;

        skill.IsActiveFl = false;
        skill.UpdatedBy = "ADMIN_SKILL";
        skill.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task ReactivateAsync(int skillId)
    {
        var skill = await _db.Skills.FindAsync(skillId);
        if (skill == null) return;

        skill.IsActiveFl = true;
        skill.UpdatedBy = "ADMIN_SKILL";
        skill.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
