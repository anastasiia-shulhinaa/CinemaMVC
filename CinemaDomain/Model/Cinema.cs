using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Model
{
    public partial class Cinema : Entity
    {

        [Required(ErrorMessage = "Назва кінотеатру є обов'язковою")]
        [Display(Name = "Назва")]
        [StringLength(100, ErrorMessage = "Назва не може бути довшою за 100 символів")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Місто є обов'язковим")]
        [Display(Name = "Місто")]
        [StringLength(100, ErrorMessage = "Місто не може бути довшим за 100 символів")]
        public string City { get; set; } = null!; // New column

        [Required(ErrorMessage = "Адреса є обов'язковою")]
        [Display(Name = "Адреса")]
        [StringLength(200, ErrorMessage = "Адреса не може бути довшою за 200 символів")]
        public string Address { get; set; } = null!;

        [Display(Name = "Опис")]
        [StringLength(500, ErrorMessage = "Опис не може бути довшим за 500 символів")]
        public string? Description { get; set; }

        [Display(Name = "Кількість залів")]
        [Range(1, 5, ErrorMessage = "Кінотеатр повинен мати хоча б 1 зал і не більше 5")]
        public int TotalCinemaHalls { get; set; }

        [Display(Name = "URL фото")]
        [Url(ErrorMessage = "Будь ласка, введіть дійсну URL-адресу для фото")]
        [StringLength(500, ErrorMessage = "URL фото не може бути довшим за 500 символів")]
        public string? PhotoUrl { get; set; }

        [Required(ErrorMessage = "Номер телефону є обов'язковим")]
        [Display(Name = "Номер телефону")]
        [Phone(ErrorMessage = "Будь ласка, введіть дійсний номер телефону")]
        [StringLength(20, ErrorMessage = "Номер телефону не може бути довшим за 20 символів")]
        public string PhoneNumber { get; set; } = null!;

        public string FormattedPhoneNumber => $"+38 {PhoneNumber}";

        public virtual ICollection<Hall> Halls { get; set; } = new List<Hall>();

        public string GoogleMapsUrl => $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(Address)}";
    }
}