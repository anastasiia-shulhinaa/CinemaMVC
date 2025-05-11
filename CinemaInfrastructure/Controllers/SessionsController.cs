using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure.ViewModels;
using Microsoft.AspNetCore.Identity;
using CinemaInfrastructure.Models;

namespace CinemaInfrastructure.Controllers
{
    public class SessionsController : Controller
    {
        private readonly DbcinemaContext _context;
        private readonly UserManager<User> _userManager;

        public SessionsController(DbcinemaContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Sessions
        public async Task<IActionResult> Index()
        {
            var sessions = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Hall)
                .ThenInclude(h => h.Cinema)
                .ToListAsync();

            ViewBag.Movies = await _context.Movies.Select(m => new { m.Id, m.Title }).ToListAsync();
            ViewBag.Cinemas = await _context.Cinemas.Select(c => new { c.Id, c.Name }).ToListAsync();

            return View(sessions);
        }

        // GET: Sessions/GetHalls
        [HttpGet]
        public IActionResult GetHalls(int cinemaId)
        {
            var halls = _context.Halls
                .Where(h => h.CinemaId == cinemaId)
                .Select(h => new { id = h.Id, name = h.Name })
                .ToList();
            return Json(halls);
        }

        // GET: Sessions/GetSchedules
        [HttpGet]
        public IActionResult GetSchedules(int hallId)
        {
            var schedules = _context.Schedules
                .Where(s => s.HallId == hallId && s.IsActive)
                .Select(s => new { id = s.Id, startTime = s.StartTime.ToString("dd.MM.yyyy HH:mm") })
                .ToList();
            return Json(schedules);
        }

        // GET: Sessions/GetSession
        [HttpGet]
        public async Task<IActionResult> GetSession(int id)
        {
            var session = await _context.Sessions
                .Where(s => s.Id == id)
                .Select(s => new { s.Id, s.IsActive })
                .FirstOrDefaultAsync();
            if (session == null)
            {
                return NotFound();
            }
            return Json(session);
        }

        // POST: Sessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateSessionDto dto)
        {
            if (dto == null || !ModelState.IsValid)
            {
                return BadRequest("Неправильні дані.");
            }

            // Load the Movie
            var movie = await _context.Movies.FindAsync(dto.MovieId);
            if (movie == null)
            {
                return BadRequest("Вибраний фільм не існує.");
            }

            // Load the Schedule, including Hall and its associated Cinema
            var schedule = await _context.Schedules
                .Include(s => s.Hall)
                .ThenInclude(h => h.Cinema)
                .FirstOrDefaultAsync(s => s.Id == dto.ScheduleId);
            if (schedule == null || !schedule.IsActive)
            {
                return BadRequest("Вибраний розклад недійсний або неактивний.");
            }

            // Check for overlapping sessions in the same hall
            var scheduleEndTime = schedule.StartTime.AddHours(3);
            var conflictingSessions = await _context.Sessions
                .Include(s => s.Schedule)
                .Where(s => s.Schedule.HallId == schedule.HallId && s.Schedule.IsActive)
                .Where(s => s.Schedule.StartTime < scheduleEndTime && s.Schedule.StartTime.AddHours(3) > schedule.StartTime)
                .AnyAsync();
            if (conflictingSessions)
            {
                return BadRequest("Цей розклад конфліктує з іншим сеансом у цьому залі.");
            }

            if (dto.PricePerSeat <= 0)
            {
                return BadRequest("Ціна за місце повинна бути більше нуля.");
            }

            var session = new Session
            {
                MovieId = dto.MovieId,
                ScheduleId = dto.ScheduleId,
                CreatedAt = DateTime.UtcNow,
                IsActive = dto.IsActive
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Create SessionSeats
            var seats = await _context.Seats
                .Where(seat => seat.HallId == schedule.HallId)
                .ToListAsync();

            var sessionSeats = seats.Select(seat => new SessionSeat
            {
                SessionId = session.Id,
                SeatId = seat.Id,
                Price = dto.PricePerSeat
            }).ToList();

            _context.SessionSeats.AddRange(sessionSeats);
            await _context.SaveChangesAsync();

            return Json(new { id = session.Id, createdAt = session.CreatedAt.ToString("o"), isActive = session.IsActive });
        }

        // POST: Sessions/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromBody] EditSessionDto dto)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null)
            {
                return NotFound("Сеанс не знайдено.");
            }

            session.IsActive = dto.IsActive;
            _context.Update(session);
            await _context.SaveChangesAsync();

            return Json(new { id = session.Id, isActive = session.IsActive });
        }

