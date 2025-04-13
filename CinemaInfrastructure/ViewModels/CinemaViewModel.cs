using CinemaDomain.Model;

namespace CinemaInfrastructure.ViewModels
{
    public class CinemaViewModel
    {
        public Cinema Cinema { get; set; } = null!;
        public string GoogleMapsEmbedUrl { get; set; } = string.Empty;
    }
}