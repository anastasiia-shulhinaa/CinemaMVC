using CinemaDomain.Model;
using CinemaInfrastructure.Models;
using CinemaInfrastructure.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CinemaInfrastructure.Controllers
{
    public class BookingsController : Controller
    {
        private readonly DbcinemaContext _context;
        private readonly UserManager<User> _userManager; // Using IdentityUser

        public BookingsController(DbcinemaContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult SelectSeat(int sessionId)
        {
            var session = _context.Sessions
                .Include(s => s.Schedule.Hall)
                .ThenInclude(h => h.Seats)
                .FirstOrDefault(s => s.Id == sessionId);

            if (session == null)
                return NotFound();

            var viewModel = new BookingFormModel
            {
                SessionId = sessionId,
                SessionSeats = session.Schedule.Hall.Seats.Select(seat => new SeatViewModel
                {
                    Id = seat.Id,
                    RowNumber = seat.RowNumber,
                    SeatNumber = seat.SeatNumber,
                    IsAvailable = !seat.SessionSeats.Any(b => b.SessionId == sessionId)
                }).ToList()
            };

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> Create(int sessionId)
        {
            var session = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return NotFound();

            var today = DateTime.Today;
            var availableTimes = await _context.Sessions
                .Include(s => s.Schedule)
                .Where(s => s.MovieId == session.MovieId && s.IsActive && s.Schedule.StartTime >= today)
                .Select(s => new TimeOption
                {
                    SessionId = s.Id,
                    StartTime = s.Schedule.StartTime // Selecting the full DateTime
                })
                .OrderBy(to => to.StartTime)
                .ToListAsync();

            var model = new BookingFormModel
            {
                Movie = session.Movie,
                SessionId = sessionId,
                AvailableTimes = availableTimes,
                IsPrivate = false
            };

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetSeatsForSession(int sessionId)
        {
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

            return View(seats);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(BookingFormModel model)
        {
            var user = await _userManager.GetUserAsync(User); // Get the current user
            if (user == null)
                return Unauthorized();

            if (model.SelectedSeatIds == null || !model.SelectedSeatIds.Any())
            {
                ModelState.AddModelError("", "You must select at least one seat.");
                return RedirectToAction("Create", new { sessionId = model.SessionId });
            }

            var booking = new Booking
            {
                SessionId = model.SessionId,
                BookingDate = DateTime.Now,
                IsPrivateBooking = model.IsPrivate,
                NumberOfSeats = model.SelectedSeatIds.Count,
                UserId = user.Id, // Store the current user's ID
                PrivateBookingPrice = model.IsPrivate ? model.PrivateBookingPrice : null
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Update seat bookings
            var seats = await _context.SessionSeats
                .Where(s => model.SelectedSeatIds.Contains(s.Id))
                .ToListAsync();

            foreach (var seat in seats)
            {
                seat.BookingId = booking.Id;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User); // Get the current user
            var bookings = await _context.Bookings
                .Include(b => b.Session)
                .ThenInclude(s => s.Movie)
                .Where(b => b.UserId == user.Id) // Filter by the current user
                .ToListAsync();

            return View(bookings);
        }
    }
}
