using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class BusinessCategoryDao : IBusinessCategoryDao
{
    private readonly AppDbContext _db;

    public BusinessCategoryDao(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<BusinessCategory>> GetAllAsync() =>
        _db.BusinessCategories
           .AsNoTracking()
           .OrderBy(c => c.CategoryName)
           .ToListAsync();

    public Task<bool> NameExistsAsync(string categoryName) =>
        _db.BusinessCategories.AnyAsync(c => c.CategoryName.ToLower() == categoryName.ToLower());

    public async Task<BusinessCategory> AddAsync(BusinessCategory category)
    {
        _db.BusinessCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public Task<BusinessCategory?> GetByIdAsync(int businessCategoryId) =>
        _db.BusinessCategories.FirstOrDefaultAsync(c => c.BusinessCategoryId == businessCategoryId);

    public Task<bool> IsInUseAsync(int businessCategoryId) =>
        _db.EmployerProfiles.AnyAsync(p => p.BusinessCategoryId == businessCategoryId);

    public async Task DeleteAsync(int businessCategoryId)
    {
        var category = await _db.BusinessCategories.FindAsync(businessCategoryId);
        if (category == null) return;

        _db.BusinessCategories.Remove(category);
        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int businessCategoryId)
    {
        var category = await _db.BusinessCategories.FindAsync(businessCategoryId);
        if (category == null) return;

        category.IsActiveFl = false;
        category.UpdatedBy = "ADMIN_BUSINESS_CATEGORY";
        category.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task ReactivateAsync(int businessCategoryId)
    {
        var category = await _db.BusinessCategories.FindAsync(businessCategoryId);
        if (category == null) return;

        category.IsActiveFl = true;
        category.UpdatedBy = "ADMIN_BUSINESS_CATEGORY";
        category.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
