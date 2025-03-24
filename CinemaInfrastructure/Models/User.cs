using Microsoft.AspNetCore.Identity;

namespace CinemaInfrastructure.Models
{
    public class User : IdentityUser
    {
        public DateOnly? DateOfBirth { get; set; }
    }
}
