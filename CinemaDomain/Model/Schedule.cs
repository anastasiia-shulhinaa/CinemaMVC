namespace CinemaDomain.Model;

public partial class Schedule : Entity
{
    public DateTime StartTime { get; set; }

    public int HallId { get; set; }

    public virtual Hall Hall { get; set; } = null!;

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
