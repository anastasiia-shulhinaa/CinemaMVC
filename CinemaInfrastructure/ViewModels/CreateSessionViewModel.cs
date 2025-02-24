using CinemaDomain.Model;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CinemaInfrastructure.ViewModels
{
    public class CreateSessionViewModel
    {
        // Selected IDs
        public int? SelectedCinemaId { get; set; }
        public int? SelectedHallId { get; set; }
        public int? SelectedScheduleId { get; set; }
        public int? SelectedMovieId { get; set; }
        public string SessionType { get; set; }

        // Dropdown lists
        public IEnumerable<Cinema> Cinemas { get; set; }
        public IEnumerable<Hall> Halls { get; set; }
        public IEnumerable<Schedule> Schedules { get; set; }
        public IEnumerable<Movie> Movies { get; set; }
    }
}
