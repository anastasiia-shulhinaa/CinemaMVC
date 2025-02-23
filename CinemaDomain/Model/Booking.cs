namespace CinemaDomain.Model;

public partial class Booking : Entity
{
    public int SessionId { get; set; }

    public int? NumberOfSeats { get; set; }

    public byte[] IsPrivateBooking { get; set; } = null!;

    public decimal? PrivateBookingPrice { get; set; }

    public byte[] BookingDate { get; set; } = null!;

    public virtual Session Session { get; set; } = null!;

    public virtual ICollection<SessionSeat> SessionSeats { get; set; } = new List<SessionSeat>();
}
