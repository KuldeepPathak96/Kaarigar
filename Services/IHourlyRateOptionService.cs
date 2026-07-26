using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IHourlyRateOptionService
{
    Task<List<HourlyRateOption>> GetAllAsync();

    Task<List<HourlyRateOption>> GetActiveAsync();

    Task<ServiceResult> AddAsync(string label, decimal amount, string? adminUser = null);

    Task<ServiceResult> UpdateAsync(int rateOptionId, string label, decimal amount, string? adminUser = null);

    /// <summary>
    /// Removes a rate option. If it's currently used by any job post, it's
    /// soft-deleted (deactivated) instead of hard-deleted so existing job
    /// posts don't lose their recorded rate; otherwise it's removed outright.
    /// </summary>
    Task<ServiceResult> RemoveAsync(int rateOptionId);

    Task<ServiceResult> ReactivateAsync(int rateOptionId);
}
