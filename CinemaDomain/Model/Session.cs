namespace CinemaDomain.Model;
using System.ComponentModel.DataAnnotations;
public partial class Session : Entity
{
    public int MovieId { get; set; }

    public int ScheduleId { get; set; }

    public DateTime CreatedAt { get; set; } 

    public bool IsActive { get; set; } 

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Movie Movie { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;

    public virtual ICollection<SessionSeat> SessionSeats { get; set; } = new List<SessionSeat>();
}
