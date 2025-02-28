using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaDomain.Model;


public partial class Movie : Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [Display(Name = "Назва фільму")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [Display(Name = "Рік виходу")]
    [Range(1900, 2024, ErrorMessage = "Рік виходу повинен бути не раніше 1900 і не пізніше поточного року")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Рік повинен складатися з 4 цифр")]
    public int ReleaseYear { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [Display(Name = "Мова")]
    [RegularExpression(@"^[a-zA-Zа-яА-ЯёЁіІїЇєЄ]+$", ErrorMessage = "Поле повинно містити лише літери")]
    public string Language { get; set; } = null!;

    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [Display(Name = "Тривалість (хв)")]
    [Range(0, 300, ErrorMessage = "Тривалість фільму повинна бути від 0 до 300 хвилин")]
    public int Duration { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [Display(Name = "Рейтинг")]
    [Range(1, 5, ErrorMessage = "Рейтинг повинен бути від 1 до 5")]
    public string? Rating { get; set; }

    [Display(Name = "Трейлер")]
    [RegularExpression(@"^https:\/\/.*", ErrorMessage = "Посилання на трейлер повинно починатися з https://")]
    public string? TrailerLink { get; set; }

    [NotMapped]
    [RegularExpression(@"^[a-zA-Z0-9_\-]+\.(jpg|jpeg|png)$", ErrorMessage = "Only .jpg, .jpeg, or .png files are allowed.")]
    public string? BannerUrl { get; set; } // Посилання на банер

    [Display(Name = "Актори")]
    public virtual ICollection<Actor> Actors { get; set; } = new List<Actor>();

    [Display(Name = "Жанри")]
    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    [Display(Name = "Режисери")]
    public virtual ICollection<Director> Directors { get; set; } = new List<Director>();
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
