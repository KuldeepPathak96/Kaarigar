using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IAdminLocationService
{
    Task<List<City>> GetAllCitiesAsync();

    Task<ServiceResult> AddCityAsync(string cityName, string? stateName, string? adminUser = null, string? ipAddress = null);

    /// <summary>Soft-deletes (deactivates) if in use by any area/profile, otherwise deletes outright.</summary>
    Task<ServiceResult> RemoveCityAsync(int cityId);

    Task<ServiceResult> ReactivateCityAsync(int cityId);

    Task<List<Area>> GetAllAreasAsync();

    Task<ServiceResult> AddAreaAsync(int cityId, string areaName, string? pincodeTxt, string? adminUser = null, string? ipAddress = null);

    /// <summary>Soft-deletes (deactivates) if in use by any profile, otherwise deletes outright.</summary>
    Task<ServiceResult> RemoveAreaAsync(int areaId);

    Task<ServiceResult> ReactivateAreaAsync(int areaId);
}
