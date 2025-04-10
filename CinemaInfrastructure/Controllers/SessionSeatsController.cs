using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaInfrastructure.ViewModels;
using CinemaInfrastructure;
using Microsoft.AspNetCore.Authorization;

namespace CinemaApp.Controllers
{
    public class SessionSeatsController : Controller
    {
        private readonly DbcinemaContext _context;

        public SessionSeatsController(DbcinemaContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetSeatsForSession(int sessionId)
        {
            var session = _context.Sessions
                .Include(s => s.Schedule.Hall)
                .FirstOrDefault(s => s.Id == sessionId);

            if (session == null)
                return NotFound();

            var seats = _context.SessionSeats
                .Include(ss => ss.Seat)
                .Where(ss => ss.SessionId == sessionId && ss.BookingId == null)
                .Select(ss => new SeatViewModel
                {
                    Id = ss.Id,
                    RowNumber = ss.Seat.RowNumber,
                    SeatNumber = ss.Seat.SeatNumber,
                    Price = ss.Price
                })
                .ToList();

            ViewBag.TotalSeats = session.Schedule.Hall.TotalSeats;
            return PartialView("_SessionSeats", seats);
        }
    }
}