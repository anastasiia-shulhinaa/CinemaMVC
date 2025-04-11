using CinemaDomain.Model;

namespace CinemaInfrastructure.ViewModels
{
    public class MyBookingsViewModel
    {
        public List<Booking> PrivateBookings { get; set; } = new List<Booking>();
        public List<Booking> TicketBookings { get; set; } = new List<Booking>();
    }
}
