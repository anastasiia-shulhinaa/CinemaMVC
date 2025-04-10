using CinemaDomain.Model;

namespace CinemaInfrastructure.ViewModels;

public class BookingFormModel
{
    public int SessionId { get; set; }
    public bool IsPrivate { get; set; }
    public decimal? PrivateBookingPrice { get; set; }
    public List<int> SelectedSeatIds { get; set; } = new();
    public List<SeatViewModel> SessionSeats { get; set; } = new List<SeatViewModel>();
    public Movie Movie { get; set; }
    public List<TimeOption> AvailableTimes { get; set; } = new();
}

public class TimeOption
{
    public int SessionId { get; set; }
    public DateTime StartTime { get; set; } // Changed to DateTime
}