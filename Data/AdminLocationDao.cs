using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class AdminLocationDao : IAdminLocationDao
{
    private readonly AppDbContext _db;

    public AdminLocationDao(AppDbContext db)
    {
        _db = db;
    }

    // ── CITY ─────────────────────────────────────────────────────────────────

    public Task<List<City>> GetAllCitiesAsync() =>
        _db.Cities
           .AsNoTracking()
           .OrderBy(c => c.CityName)
           .ToListAsync();

    public Task<bool> CityNameExistsAsync(string cityName) =>
        _db.Cities.AnyAsync(c => c.CityName.ToLower() == cityName.ToLower());

    public async Task<City> AddCityAsync(City city)
    {
        _db.Cities.Add(city);
        await _db.SaveChangesAsync();
        return city;
    }

    public Task<City?> GetCityByIdAsync(int cityId) =>
        _db.Cities.FirstOrDefaultAsync(c => c.CityId == cityId);

    public async Task<bool> IsCityInUseAsync(int cityId)
    {
        var hasAreas = await _db.Areas.AnyAsync(a => a.CityId == cityId);
        if (hasAreas) return true;

        var city = await _db.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.CityId == cityId);
        if (city == null) return false;

        var usedByEmployee = await _db.EmployeeProfiles.AnyAsync(p => p.CityName != null && p.CityName.ToLower() == city.CityName.ToLower());
        if (usedByEmployee) return true;

        return await _db.EmployerProfiles.AnyAsync(p => p.CityName != null && p.CityName.ToLower() == city.CityName.ToLower());
    }

    public async Task DeleteCityAsync(int cityId)
    {
        var city = await _db.Cities.FindAsync(cityId);
        if (city == null) return;

        _db.Cities.Remove(city);
        await _db.SaveChangesAsync();
    }

    public async Task DeactivateCityAsync(int cityId)
    {
        var city = await _db.Cities.FindAsync(cityId);
        if (city == null) return;

        city.IsActiveFl = false;
        city.UpdatedBy = "ADMIN_LOCATION";
        city.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task ReactivateCityAsync(int cityId)
    {
        var city = await _db.Cities.FindAsync(cityId);
        if (city == null) return;

        city.IsActiveFl = true;
        city.UpdatedBy = "ADMIN_LOCATION";
        city.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ── AREA ─────────────────────────────────────────────────────────────────

    public Task<List<Area>> GetAllAreasAsync() =>
        _db.Areas
           .AsNoTracking()
           .Include(a => a.City)
           .OrderBy(a => a.City!.CityName).ThenBy(a => a.AreaName)
           .ToListAsync();

    public Task<bool> AreaNameExistsInCityAsync(int cityId, string areaName) =>
        _db.Areas.AnyAsync(a => a.CityId == cityId && a.AreaName.ToLower() == areaName.ToLower());

    public async Task<Area> AddAreaAsync(Area area)
    {
        _db.Areas.Add(area);
        await _db.SaveChangesAsync();
        return area;
    }

    public Task<Area?> GetAreaByIdAsync(int areaId) =>
        _db.Areas.Include(a => a.City).FirstOrDefaultAsync(a => a.AreaId == areaId);

    public async Task<bool> IsAreaInUseAsync(int areaId)
    {
        var area = await _db.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.AreaId == areaId);
        if (area == null) return false;

        var usedByEmployee = await _db.EmployeeProfiles.AnyAsync(p => p.AreaAddressTxt != null && p.AreaAddressTxt.ToLower() == area.AreaName.ToLower());
        if (usedByEmployee) return true;

        return await _db.EmployerProfiles.AnyAsync(p => p.AreaAddressTxt != null && p.AreaAddressTxt.ToLower() == area.AreaName.ToLower());
    }

    public async Task DeleteAreaAsync(int areaId)
    {
        var area = await _db.Areas.FindAsync(areaId);
        if (area == null) return;

        _db.Areas.Remove(area);
        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAreaAsync(int areaId)
    {
        var area = await _db.Areas.FindAsync(areaId);
        if (area == null) return;

        area.IsActiveFl = false;
        area.UpdatedBy = "ADMIN_LOCATION";
        area.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task ReactivateAreaAsync(int areaId)
    {
        var area = await _db.Areas.FindAsync(areaId);
        if (area == null) return;

        area.IsActiveFl = true;
        area.UpdatedBy = "ADMIN_LOCATION";
        area.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
