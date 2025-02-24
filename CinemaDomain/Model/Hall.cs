using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Model;

public partial class Hall : Entity
{
    [Required(ErrorMessage = "Виберіть кінотеатр")]
    [Display(Name = "Кінотеатр")]
    public int CinemaId { get; set; }

    [Required(ErrorMessage = "Назва залу є обов'язковою")]
    [Display(Name = "Назва")]
    [StringLength(100, ErrorMessage = "Назва не може бути довшою за 100 символів")]
    public string Name { get; set; } = null!;

    [Display(Name = "Кількість місць")]
    [Range(8, 50, ErrorMessage = "Зал повинен мати від 8 до 50 місць")]
    public int TotalSeats { get; set; }

    [Display(Name = "Кінотеатр")]
    public virtual Cinema Cinema { get; set; } = null!;

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
