namespace CinemaDomain.Model;

public partial class Hall : Entity
{
    public int CinemaId { get; set; }

    public string Name { get; set; } = null!;

    public int TotalSeats { get; set; }

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
