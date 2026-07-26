using Kaarigar.Models;

namespace Kaarigar.Data;

public interface ILocationDao
{
    /// <summary>All active cities, alphabetical — small list, safe to load in full for a dropdown.</summary>
    Task<List<City>> GetActiveCitiesAsync();

    /// <summary>Active areas within one city, optionally filtered by a typed prefix/substring, capped for a type-ahead dropdown.</summary>
    Task<List<Area>> SearchAreasAsync(int cityId, string? query, int maxResults);

    /// <summary>Looks up a city by exact name (case-insensitive) — used to resolve an existing profile's saved CityName back to a CityId for the dropdown.</summary>
    Task<City?> GetCityByNameAsync(string cityName);
}
