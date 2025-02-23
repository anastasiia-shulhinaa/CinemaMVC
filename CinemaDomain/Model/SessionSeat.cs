namespace CinemaDomain.Model;

public partial class SessionSeat : Entity
{
    public int? BookingId { get; set; }

    public int SessionId { get; set; }

    public int SeatId { get; set; }

    public decimal Price { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Seat Seat { get; set; } = null!;

    public virtual Session Session { get; set; } = null!;
}
