using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CinemaInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;

namespace CinemaInfrastructure.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbcinemaContext _context;

        public HomeController(DbcinemaContext context)
        {
            _context = context;
        }

        public IActionResult TestSession()
        {
            // Set a test value in the session
            HttpContext.Session.SetString("TestKey", "Session is working!");
            return RedirectToAction("ReadSession");
        }

        public IActionResult ReadSession()
        {
            // Read the test value from the session
            var value = HttpContext.Session.GetString("TestKey") ?? "Session not set";
            ViewBag.SessionValue = value;
            return View();
        }
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var uniqueSessions = _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Schedule)
                .Where(s => s.IsActive && s.Schedule.StartTime > today)
                .ToList()
                .GroupBy(s => s.MovieId)
                .Select(g => g.OrderBy(s => s.Schedule.StartTime).First())
                .ToList();

            return View(uniqueSessions);
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

