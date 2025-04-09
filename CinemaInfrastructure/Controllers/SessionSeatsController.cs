using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaInfrastructure.ViewModels;
using CinemaInfrastructure;

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
        public async Task<IActionResult> GetSeats(int sessionId)
        {
            var seats = await _context.SessionSeats
                .Include(s => s.Seat)
                .Where(s => s.SessionId == sessionId)
                .Select(s => new SeatViewModel
                {
                    Id = s.Id,
                    RowNumber = s.Seat.RowNumber,
                    SeatNumber = s.Seat.SeatNumber,
                    Price = s.Price,
                    TopRowLabel = s.Seat.RowNumber * 40 // Adjust as needed for row position
                })
                .ToListAsync();

            return PartialView("_SessionSeats", seats);  // This should now correctly pass a List<SeatViewModel> to the view
        }
    }
}