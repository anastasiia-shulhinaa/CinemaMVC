using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CinemaInfrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaInfrastructure.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbcinemaContext _context;

        public HomeController(DbcinemaContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch active sessions including the related movies
            var activeSessions = await _context.Sessions
                .Include(s => s.Movie) // Include Movie details
                .Where(s => s.IsActive == true) // Check if IsActive is true
                .ToListAsync();

            return View(activeSessions); // Pass the active sessions to the view
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

