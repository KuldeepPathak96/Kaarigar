using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class AdminEmployerService : IAdminEmployerService
{
    private readonly IAdminEmployerDao _dao;
    private readonly ILogger<AdminEmployerService> _logger;

    public AdminEmployerService(IAdminEmployerDao dao, ILogger<AdminEmployerService> logger)
    {
        _dao = dao;
        _logger = logger;
    }

    public async Task<ManageEmployersViewModel> SearchAsync(string? search, string? city, string? status)
    {
        var employers = await _dao.SearchEmployersAsync(search, city, status);
        var cities = await _dao.GetDistinctCitiesAsync();

        return new ManageEmployersViewModel
        {
            Employers = employers,
            CityOptions = cities,
            SearchTerm = search,
            CityFilter = city,
            StatusFilter = status,
        };
    }

    public Task<EmployerDetailViewModel?> GetDetailAsync(int userAccountId) =>
        _dao.GetEmployerDetailAsync(userAccountId);

    public async Task<ServiceResult> ToggleActiveAsync(int userAccountId)
    {
        var detail = await _dao.GetEmployerDetailAsync(userAccountId);
        if (detail == null)
            return new ServiceResult(false, "Employer not found.");

        var newState = !detail.IsActiveFl;
        await _dao.SetActiveStatusAsync(userAccountId, newState);

        _logger.LogInformation("Admin set Employer {Id} IsActiveFl={State}", userAccountId, newState);

        return new ServiceResult(true, newState ? "Employer unblocked." : "Employer blocked.");
    }

    public async Task<ServiceResult> ToggleApprovalAsync(int userAccountId)
    {
        var detail = await _dao.GetEmployerDetailAsync(userAccountId);
        if (detail == null)
            return new ServiceResult(false, "Employer not found.");

        var newState = !detail.IsApprovedFl;
        await _dao.SetApprovedStatusAsync(userAccountId, newState);

        _logger.LogInformation("Admin set Employer {Id} IsApprovedFl={State}", userAccountId, newState);

        return new ServiceResult(true, newState
            ? "Employer approved. They can now post and view jobs."
            : "Approval revoked. Employer can no longer post or view jobs until re-approved.");
    }

    public async Task<ServiceResult> DeleteAsync(int userAccountId)
    {
        var jobsCount = await _dao.GetJobsPostedCountAsync(userAccountId);
        if (jobsCount > 0)
            return new ServiceResult(false,
                $"Can't delete — this employer has {jobsCount} job post(s) on record. Block them instead, or delete their job posts first.");

        await _dao.DeleteEmployerAsync(userAccountId);
        _logger.LogInformation("Admin deleted Employer {Id}", userAccountId);

        return new ServiceResult(true, "Employer deleted.");
    }

    public async Task<ServiceResult> ApproveBusinessProofAsync(int userAccountId)
    {
        var updated = await _dao.SetBusinessProofReviewStatusAsync(userAccountId, "APPROVED");
        if (!updated) return new ServiceResult(false, "Employer not found.");

        _logger.LogInformation("Admin approved business proof for Employer {Id}", userAccountId);
        return new ServiceResult(true, "Business proof approved.");
    }

    public async Task<ServiceResult> RejectBusinessProofAsync(int userAccountId)
    {
        var updated = await _dao.SetBusinessProofReviewStatusAsync(userAccountId, "REJECTED");
        if (!updated) return new ServiceResult(false, "Employer not found.");

        _logger.LogInformation("Admin rejected business proof for Employer {Id}", userAccountId);
        return new ServiceResult(true, "Business proof rejected.");
    }
}
