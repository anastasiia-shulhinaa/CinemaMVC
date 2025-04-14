using CinemaDomain.Model;

namespace CinemaInfrastructure.ViewModels
{
    public class MovieWithSessionViewModel
    {
        public Movie Movie { get; set; }
        public List<Cinema> Cinemas { get; set; }
        public int? SelectedCinemaId { get; set; } // Stores the pre-selected cinema ID
    }
}