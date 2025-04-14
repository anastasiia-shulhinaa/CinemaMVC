using CinemaDomain.Model;
using CinemaInfrastructure;
using CinemaInfrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


public class HomeController : Controller
{
    private readonly DbcinemaContext _context;
    private readonly UserManager<User> _userManager;

    public HomeController(DbcinemaContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User); // Get the current user's ID (null if not logged in)

        // Fetch sessions with related data
        var sessionsQuery = _context.Sessions
            .Include(s => s.Movie)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Hall)
                .ThenInclude(h => h.Cinema)
            .Include(s => s.Bookings) // Include the Bookings collection
            .Where(s => s.IsActive && s.Schedule.StartTime >= DateTime.Today);

        // Apply cinema filter if a cinema is selected
        string? selectedCinemaId = HttpContext.Session.GetString("SelectedCinemaId");
        if (!string.IsNullOrEmpty(selectedCinemaId) && selectedCinemaId != "0" && int.TryParse(selectedCinemaId, out int cinemaId))
        {
            sessionsQuery = sessionsQuery.Where(s => s.Schedule.Hall.CinemaId == cinemaId);
        }

        // Filter out sessions that have private bookings made by other users
        if (currentUserId != null)
        {
            sessionsQuery = sessionsQuery
                .Where(s => !s.Bookings.Any(b => b.IsPrivateBooking && b.UserId != currentUserId));
        }
        else
        {
            // For anonymous users, exclude all sessions with private bookings
            sessionsQuery = sessionsQuery
                .Where(s => !s.Bookings.Any(b => b.IsPrivateBooking));
        }

        var sessions = await sessionsQuery.ToListAsync();

        // Fetch all UserIds from the bookings
        var allBookings = sessions.SelectMany(s => s.Bookings).ToList();
        var userIds = allBookings.Select(b => b.UserId).Distinct().ToList();

        // Load users for those UserIds
        var userDict = new Dictionary<string, User>();
        foreach (var userId in userIds)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                userDict[userId] = user;
            }
        }

        // Pass the user dictionary to the view (optional, for future use)
        ViewBag.UserDictionary = userDict;

        return View(sessions);
    }

    [HttpPost]
    public IActionResult SetCinemaFilter(int cinemaId, string cinemaName)
    {
        if (cinemaId == 0)
        {
            HttpContext.Session.Remove("SelectedCinemaId");
            HttpContext.Session.Remove("SelectedCinemaName");
        }
        else
        {
            HttpContext.Session.SetString("SelectedCinemaId", cinemaId.ToString());
            HttpContext.Session.SetString("SelectedCinemaName", cinemaName);
        }
        return Ok();
    }
}