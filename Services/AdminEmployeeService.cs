using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class AdminEmployeeService : IAdminEmployeeService
{
    private readonly IAdminEmployeeDao _dao;
    private readonly ILogger<AdminEmployeeService> _logger;

    public AdminEmployeeService(IAdminEmployeeDao dao, ILogger<AdminEmployeeService> logger)
    {
        _dao = dao;
        _logger = logger;
    }

    public async Task<ManageEmployeesViewModel> SearchAsync(string? search, string? city, string? skill, string? status)
    {
        var employees = await _dao.SearchEmployeesAsync(search, city, skill, status);
        var cities = await _dao.GetDistinctCitiesAsync();
        var skills = await _dao.GetDistinctSkillsAsync();

        return new ManageEmployeesViewModel
        {
            Employees = employees,
            CityOptions = cities,
            SkillOptions = skills,
            SearchTerm = search,
            CityFilter = city,
            SkillFilter = skill,
            StatusFilter = status,
        };
    }

    public Task<EmployeeDetailViewModel?> GetDetailAsync(int userAccountId) =>
        _dao.GetEmployeeDetailAsync(userAccountId);

    public async Task<ServiceResult> ToggleActiveAsync(int userAccountId)
    {
        var detail = await _dao.GetEmployeeDetailAsync(userAccountId);
        if (detail == null)
            return new ServiceResult(false, "Employee not found.");

        var newState = !detail.IsActiveFl;
        await _dao.SetActiveStatusAsync(userAccountId, newState);

        _logger.LogInformation("Admin set Employee {Id} IsActiveFl={State}", userAccountId, newState);

        return new ServiceResult(true, newState ? "Employee unblocked." : "Employee blocked.");
    }

    public async Task<ServiceResult> ToggleApprovalAsync(int userAccountId)
    {
        var detail = await _dao.GetEmployeeDetailAsync(userAccountId);
        if (detail == null)
            return new ServiceResult(false, "Employee not found.");

        var newState = !detail.IsApprovedFl;
        await _dao.SetApprovedStatusAsync(userAccountId, newState);

        _logger.LogInformation("Admin set Employee {Id} IsApprovedFl={State}", userAccountId, newState);

        return new ServiceResult(true, newState
            ? "Employee approved."
            : "Approval revoked. Employee is now pending approval again.");
    }

    public async Task<ServiceResult> DeleteAsync(int userAccountId)
    {
        var applicationsCount = await _dao.GetApplicationsCountAsync(userAccountId);
        if (applicationsCount > 0)
            return new ServiceResult(false,
                $"Can't delete — this employee has {applicationsCount} job application(s) on record. Block them instead.");

        await _dao.DeleteEmployeeAsync(userAccountId);
        _logger.LogInformation("Admin deleted Employee {Id}", userAccountId);

        return new ServiceResult(true, "Employee deleted.");
    }

    public async Task<ServiceResult> ApproveDocumentAsync(int employeeDocumentId, int adminUserAccountId)
    {
        var updated = await _dao.SetDocumentReviewStatusAsync(employeeDocumentId, "APPROVED", adminUserAccountId, null);
        if (!updated) return new ServiceResult(false, "Document not found.");

        _logger.LogInformation("Admin {AdminId} approved EmployeeDocumentId={DocId}", adminUserAccountId, employeeDocumentId);
        return new ServiceResult(true, "Document approved.");
    }

    public async Task<ServiceResult> RejectDocumentAsync(int employeeDocumentId, int adminUserAccountId, string? reason)
    {
        var updated = await _dao.SetDocumentReviewStatusAsync(employeeDocumentId, "REJECTED", adminUserAccountId, reason);
        if (!updated) return new ServiceResult(false, "Document not found.");

        _logger.LogInformation("Admin {AdminId} rejected EmployeeDocumentId={DocId}", adminUserAccountId, employeeDocumentId);
        return new ServiceResult(true, "Document rejected.");
    }
}
