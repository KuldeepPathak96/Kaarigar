using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class WorkCategoryDao : IWorkCategoryDao
{
    private readonly AppDbContext _db;

    public WorkCategoryDao(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<WorkCategory>> GetByBusinessCategoryIdAsync(int businessCategoryId) =>
        _db.WorkCategories
           .AsNoTracking()
           .Where(wc => wc.BusinessCategoryId == businessCategoryId && wc.IsActiveFl)
           .OrderBy(wc => wc.CategoryName)
           .ToListAsync();

    public Task<List<WorkCategory>> GetAllActiveAsync() =>
        _db.WorkCategories
           .AsNoTracking()
           .Where(wc => wc.IsActiveFl)
           .OrderBy(wc => wc.CategoryName)
           .ToListAsync();

    public Task<List<Skill>> GetSkillsByWorkCategoryIdAsync(int workCategoryId) =>
        _db.WorkCategorySkills
           .AsNoTracking()
           .Where(wcs => wcs.WorkCategoryId == workCategoryId && wcs.IsActiveFl)
           .Select(wcs => wcs.Skill!)
           .Where(s => s != null && s.IsActiveFl)
           .OrderBy(s => s.SkillName)
           .ToListAsync();

    public Task<WorkCategory?> GetByIdAsync(int workCategoryId) =>
        _db.WorkCategories.AsNoTracking().FirstOrDefaultAsync(wc => wc.WorkCategoryId == workCategoryId);
}
