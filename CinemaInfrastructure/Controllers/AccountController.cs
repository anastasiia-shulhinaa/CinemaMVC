using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CinemaInfrastructure.Models;
using CinemaInfrastructure.Enums;
using CinemaInfrastructure.ViewModels;
using Microsoft.Extensions.Logging; // For logging

namespace CinemaInfrastructure.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AccountController> _logger; // Add logger

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                _logger.LogInformation("User {Username} registered successfully.", model.Username);
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
                _logger.LogWarning("Registration error for {Username}: {Error}", model.Username, error.Description);
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
                _logger.LogWarning("Login validation failed: {Errors}", string.Join(", ", errors));
                return View(model);
            }

            // Check if the user exists
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent username: {Username}", model.Username);
                ViewData["Notification"] = new Notification
                {
                    Type = NotificationType.Error,
                    Message = "User does not exist."
                };
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Username} logged in successfully.", model.Username);
                // Check if the ReturnUrl is valid and local
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home"); // Default redirect
                }
            }

            _logger.LogWarning("Failed login attempt for {Username}: Invalid password.", model.Username);
            ViewData["Notification"] = new Notification
            {
                Type = NotificationType.Error,
                Message = "Invalid password."
            };

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name ?? "Unknown";
            await _signInManager.SignOutAsync();
            HttpContext.Session.Remove("AdminMode"); // Clear session on logout
            _logger.LogInformation("User {Username} logged out successfully.", username);
            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/ToggleAdminMode
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public IActionResult ToggleAdminMode()
        {
            // Align with _Layout.cshtml: "user" for user mode, null for admin mode
            var currentMode = HttpContext.Session.GetString("AdminMode");
            if (currentMode == "user")
            {
                HttpContext.Session.Remove("AdminMode"); // Switch to admin mode
                _logger.LogInformation("User {Username} switched to admin mode.", User.Identity?.Name);
            }
            else
            {
                HttpContext.Session.SetString("AdminMode", "user"); // Switch to user mode
                _logger.LogInformation("User {Username} switched to user mode.", User.Identity?.Name);
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Profile access failed: User not found.");
                return RedirectToAction("Login");
            }

            var model = new ProfileViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth
            };

            return View(model);
        }
    }
}