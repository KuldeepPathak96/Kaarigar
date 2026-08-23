using Kaarigar.Models;

namespace Kaarigar.Data;

public interface IWorkCategoryDao
{
    /// <summary>All active Work Categories under one Business Category, e.g. Business Category "Restaurants" → ["Steward / Waiter", "Kitchen Helper", ...].</summary>
    Task<List<WorkCategory>> GetByBusinessCategoryIdAsync(int businessCategoryId);

    /// <summary>All active Work Categories across every Business Category — used by the Worker Register page, which no longer asks for an industry first.</summary>
    Task<List<WorkCategory>> GetAllActiveAsync();

    /// <summary>All active Skills mapped to one Work Category, e.g. Work Category "Steward / Waiter" → ["Steward/Waiter", ...].</summary>
    Task<List<Skill>> GetSkillsByWorkCategoryIdAsync(int workCategoryId);

    Task<WorkCategory?> GetByIdAsync(int workCategoryId);
}
