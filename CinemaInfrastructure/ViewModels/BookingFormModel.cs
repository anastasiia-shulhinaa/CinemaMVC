using CinemaDomain.Model;

namespace CinemaInfrastructure.ViewModels
{
    public class BookingFormModel
    {
        public Movie Movie { get; set; }
        public int SessionId { get; set; }
        public List<TimeOption> AvailableTimes { get; set; }
        public List<SeatViewModel> SessionSeats { get; set; }
        public string SelectedSeats { get; set; }
        public bool IsPrivate { get; set; }
        public decimal? PrivateBookingPrice { get; set; }
    }

    public class TimeOption
    {
        public int SessionId { get; set; }
        public DateTime StartTime { get; set; }
    }
}