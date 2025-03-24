using System.ComponentModel.DataAnnotations;

namespace CinemaInfrastructure.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }
        [Display(Name = "Запам'ятати?")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }

    }
}
