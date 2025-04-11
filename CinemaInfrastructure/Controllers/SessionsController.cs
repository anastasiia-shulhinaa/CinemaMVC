using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure.ViewModels;
using System.Security.Claims;
using CinemaInfrastructure.Models;
using Microsoft.AspNetCore.Identity;

namespace CinemaInfrastructure.Controllers
{
    public class SessionsController : Controller
    {
        private readonly DbcinemaContext _context;
        private readonly UserManager<User> _userManager; // Using IdentityUser

        public SessionsController(DbcinemaContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult GetHalls(int cinemaId)
        {
            var halls = _context.Halls
                .Where(h => h.CinemaId == cinemaId)
                .Select(h => new { id = h.Id, name = h.Name })
                .ToList();

            return Json(halls);
        }

        [HttpGet]
        public IActionResult GetSchedules(int hallId)
        {
            var schedules = _context.Schedules
                .Where(s => s.HallId == hallId && s.IsActive) // Only active schedules
                .Select(s => new { id = s.Id, startTime = s.StartTime.ToString("HH:mm") })
                .ToList();
            return Json(schedules);
        }

        // GET: Sessions
        public async Task<IActionResult> Index()
        {
            var sessions = await _context.Sessions
                .Include(s => s.Movie) // Include the related Movie
                .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Hall) // Include Hall details if necessary
                .ToListAsync(); // Fetch all sessions

            return View(sessions); // Pass sessions to the view
        }

        // GET: Sessions/Create
        public IActionResult Create()
        {
            ViewBag.Cinemas = _context.Cinemas.ToList();
            ViewBag.Movies = _context.Movies.ToList(); // Add movies for selection
            ViewBag.Schedules = _context.Schedules.ToList(); // Add schedules for selection
            ViewBag.Halls = _context.Halls.ToList(); // Add halls for selection
            return View();
        }


        // POST: Sessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MovieId, ScheduleId")] Session session, decimal PricePerSeat)
        {
            // Load the Movie
            session.Movie = await _context.Movies.FindAsync(session.MovieId);

            // Load the Schedule, including Hall and its associated Cinema
            session.Schedule = await _context.Schedules
                .Include(s => s.Hall)
                .ThenInclude(h => h.Cinema)
                .FirstOrDefaultAsync(s => s.Id == session.ScheduleId);

            if (session.Movie == null)
            {
                ModelState.AddModelError("MovieId", "The selected movie does not exist.");
            }

            if (session.Schedule == null)
            {
                ModelState.AddModelError("ScheduleId", "The selected schedule does not exist.");
            }

            if (session.Schedule != null && session.Schedule.Hall == null)
            {
                ModelState.AddModelError("ScheduleId", "The selected schedule must have an associated hall.");
            }

            if (PricePerSeat <= 0)
            {
                ModelState.AddModelError("PricePerSeat", "Price must be greater than zero.");
            }

            session.IsActive = true;
            session.CreatedAt = DateTime.UtcNow;

            ModelState.Clear();
            TryValidateModel(session);

            if (!ModelState.IsValid)
            {
                ViewBag.Cinemas = _context.Cinemas.ToList();
                ViewBag.Movies = _context.Movies.ToList();
                ViewBag.Schedules = _context.Schedules.ToList();
                return View(session);
            }

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            var hallId = session.Schedule?.Hall?.Id;

            if (hallId != null)
            {
                var seats = await _context.Seats
                    .Where(seat => seat.HallId == hallId)
                    .ToListAsync();

                var sessionSeats = seats.Select(seat => new SessionSeat
                {
                    SessionId = session.Id,
                    SeatId = seat.Id,
                    Price = PricePerSeat
                    // BookingId remains null, so seat is available
                }).ToList();

                _context.SessionSeats.AddRange(sessionSeats);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> CreateSessionFromMovie(int movieId, int hallId, int scheduleId, bool isPrivate)
        {
            var session = new Session
            {
                MovieId = movieId,
                ScheduleId = scheduleId,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            if (isPrivate)
            {
                var booking = new Booking
                {
                    SessionId = session.Id,
                    IsPrivateBooking = true,
                    BookingDate = DateTime.Now,
                    UserId = _userManager.GetUserId(User) // Replace with actual user
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyBookings");
        }


        // GET: Sessions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }


        // GET: Sessions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions.FindAsync(id);
            if (session == null)
            {
                return NotFound();
            }
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Language", session.MovieId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "Id", "Id", session.ScheduleId);
            return View(session);
        }

        // POST: Sessions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MovieId,ScheduleId,CreatedAt,IsActive,Id")] Session session)
        {
            if (id != session.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(session);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SessionExists(session.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Language", session.MovieId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "Id", "Id", session.ScheduleId);
            return View(session);
        }

        // GET: Sessions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }

        // POST: Sessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session != null)
            {
                _context.Sessions.Remove(session);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SessionExists(int id)
        {
            return _context.Sessions.Any(e => e.Id == id);
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
                .Include(b => b.Session) // Ensure the session is included
                .ThenInclude(s => s.Movie) // Include related movie
                .Include(b => b.SessionSeats)
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
}
