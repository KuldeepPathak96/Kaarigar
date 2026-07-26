using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data Access Object for the Admin-only Hourly Rate management screen
/// (add/edit/remove entries in the Post a Job "Hourly Rate" dropdown).
/// </summary>
public interface IHourlyRateOptionDao
{
    /// <summary>All rate options, active and inactive, for the admin list view.</summary>
    Task<List<HourlyRateOption>> GetAllAsync();

    /// <summary>Active rate options only, ordered for display — for the Post a Job dropdown.</summary>
    Task<List<HourlyRateOption>> GetActiveAsync();

    Task<HourlyRateOption?> GetByIdAsync(int rateOptionId);

    Task<HourlyRateOption> AddAsync(HourlyRateOption option);

    Task UpdateAsync(HourlyRateOption option);

    /// <summary>True if any JOB_POST currently references this exact rate amount — used to decide hard vs soft delete.</summary>
    Task<bool> IsInUseAsync(decimal hourlyRateAmt);

    Task DeleteAsync(int rateOptionId);

    Task DeactivateAsync(int rateOptionId);

    Task ReactivateAsync(int rateOptionId);
}
