using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class LocationDao : ILocationDao
{
    private readonly AppDbContext _db;

    public LocationDao(AppDbContext db) => _db = db;

    public Task<List<City>> GetActiveCitiesAsync() =>
        _db.Cities.AsNoTracking()
           .Where(c => c.IsActiveFl)
           .OrderBy(c => c.CityName)
           .ToListAsync();

    public Task<List<Area>> SearchAreasAsync(int cityId, string? query, int maxResults)
    {
        var areas = _db.Areas.AsNoTracking()
            .Where(a => a.CityId == cityId && a.IsActiveFl);

        if (!string.IsNullOrWhiteSpace(query))
            areas = areas.Where(a => a.AreaName.Contains(query));

        return areas.OrderBy(a => a.AreaName)
                    .Take(maxResults)
                    .ToListAsync();
    }

    public Task<City?> GetCityByNameAsync(string cityName) =>
        _db.Cities.AsNoTracking()
           .FirstOrDefaultAsync(c => c.CityName == cityName);
}
