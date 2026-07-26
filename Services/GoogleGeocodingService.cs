using System.Text.Json;

namespace Kaarigar.Services;

/// <summary>
/// Server-side reverse geocoding via the Google Maps Geocoding API.
/// Kept server-side (rather than calling Google directly from the browser)
/// so the API key never reaches the client and can be key-restricted to
/// this server's IP instead of being restricted to a public HTTP referrer.
///
/// Requires in appsettings.json:
///   "GoogleMaps": { "ApiKey": "YOUR_SERVER_KEY" }
///
/// The key needs the "Geocoding API" enabled in Google Cloud Console.
/// </summary>
public class GoogleGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleGeocodingService> _logger;

    public GoogleGeocodingService(HttpClient httpClient, IConfiguration config, ILogger<GoogleGeocodingService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<ReverseGeocodeResult?> ReverseGeocodeAsync(decimal latitude, decimal longitude)
    {
        var apiKey = _config["GoogleMaps:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("GoogleMaps:ApiKey is not configured — reverse geocoding skipped.");
            return null;
        }

        var url = $"https://maps.googleapis.com/maps/api/geocode/json" +
                  $"?latlng={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                  $"{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&key={apiKey}";

        try
        {
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Geocoding API returned {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var status = root.GetProperty("status").GetString();
            if (status != "OK")
            {
                _logger.LogWarning("Google Geocoding API status: {Status}", status);
                return null;
            }

            var results = root.GetProperty("results");
            if (results.GetArrayLength() == 0)
                return null;

            var firstResult = results[0];
            var formattedAddress = firstResult.GetProperty("formatted_address").GetString();

            string? cityName = null;
            if (firstResult.TryGetProperty("address_components", out var components))
            {
                // Prefer "locality" (city). Fall back to
                // "administrative_area_level_2" (district) for rural/GPS
                // points that fall outside a named locality.
                cityName = FindComponent(components, "locality")
                        ?? FindComponent(components, "administrative_area_level_2")
                        ?? FindComponent(components, "administrative_area_level_1");
            }

            return new ReverseGeocodeResult(cityName, formattedAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reverse geocoding failed for {Lat},{Lng}", latitude, longitude);
            return null;
        }
    }

    private static string? FindComponent(JsonElement addressComponents, string typeName)
    {
        foreach (var component in addressComponents.EnumerateArray())
        {
            if (!component.TryGetProperty("types", out var types)) continue;

            foreach (var type in types.EnumerateArray())
            {
                if (type.GetString() == typeName)
                    return component.GetProperty("long_name").GetString();
            }
        }
        return null;
    }
}
