using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class HourlyRateOptionService : IHourlyRateOptionService
{
    private readonly IHourlyRateOptionDao _dao;
    private readonly ILogger<HourlyRateOptionService> _logger;

    public HourlyRateOptionService(IHourlyRateOptionDao dao, ILogger<HourlyRateOptionService> logger)
    {
        _dao = dao;
        _logger = logger;
    }

    public Task<List<HourlyRateOption>> GetAllAsync() => _dao.GetAllAsync();

    public Task<List<HourlyRateOption>> GetActiveAsync() => _dao.GetActiveAsync();

    public async Task<ServiceResult> AddAsync(string label, decimal amount, string? adminUser = null)
    {
        var validation = Validate(label, amount);
        if (validation != null) return new ServiceResult(false, validation);

        var all = await _dao.GetAllAsync();
        var nextOrder = all.Count > 0 ? all.Max(r => r.DisplayOrderNbr) + 1 : 1;

        await _dao.AddAsync(new HourlyRateOption
        {
            RateLabelTxt = label.Trim(),
            HourlyRateAmt = amount,
            DisplayOrderNbr = nextOrder,
            IsActiveFl = true,
            CreatedBy = adminUser ?? "ADMIN_HOURLY_RATE",
            CreatedTs = DateTime.UtcNow,
        });

        _logger.LogInformation("Hourly rate option added: {Label} (₹{Amount}/hr)", label, amount);
        return new ServiceResult(true, $"\"{label.Trim()}\" added to the list.");
    }

    public async Task<ServiceResult> UpdateAsync(int rateOptionId, string label, decimal amount, string? adminUser = null)
    {
        var validation = Validate(label, amount);
        if (validation != null) return new ServiceResult(false, validation);

        var existing = await _dao.GetByIdAsync(rateOptionId);
        if (existing == null) return new ServiceResult(false, "Rate option not found.");

        await _dao.UpdateAsync(new HourlyRateOption
        {
            RateOptionId = rateOptionId,
            RateLabelTxt = label.Trim(),
            HourlyRateAmt = amount,
            DisplayOrderNbr = existing.DisplayOrderNbr,
            UpdatedBy = adminUser ?? "ADMIN_HOURLY_RATE",
        });

        return new ServiceResult(true, $"\"{label.Trim()}\" updated.");
    }

    public async Task<ServiceResult> RemoveAsync(int rateOptionId)
    {
        var option = await _dao.GetByIdAsync(rateOptionId);
        if (option == null) return new ServiceResult(false, "Rate option not found.");

        var inUse = await _dao.IsInUseAsync(option.HourlyRateAmt);
        if (inUse)
        {
            await _dao.DeactivateAsync(rateOptionId);
            return new ServiceResult(true,
                $"\"{option.RateLabelTxt}\" is used by existing job posts, so it was hidden from the dropdown instead of deleted.");
        }

        await _dao.DeleteAsync(rateOptionId);
        return new ServiceResult(true, $"\"{option.RateLabelTxt}\" was deleted.");
    }

    public async Task<ServiceResult> ReactivateAsync(int rateOptionId)
    {
        var option = await _dao.GetByIdAsync(rateOptionId);
        if (option == null) return new ServiceResult(false, "Rate option not found.");

        await _dao.ReactivateAsync(rateOptionId);
        return new ServiceResult(true, $"\"{option.RateLabelTxt}\" is visible in the dropdown again.");
    }

    private static string? Validate(string label, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "Label is required.";

        if (label.Trim().Length > 100)
            return "Label cannot exceed 100 characters.";

        if (amount <= 0 || amount > 100000)
            return "Enter a valid hourly rate amount.";

        return null;
    }
}
