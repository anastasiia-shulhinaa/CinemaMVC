using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CinemaInfrastructure.Models;
using CinemaInfrastructure.Enums;
using CinemaInfrastructure.ViewModels;

namespace CinemaInfrastructure.Controllers
{
    public class AccountController : Controller
    {

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User
            {
                UserName = model.Username,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction("Login"); // Redirect to Login page
            }

            ViewData["Notification"] = new Notification
            {
                Type = NotificationType.Error,
                Message = "Something went wrong."
            };

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                // Log error messages for further investigation
                Console.WriteLine($"Registration Error: {error.Description}"); // Or use a logging framework
            }

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl // Set the ReturnUrl in the ViewModel
            };
            return View(model);

        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return View(model);
            }
            
            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                // перевіряємо, чи належить URL додатку
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                else
                {

                    return RedirectToAction("Index", "Home"); // Redirect after success
                }
            }

            ViewData["Notification"] = new Notification
            {
                Type = NotificationType.Error,
                Message = "Invalid username or password."
            };

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }

}
