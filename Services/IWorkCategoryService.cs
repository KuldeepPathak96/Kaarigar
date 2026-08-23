using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IWorkCategoryService
{
    Task<List<WorkCategory>> GetWorkCategoriesAsync(int businessCategoryId);

    /// <summary>All active Work Categories, unfiltered — Worker Register page.</summary>
    Task<List<WorkCategory>> GetAllActiveWorkCategoriesAsync();

    Task<List<Skill>> GetSkillsAsync(int workCategoryId);
}
