using System.ComponentModel.DataAnnotations;

namespace CinemaInfrastructure
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }


        [Display(Name = "Номер телефону")]
        [DataType(DataType.PhoneNumber)]
        public string? PhoneNumber { get; set; } // Optional

        [Display(Name = "Дата народження")]
        public DateOnly? DateOfBirth { get; set; } // Optional

        [Display(Name = "Ім'я")]
        public string? Username { get; set; } // Optional

        [Required]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Паролі не співпадають")]
        [Display(Name = "Підтвердження паролю")]
        public string PasswordConfirm { get; set; }

    }
}
