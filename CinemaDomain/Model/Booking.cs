namespace CinemaDomain.Model;

public partial class Booking 
{
    public int Id { get; set; }
    public int SessionId { get; set; }

    public int? NumberOfSeats { get; set; }

    public bool IsPrivateBooking { get; set; }

    public decimal? PrivateBookingPrice { get; set; }
    public string UserId { get; set; } = null!;
    public DateTime BookingDate { get; set; }

    public virtual Session Session { get; set; } = null!;

    public virtual ICollection<SessionSeat> SessionSeats { get; set; } = new List<SessionSeat>();
}
