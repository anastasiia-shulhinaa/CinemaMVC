namespace CinemaDomain.Model;

public partial class Cinema : Entity
{
   public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Description { get; set; }

    public int TotalCinemaHalls { get; set; }

    public virtual ICollection<Hall> Halls { get; set; } = new List<Hall>();
}
