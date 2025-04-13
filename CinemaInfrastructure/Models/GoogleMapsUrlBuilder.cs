using Microsoft.Extensions.Options;
using CinemaDomain.Model;
using CinemaInfrastructure.Models;

public class GoogleMapsUrlBuilder
{
    private readonly string _apiKey;

    public GoogleMapsUrlBuilder(IOptions<GoogleApiSettings> options)
    {
        _apiKey = options.Value.ApiKey;
    }

    public string GetEmbedUrl(Cinema cinema)
    {
        return $"https://www.google.com/maps/embed/v1/place?key={_apiKey}&q={Uri.EscapeDataString(cinema.Address)}";
    }
}