        // POST: Sessions/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.SessionSeats)
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (session == null)
            {
                return NotFound("Сеанс не знайдено.");
            }

            if (session.Bookings.Any())
            {
                return BadRequest("Неможливо видалити сеанс, оскільки на нього є бронювання.");
            }

            _context.SessionSeats.RemoveRange(session.SessionSeats);
            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // View to create a session for a selected movie
        public async Task<IActionResult> CreateSessionFromMovie(int movieId)
        {
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null) return NotFound();

            var cinemas = await _context.Cinemas
                .Include(c => c.Halls)
                .ToListAsync();

            var viewModel = new MovieWithSessionViewModel
            {
                Movie = movie,
                Cinemas = cinemas
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSessionFromMovie(int movieId, int hallId, int scheduleId, bool isPrivate, decimal pricePerSeat)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == scheduleId && s.HallId == hallId && s.IsActive);
            if (schedule == null)
            {
                return BadRequest("Вибраний розклад недійсний або неактивний.");
            }

            // Check for overlapping sessions
            var scheduleEndTime = schedule.StartTime.AddHours(3);
            var conflictingSessions = await _context.Sessions
                .Include(s => s.Schedule)
                .Where(s => s.Schedule.HallId == hallId && s.Schedule.IsActive)
                .Where(s => s.Schedule.StartTime < scheduleEndTime && s.Schedule.StartTime.AddHours(3) > schedule.StartTime)
                .AnyAsync();
            if (conflictingSessions)
            {
                return BadRequest("Цей розклад конфліктує з іншим сеансом у цьому залі.");
            }

            if (pricePerSeat <= 0)
            {
                return BadRequest("Ціна за місце повинна бути більше нуля.");
            }

            var session = new Session
            {
                MovieId = movieId,
                ScheduleId = scheduleId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Create SessionSeats
            var seats = await _context.Seats
                .Where(seat => seat.HallId == hallId)
                .ToListAsync();

            var sessionSeats = seats.Select(seat => new SessionSeat
            {
                SessionId = session.Id,
                SeatId = seat.Id,
                Price = pricePerSeat
            }).ToList();

            _context.SessionSeats.AddRange(sessionSeats);

            if (isPrivate)
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var booking = new Booking
                {
                    SessionId = session.Id,
                    IsPrivateBooking = true,
                    PrivateBookingPrice = 1200,
                    BookingDate = DateTime.UtcNow,
                    UserId = userId
                };

                _context.Bookings.Add(booking);

                // Mark all seats as booked
                foreach (var sessionSeat in sessionSeats)
                {
                    sessionSeat.BookingId = booking.Id;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MyBookings");
        }

        // My Bookings Action
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _context.Bookings
                .Include(b => b.Session)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Hall)
                            .ThenInclude(h => h.Cinema)
                .Include(b => b.SessionSeats)
                    .ThenInclude(ss => ss.Seat)
                .Where(b => b.UserId == userId)
                .ToListAsync();

            var model = new MyBookingsViewModel
            {
                PrivateBookings = bookings.Where(b => b.IsPrivateBooking).ToList(),
                TicketBookings = bookings.Where(b => !b.IsPrivateBooking).ToList()
            };

            return View(model);
        }
    }

    public class CreateSessionDto
    {
        public int MovieId { get; set; }
        public int ScheduleId { get; set; }
        public decimal PricePerSeat { get; set; }
        public bool IsActive { get; set; }
    }

    public class EditSessionDto
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }

    public class MovieWithSessionViewModel
    {
        public Movie Movie { get; set; }
        public List<Cinema> Cinemas { get; set; }
    }

}