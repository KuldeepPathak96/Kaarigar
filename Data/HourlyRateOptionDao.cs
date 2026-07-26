using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class HourlyRateOptionDao : IHourlyRateOptionDao
{
    private readonly AppDbContext _db;

    public HourlyRateOptionDao(AppDbContext db) => _db = db;

    public Task<List<HourlyRateOption>> GetAllAsync() =>
        _db.HourlyRateOptions.AsNoTracking()
           .OrderBy(r => r.DisplayOrderNbr).ThenBy(r => r.HourlyRateAmt)
           .ToListAsync();

    public Task<List<HourlyRateOption>> GetActiveAsync() =>
        _db.HourlyRateOptions.AsNoTracking()
           .Where(r => r.IsActiveFl)
           .OrderBy(r => r.DisplayOrderNbr).ThenBy(r => r.HourlyRateAmt)
           .ToListAsync();

    public Task<HourlyRateOption?> GetByIdAsync(int rateOptionId) =>
        _db.HourlyRateOptions.FirstOrDefaultAsync(r => r.RateOptionId == rateOptionId);

    public async Task<HourlyRateOption> AddAsync(HourlyRateOption option)
    {
        _db.HourlyRateOptions.Add(option);
        await _db.SaveChangesAsync();
        return option;
    }

    public async Task UpdateAsync(HourlyRateOption option)
    {
        var existing = await _db.HourlyRateOptions.FirstOrDefaultAsync(r => r.RateOptionId == option.RateOptionId);
        if (existing == null) return;

        existing.RateLabelTxt = option.RateLabelTxt;
        existing.HourlyRateAmt = option.HourlyRateAmt;
        existing.DisplayOrderNbr = option.DisplayOrderNbr;
        existing.UpdatedBy = option.UpdatedBy;
        existing.UpdatedTs = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public Task<bool> IsInUseAsync(decimal hourlyRateAmt) =>
        _db.JobPosts.AnyAsync(jp => jp.HourlyWageAmt == hourlyRateAmt);

    public async Task DeleteAsync(int rateOptionId)
    {
        var option = await _db.HourlyRateOptions.FindAsync(rateOptionId);
        if (option == null) return;

        _db.HourlyRateOptions.Remove(option);
        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int rateOptionId)
    {
        var option = await _db.HourlyRateOptions.FindAsync(rateOptionId);
        if (option == null) return;

        option.IsActiveFl = false;
        option.UpdatedBy = "ADMIN_HOURLY_RATE";
        option.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task ReactivateAsync(int rateOptionId)
    {
        var option = await _db.HourlyRateOptions.FindAsync(rateOptionId);
        if (option == null) return;

        option.IsActiveFl = true;
        option.UpdatedBy = "ADMIN_HOURLY_RATE";
        option.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
