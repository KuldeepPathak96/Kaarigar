namespace Kaarigar.Services;

/// <summary>
/// Result of a reverse-geocode lookup — only the pieces the Employer Profile
/// form needs to auto-fill.
/// </summary>
public record ReverseGeocodeResult(string? CityName, string? FormattedAddress);

public interface IGeocodingService
{
    /// <summary>
    /// Calls the Google Maps Geocoding API to resolve a lat/lng pair into a
    /// city name + formatted address. Returns null if the API call fails or
    /// no result is found.
    /// </summary>
    Task<ReverseGeocodeResult?> ReverseGeocodeAsync(decimal latitude, decimal longitude);
}
