using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Model;

public partial class Cinema : Entity
{
    [Required(ErrorMessage = "Назва кінотеатру є обов'язковою")]
    [Display(Name = "Назва")]
    [StringLength(100, ErrorMessage = "Назва не може бути довшою за 100 символів")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Адреса є обов'язковою")]
    [Display(Name = "Адреса")]
    public string Address { get; set; } = null!;

    [Display(Name = "Опис")]
    [StringLength(500, ErrorMessage = "Опис не може бути довшим за 500 символів")]
    public string? Description { get; set; }

    [Display(Name = "Кількість залів")]
    [Range(1, 5, ErrorMessage = "Кінотеатр повинен мати хоча б 1 зал і не більше 5")]
    public int TotalCinemaHalls { get; set; }

    public virtual ICollection<Hall> Halls { get; set; } = new List<Hall>();
    
    // Generate Google Maps url from Address
    public string GoogleMapsUrl => $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(Address)}";
}