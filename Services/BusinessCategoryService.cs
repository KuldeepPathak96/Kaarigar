using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class BusinessCategoryService : IBusinessCategoryService
{
    private readonly IBusinessCategoryDao _dao;
    private readonly ILogger<BusinessCategoryService> _logger;

    public BusinessCategoryService(IBusinessCategoryDao dao, ILogger<BusinessCategoryService> logger)
    {
        _dao = dao;
        _logger = logger;
    }

    public Task<List<BusinessCategory>> GetAllAsync() => _dao.GetAllAsync();

    public async Task<ServiceResult> AddAsync(string categoryName, string? adminUser = null, string? ipAddress = null)
    {
        var name = categoryName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return new ServiceResult(false, "Category name is required.");

        if (name.Length > 150)
            return new ServiceResult(false, "Category name cannot exceed 150 characters.");

        if (await _dao.NameExistsAsync(name))
            return new ServiceResult(false, $"\"{name}\" already exists in the list.");

        await _dao.AddAsync(new BusinessCategory
        {
            CategoryName = name,
            IsActiveFl = true,
            CreatedBy = adminUser ?? "ADMIN_BUSINESS_CATEGORY",
            CreatedIpAddr = ipAddress,
            CreatedTs = DateTime.UtcNow,
        });

        _logger.LogInformation("Business category added: {Name}", name);
        return new ServiceResult(true, $"\"{name}\" added to the list.");
    }

    public async Task<ServiceResult> RemoveAsync(int businessCategoryId)
    {
        var category = await _dao.GetByIdAsync(businessCategoryId);
        if (category == null)
            return new ServiceResult(false, "Category not found.");

        var inUse = await _dao.IsInUseAsync(businessCategoryId);
        if (inUse)
        {
            await _dao.DeactivateAsync(businessCategoryId);
            return new ServiceResult(true,
                $"\"{category.CategoryName}\" is used by existing employers, so it was hidden from the dropdown instead of deleted.");
        }

        await _dao.DeleteAsync(businessCategoryId);
        return new ServiceResult(true, $"\"{category.CategoryName}\" was deleted.");
    }

    public async Task<ServiceResult> ReactivateAsync(int businessCategoryId)
    {
        var category = await _dao.GetByIdAsync(businessCategoryId);
        if (category == null)
            return new ServiceResult(false, "Category not found.");

        await _dao.ReactivateAsync(businessCategoryId);
        return new ServiceResult(true, $"\"{category.CategoryName}\" is visible in the dropdown again.");
    }
}
