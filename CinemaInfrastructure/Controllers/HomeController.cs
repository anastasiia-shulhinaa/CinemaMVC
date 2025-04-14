using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


public class HomeController : Controller
{
    private readonly DbcinemaContext _context;

    public HomeController(DbcinemaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        string? selectedCinemaId = HttpContext.Session.GetString("SelectedCinemaId");

        IQueryable<Session> sessionsQuery = _context.Sessions
            .Include(s => s.Movie)
            .Include(s => s.Schedule)
            .ThenInclude(sch => sch.Hall)
            .ThenInclude(h => h.Cinema)
            .Where(s => s.IsActive && s.Schedule.StartTime >= DateTime.Today);

        if (!string.IsNullOrEmpty(selectedCinemaId) && int.TryParse(selectedCinemaId, out int cinemaId) && cinemaId != 0)
        {
            sessionsQuery = sessionsQuery.Where(s => s.Schedule.Hall.CinemaId == cinemaId);
        }

        var sessions = await sessionsQuery.OrderBy(s => s.Schedule.StartTime).ToListAsync();

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