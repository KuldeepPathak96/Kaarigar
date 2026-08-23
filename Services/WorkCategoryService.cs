using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class WorkCategoryService : IWorkCategoryService
{
    private readonly IWorkCategoryDao _dao;

    public WorkCategoryService(IWorkCategoryDao dao)
    {
        _dao = dao;
    }

    public Task<List<WorkCategory>> GetWorkCategoriesAsync(int businessCategoryId) =>
        businessCategoryId <= 0
            ? Task.FromResult(new List<WorkCategory>())
            : _dao.GetByBusinessCategoryIdAsync(businessCategoryId);

    public Task<List<WorkCategory>> GetAllActiveWorkCategoriesAsync() =>
        _dao.GetAllActiveAsync();

    public Task<List<Skill>> GetSkillsAsync(int workCategoryId) =>
        workCategoryId <= 0
            ? Task.FromResult(new List<Skill>())
            : _dao.GetSkillsByWorkCategoryIdAsync(workCategoryId);
}
