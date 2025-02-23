namespace CinemaDomain.Model;

public partial class Seat : Entity
{
    public int HallId { get; set; }

    public int SeatNumber { get; set; }

    public int RowNumber { get; set; }

    public virtual Hall Hall { get; set; } = null!;

    public virtual ICollection<SessionSeat> SessionSeats { get; set; } = new List<SessionSeat>();
}
