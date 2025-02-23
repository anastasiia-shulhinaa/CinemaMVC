namespace CinemaDomain.Model;


public partial class Category : Entity
{
    public string Name { get; set; } = null!;

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
