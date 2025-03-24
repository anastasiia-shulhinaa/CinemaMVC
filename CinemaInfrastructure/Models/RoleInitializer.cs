using Microsoft.AspNetCore.Identity;
using CinemaInfrastructure.Models;

namespace CinemaInfrastructure
{
    public static class RoleInitializer // Add a class to contain the method
    {
        public static async Task InitializeAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            string adminEmail = "superadmin@gmail.com";
            string password = "superadmin22@";

            if (await roleManager.FindByNameAsync("admin") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("admin"));
            }

            if (await roleManager.FindByNameAsync("user") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("user"));
            }

            if (await userManager.FindByNameAsync(adminEmail) == null)
            {
                User admin = new User { Email = adminEmail, UserName = adminEmail };
                IdentityResult result = await userManager.CreateAsync(admin, password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "admin");
                }
                else
                {
                    // Log errors if user creation failed
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating user {adminEmail}: {error.Description}");
                    }
                }
            }
        }
    }
}



