namespace CinemaInfrastructure.ViewModels
{
    public class BookingEditModel
    {
        public int Id { get; set; }
        public bool IsPrivateBooking { get; set; }
        public decimal? PrivateBookingPrice { get; set; }
    }
}
