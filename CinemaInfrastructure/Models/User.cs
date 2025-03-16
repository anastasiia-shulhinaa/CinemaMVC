using Microsoft.AspNetCore.Identity;

namespace CinemaInfrastructure.Models
{
    public class User : IdentityUser
    {
        public int Year { get; set; }
    }
}

