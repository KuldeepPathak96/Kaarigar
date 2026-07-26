using Kaarigar.Data;
using Microsoft.AspNetCore.Mvc;

namespace Kaarigar.Controllers;

/// <summary>
/// Read-only City/Area lookups backing the City dropdown + Area type-ahead on
/// Register, Employee Profile and Employer Profile. Deliberately unauthenticated
/// — Register happens before login, and this is non-sensitive reference data.
/// </summary>
public class LocationController : Controller
{
    private readonly ILocationDao _locationDao;

    public LocationController(ILocationDao locationDao) => _locationDao = locationDao;

    [HttpGet("/api/locations/cities")]
    public async Task<IActionResult> Cities()
    {
        var cities = await _locationDao.GetActiveCitiesAsync();
        return Json(cities.Select(c => new { cityId = c.CityId, cityName = c.CityName }));
    }

    /// <summary>Areas within one city, filtered as the person types (min 1 char) — capped to keep the dropdown short.</summary>
    [HttpGet("/api/locations/areas")]
    public async Task<IActionResult> Areas(int cityId, string? q)
    {
        if (cityId <= 0)
            return Json(Array.Empty<object>());

        var areas = await _locationDao.SearchAreasAsync(cityId, q, maxResults: 20);
        return Json(areas.Select(a => new { areaId = a.AreaId, areaName = a.AreaName }));
    }
}
