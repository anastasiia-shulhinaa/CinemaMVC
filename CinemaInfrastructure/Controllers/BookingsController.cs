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
        private readonly UserManager<User> _userManager;

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
                .Include(s => s.SessionSeats)
                .FirstOrDefault(s => s.Id == sessionId);

            if (session == null)
                return NotFound();

            var viewModel = new BookingFormModel
            {
                SessionId = sessionId,
                SessionSeats = session.SessionSeats.Select(ss => new SeatViewModel
                {
                    Id = ss.Id,
                    RowNumber = ss.Seat.RowNumber,
                    SeatNumber = ss.Seat.SeatNumber,
                    Price = ss.Price,
                    IsAvailable = ss.BookingId == null
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
                .ThenInclude(sch => sch.Hall)
                .ThenInclude(h => h.Cinema)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return NotFound();

            string? selectedCinemaId = HttpContext.Session.GetString("SelectedCinemaId");
            int? cinemaId = string.IsNullOrEmpty(selectedCinemaId) || !int.TryParse(selectedCinemaId, out int parsedId) ? null : parsedId;

            var today = DateTime.Today;
            var sessionsQuery = _context.Sessions
                .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Hall)
                .ThenInclude(h => h.Cinema)
                .Where(s => s.MovieId == session.MovieId && s.IsActive && s.Schedule.StartTime >= today);

            // Apply cinema filter if a cinema is selected
            if (cinemaId.HasValue && cinemaId != 0)
            {
                sessionsQuery = sessionsQuery.Where(s => s.Schedule.Hall.CinemaId == cinemaId.Value);
            }

            var availableSessions = await sessionsQuery
                .Select(s => new
                {
                    s.Id,
                    s.Schedule.StartTime,
                    Cinema = s.Schedule.Hall.Cinema
                })
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            // Group sessions by cinema if no cinema is selected
            var groupedTimes = cinemaId.HasValue && cinemaId != 0
                ? null
                : availableSessions
                    .GroupBy(s => s.Cinema)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(s => new TimeOption
                        {
                            SessionId = s.Id,
                            StartTime = s.StartTime
                        }).ToList()
                    );

            var availableTimes = cinemaId.HasValue && cinemaId != 0
                ? availableSessions.Select(s => new TimeOption
                {
                    SessionId = s.Id,
                    StartTime = s.StartTime
                }).ToList()
                : availableSessions.Select(s => new TimeOption
                {
                    SessionId = s.Id,
                    StartTime = s.StartTime
                }).ToList();

            var model = new BookingFormModel
            {
                Movie = session.Movie,
                SessionId = sessionId,
                AvailableTimes = availableTimes,
                GroupedTimes = groupedTimes, // Dictionary of cinema-wise times if no cinema is selected
                IsPrivate = false
            };

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSeatsForSession(int sessionId)
        {
            var session = await _context.Sessions
                .Include(s => s.SessionSeats)
                    .ThenInclude(ss => ss.Seat)
                .Include(s => s.Schedule)
                    .ThenInclude(sch => sch.Hall)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return NotFound("Session not found.");
            }

            int totalSeats = session.Schedule.Hall.TotalSeats;
            int[] layoutRows;

            switch (totalSeats)
            {
                case 43:
                    layoutRows = new[] { 8, 8, 8, 6, 6 };
                    break;
                case 36:
                    layoutRows = new[] { 6, 6, 6, 6, 6, 6 };
                    break;
                case 24:
                    layoutRows = new[] { 6, 6, 6, 6 };
                    break;
                default:
                    layoutRows = new[] { 5, 5 };
                    break;
            }

            var seats = session.SessionSeats
                .OrderBy(ss => ss.Seat.RowNumber)
                .ThenBy(ss => ss.Seat.SeatNumber)
                .Select(ss => new SeatViewModel
                {
                    Id = ss.Id,
                    RowNumber = ss.Seat.RowNumber,
                    SeatNumber = ss.Seat.SeatNumber,
                    Price = ss.Price,
                    IsAvailable = ss.BookingId == null
                })
                .ToList();

            return PartialView("_UserSessionSeats", new UserSessionSeatsViewModel
            {
                Seats = seats,
                LayoutRows = layoutRows
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(BookingFormModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (string.IsNullOrEmpty(model.SelectedSeats))
            {
                ModelState.AddModelError("", "You must select at least one seat.");
                return RedirectToAction("Create", new { sessionId = model.SessionId });
            }

            var selectedSeatNumbers = System.Text.Json.JsonSerializer.Deserialize<List<string>>(model.SelectedSeats);
            if (selectedSeatNumbers == null || !selectedSeatNumbers.Any())
            {
                ModelState.AddModelError("", "You must select at least one seat.");
                return RedirectToAction("Create", new { sessionId = model.SessionId });
            }

            var sessionSeats = await _context.SessionSeats
                .Include(ss => ss.Seat)
                .Where(ss => ss.SessionId == model.SessionId)
                .ToListAsync();

            var selectedSessionSeats = sessionSeats
                .Where(ss => selectedSeatNumbers.Contains($"{ss.Seat.RowNumber}-{ss.Seat.SeatNumber}"))
                .ToList();

            if (selectedSessionSeats.Count != selectedSeatNumbers.Count)
            {
                ModelState.AddModelError("", "Some selected seats are invalid or unavailable.");
                return RedirectToAction("Create", new { sessionId = model.SessionId });
            }

            if (selectedSessionSeats.Any(ss => ss.BookingId != null))
            {
                ModelState.AddModelError("", "Some selected seats are already booked.");
                return RedirectToAction("Create", new { sessionId = model.SessionId });
            }

            var booking = new Booking
            {
                SessionId = model.SessionId,
                BookingDate = DateTime.UtcNow,
                IsPrivateBooking = model.IsPrivate,
                NumberOfSeats = selectedSessionSeats.Count,
                UserId = user.Id,
                PrivateBookingPrice = model.IsPrivate ? model.PrivateBookingPrice : null
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            foreach (var seat in selectedSessionSeats)
            {
                seat.BookingId = booking.Id;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            var bookings = await _context.Bookings
                .Include(b => b.Session)
                .ThenInclude(s => s.Movie)
                .Where(b => b.UserId == user.Id)
                .ToListAsync();

            return View(bookings);
        }
    }
}