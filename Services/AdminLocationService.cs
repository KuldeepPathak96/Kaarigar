using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class AdminLocationService : IAdminLocationService
{
    private readonly IAdminLocationDao _dao;
    private readonly ILogger<AdminLocationService> _logger;

    public AdminLocationService(IAdminLocationDao dao, ILogger<AdminLocationService> logger)
    {
        _dao = dao;
        _logger = logger;
    }

    // ── CITY ─────────────────────────────────────────────────────────────────

    public Task<List<City>> GetAllCitiesAsync() => _dao.GetAllCitiesAsync();

    public async Task<ServiceResult> AddCityAsync(string cityName, string? stateName, string? adminUser = null, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(cityName))
            return new ServiceResult(false, "Please enter a city name.");

        cityName = cityName.Trim();
        if (cityName.Length > 100)
            return new ServiceResult(false, "City name is too long (max 100 characters).");

        if (await _dao.CityNameExistsAsync(cityName))
            return new ServiceResult(false, $"\"{cityName}\" already exists in the list.");

        await _dao.AddCityAsync(new City
        {
            CityName = cityName,
            StateName = string.IsNullOrWhiteSpace(stateName) ? null : stateName.Trim(),
            IsActiveFl = true,
            CreatedBy = adminUser ?? "ADMIN_LOCATION",
            CreatedTs = DateTime.UtcNow,
        });

        _logger.LogInformation("City added: {CityName}", cityName);
        return new ServiceResult(true, $"\"{cityName}\" added to the list.");
    }

    public async Task<ServiceResult> RemoveCityAsync(int cityId)
    {
        var city = await _dao.GetCityByIdAsync(cityId);
        if (city == null)
            return new ServiceResult(false, "City not found.");

        var inUse = await _dao.IsCityInUseAsync(cityId);
        if (inUse)
        {
            await _dao.DeactivateCityAsync(cityId);
            return new ServiceResult(true,
                $"\"{city.CityName}\" has areas or profiles using it, so it was hidden from the dropdown instead of deleted.");
        }

        await _dao.DeleteCityAsync(cityId);
        return new ServiceResult(true, $"\"{city.CityName}\" was deleted.");
    }

    public async Task<ServiceResult> ReactivateCityAsync(int cityId)
    {
        var city = await _dao.GetCityByIdAsync(cityId);
        if (city == null)
            return new ServiceResult(false, "City not found.");

        await _dao.ReactivateCityAsync(cityId);
        return new ServiceResult(true, $"\"{city.CityName}\" is visible in the dropdown again.");
    }

    // ── AREA ─────────────────────────────────────────────────────────────────

    public Task<List<Area>> GetAllAreasAsync() => _dao.GetAllAreasAsync();

    public async Task<ServiceResult> AddAreaAsync(int cityId, string areaName, string? pincodeTxt, string? adminUser = null, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(areaName))
            return new ServiceResult(false, "Please enter an area name.");

        var city = await _dao.GetCityByIdAsync(cityId);
        if (city == null)
            return new ServiceResult(false, "Please select a valid city.");

        areaName = areaName.Trim();
        if (areaName.Length > 150)
            return new ServiceResult(false, "Area name is too long (max 150 characters).");

        if (await _dao.AreaNameExistsInCityAsync(cityId, areaName))
            return new ServiceResult(false, $"\"{areaName}\" already exists in {city.CityName}.");

        await _dao.AddAreaAsync(new Area
        {
            CityId = cityId,
            AreaName = areaName,
            PincodeTxt = string.IsNullOrWhiteSpace(pincodeTxt) ? null : pincodeTxt.Trim(),
            IsActiveFl = true,
            CreatedBy = adminUser ?? "ADMIN_LOCATION",
            CreatedTs = DateTime.UtcNow,
        });

        _logger.LogInformation("Area added: {AreaName} ({CityName})", areaName, city.CityName);
        return new ServiceResult(true, $"\"{areaName}\" added to {city.CityName}.");
    }

    public async Task<ServiceResult> RemoveAreaAsync(int areaId)
    {
        var area = await _dao.GetAreaByIdAsync(areaId);
        if (area == null)
            return new ServiceResult(false, "Area not found.");

        var inUse = await _dao.IsAreaInUseAsync(areaId);
        if (inUse)
        {
            await _dao.DeactivateAreaAsync(areaId);
            return new ServiceResult(true,
                $"\"{area.AreaName}\" is used by existing profiles, so it was hidden from the dropdown instead of deleted.");
        }

        await _dao.DeleteAreaAsync(areaId);
        return new ServiceResult(true, $"\"{area.AreaName}\" was deleted.");
    }

    public async Task<ServiceResult> ReactivateAreaAsync(int areaId)
    {
        var area = await _dao.GetAreaByIdAsync(areaId);
        if (area == null)
            return new ServiceResult(false, "Area not found.");

        await _dao.ReactivateAreaAsync(areaId);
        return new ServiceResult(true, $"\"{area.AreaName}\" is visible in the dropdown again.");
    }
}
