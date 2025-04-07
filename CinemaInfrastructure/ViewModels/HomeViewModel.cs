using CinemaDomain.Model;

namespace CinemaInfrastructure.ViewModels
{
    // ViewModels/HomeViewModel.cs
    public class MovieCarouselItem
    {
        public Movie Movie { get; set; }
        public List<Schedule> TodaySchedules { get; set; }
    }

    public class HomeViewModel
    {
        public List<MovieCarouselItem> CarouselItems { get; set; }
        // other properties like Trending, ComingSoon...
    }
}
