using Kaarigar.Models;

namespace Kaarigar.Data;

/// <summary>
/// Data Access Object interface for the Admin-only City &amp; Area management
/// screen (add/remove entries in the CITY and AREA master lists — used by
/// the City dropdown / Area type-ahead on Register, Employee Profile,
/// Employer Profile and Post a Job).
/// </summary>
public interface IAdminLocationDao
{
    // ── CITY ─────────────────────────────────────────────────────────────────

    Task<List<City>> GetAllCitiesAsync();

    Task<bool> CityNameExistsAsync(string cityName);

    Task<City> AddCityAsync(City city);

    Task<City?> GetCityByIdAsync(int cityId);

    /// <summary>True if any AREA, EMPLOYEE_PROFILE, EMPLOYER_PROFILE, or JOB_POST row currently references this city (by name).</summary>
    Task<bool> IsCityInUseAsync(int cityId);

    Task DeleteCityAsync(int cityId);

    Task DeactivateCityAsync(int cityId);

    Task ReactivateCityAsync(int cityId);

    // ── AREA ─────────────────────────────────────────────────────────────────

    /// <summary>All areas (active and inactive), newest city first, then alphabetical — for the admin list view.</summary>
    Task<List<Area>> GetAllAreasAsync();

    Task<bool> AreaNameExistsInCityAsync(int cityId, string areaName);

    Task<Area> AddAreaAsync(Area area);

    Task<Area?> GetAreaByIdAsync(int areaId);

    /// <summary>True if any EMPLOYEE_PROFILE or EMPLOYER_PROFILE row currently references this area (by name).</summary>
    Task<bool> IsAreaInUseAsync(int areaId);

    Task DeleteAreaAsync(int areaId);

    Task DeactivateAreaAsync(int areaId);

    Task ReactivateAreaAsync(int areaId);
}
