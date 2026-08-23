using Kaarigar.Data;
using Kaarigar.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kaarigar.Controllers;

/// <summary>
/// Read-only Business Category → Work Category → Skill lookups backing the
/// "Work Preference" cascading picker on the Worker Register page (and,
/// where reused, Employee Profile). Deliberately unauthenticated — Register
/// happens before login, and this is non-sensitive reference data — same
/// pattern as LocationController.
/// </summary>
public class WorkCategoryController : Controller
{
    private readonly IBusinessCategoryService _businessCategoryService;
    private readonly IWorkCategoryService _workCategoryService;

    public WorkCategoryController(IBusinessCategoryService businessCategoryService, IWorkCategoryService workCategoryService)
    {
        _businessCategoryService = businessCategoryService;
        _workCategoryService = workCategoryService;
    }

    [HttpGet("/api/workcategories/business-categories")]
    public async Task<IActionResult> BusinessCategories()
    {
        var categories = await _businessCategoryService.GetAllAsync();
        return Json(categories
            .Where(c => c.IsActiveFl)
            .Select(c => new { businessCategoryId = c.BusinessCategoryId, categoryName = c.CategoryName }));
    }

    [HttpGet("/api/workcategories/work-categories")]
    public async Task<IActionResult> WorkCategories(int businessCategoryId)
    {
        if (businessCategoryId <= 0)
            return Json(Array.Empty<object>());

        var workCategories = await _workCategoryService.GetWorkCategoriesAsync(businessCategoryId);
        return Json(workCategories.Select(wc => new { workCategoryId = wc.WorkCategoryId, categoryName = wc.CategoryName }));
    }

    /// <summary>
    /// All active Work Categories, unfiltered by industry. Backs the Worker
    /// Register page's "Work Category" dropdown, which no longer asks the
    /// worker to pick a Business Category first.
    /// </summary>
    [HttpGet("/api/workcategories/work-categories/all")]
    public async Task<IActionResult> AllWorkCategories()
    {
        var workCategories = await _workCategoryService.GetAllActiveWorkCategoriesAsync();
        return Json(workCategories.Select(wc => new { workCategoryId = wc.WorkCategoryId, categoryName = wc.CategoryName }));
    }

    [HttpGet("/api/workcategories/skills")]
    public async Task<IActionResult> Skills(int workCategoryId)
    {
        if (workCategoryId <= 0)
            return Json(Array.Empty<object>());

        var skills = await _workCategoryService.GetSkillsAsync(workCategoryId);
        return Json(skills.Select(s => new { skillId = s.SkillId, skillName = s.SkillName }));
    }
}
