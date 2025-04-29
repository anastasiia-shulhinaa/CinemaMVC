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

        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Session)
                .ThenInclude(s => s.Movie)
                .Include(b => b.Session)
                .ThenInclude(s => s.Schedule) // Include Schedule
                .Include(b => b.SessionSeats)
                .ToListAsync();

            // Create a view model to include user email, calculated price, and session date
            var bookingViewModels = new List<BookingViewModel>();
            foreach (var booking in bookings)
            {
                var user = await _userManager.FindByIdAsync(booking.UserId);

                // Calculate price for non-private bookings by summing SessionSeats.Price
                decimal calculatedPrice = 0;
                if (!booking.IsPrivateBooking && booking.SessionSeats != null && booking.SessionSeats.Any())
                {
                    calculatedPrice = booking.SessionSeats.Sum(ss => ss.Price);
                }

                bookingViewModels.Add(new BookingViewModel
                {
                    Id = booking.Id,
                    SessionId = booking.SessionId,
                    MovieTitle = booking.Session?.Movie?.Title ?? "Невідомий фільм", // Null check
                    NumberOfSeats = booking.NumberOfSeats,
                    IsPrivateBooking = booking.IsPrivateBooking,
                    PrivateBookingPrice = booking.PrivateBookingPrice,
                    CalculatedPrice = calculatedPrice,
                    UserEmail = user?.Email ?? "Невідомий",
                    BookingDate = booking.BookingDate,
                    SessionDate = booking.Session?.Schedule?.StartTime ?? default(DateTime) // Null check with default
                });
            }

            return View(bookingViewModels);
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

            return RedirectToAction("MyBookings", "Sessions");
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


        [HttpGet]
        [Route("Bookings/GetBooking/{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            return Json(new { id = booking.Id, isPrivateBooking = booking.IsPrivateBooking, privateBookingPrice = booking.PrivateBookingPrice });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            var model = new BookingEditModel
            {
                Id = booking.Id,
                IsPrivateBooking = booking.IsPrivateBooking,
                PrivateBookingPrice = booking.PrivateBookingPrice
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BookingEditModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var booking = await _context.Bookings.FindAsync(model.Id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.IsPrivateBooking = model.IsPrivateBooking;
            booking.PrivateBookingPrice = model.IsPrivateBooking ? model.PrivateBookingPrice : null;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Route("Bookings/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.SessionSeats) // Include SessionSeats to delete them
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    return NotFound(new { message = $"Booking with ID {id} not found." });
                }

                // Delete associated SessionSeats
                if (booking.SessionSeats != null && booking.SessionSeats.Any())
                {
                    _context.SessionSeats.RemoveRange(booking.SessionSeats);
                }

                // Delete the Booking
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Booking deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting booking ID {id}: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }

                return StatusCode(500, new { message = "An error occurred while deleting the booking.", error = ex.Message, details = ex.InnerException?.Message ?? "No inner exception." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadTicket(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Session)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sh => sh.Hall)
                            .ThenInclude(h => h.Cinema)
                .Include(b => b.SessionSeats)
                    .ThenInclude(ss => ss.Seat)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return NotFound();

            var pdfBytes = await TicketGenerator.GenerateBookingTicket(booking);

            return File(pdfBytes, "application/pdf", $"Ticket_Booking_{booking.Id}.pdf");
        }
    }

    public class BookingViewModel
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string MovieTitle { get; set; }
        public int? NumberOfSeats { get; set; }
        public bool IsPrivateBooking { get; set; }
        public decimal? PrivateBookingPrice { get; set; }
        public decimal PricePerSeat { get; set; } // For calculating price
        public decimal CalculatedPrice { get; set; } // For non-private bookings
        public string UserEmail { get; set; }
        public DateTime BookingDate { get; set; }
        public DateTime SessionDate { get; set; } // Added for chart
    }

    public class BookingEditModel
    {
        public int Id { get; set; }
        public bool IsPrivateBooking { get; set; }
        public decimal? PrivateBookingPrice { get; set; }
    }
}