namespace CinemaDomain.Model;

public partial class Actor : Entity
{
    public string FullName { get; set; } = null!;

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
