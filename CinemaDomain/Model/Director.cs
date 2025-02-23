namespace CinemaDomain.Model;

public partial class Director : Entity
{
    public string FullName { get; set; } = null!;

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
