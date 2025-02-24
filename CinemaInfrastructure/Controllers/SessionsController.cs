using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure.ViewModels;

namespace CinemaInfrastructure.Controllers
{
    public class SessionsController : Controller
    {
        private readonly DbcinemaContext _context;

        public SessionsController(DbcinemaContext context)
        {
            _context = context;
        }

        public IActionResult GetHallsByCinema(int cinemaId)
        {
            var halls = _context.Halls
                .Where(h => h.CinemaId == cinemaId)
                .Select(h => new { id = h.Id, name = h.Name })
                .ToList();
            return Json(halls);
        }

        public IActionResult GetSchedulesByHall(int hallId)
        {
            var schedules = _context.Schedules
                .Where(s => s.HallId == hallId)
                .Select(s => new { id = s.Id, startTime = s.StartTime.ToString("HH:mm") })
                .ToList();
            return Json(schedules);
        }

        // GET: Sessions
        public async Task<IActionResult> Index()
        {
            var sessions = _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Hall)
                .ThenInclude(hall => hall.Cinema)
                .ToList();
            return View(sessions);
        }

        // ========== 📌 SESSION CREATION (USERS & ADMINS) ==========
        // GET: Sessions/Create
        public IActionResult Create()
        {
            ViewData["Movies"] = new SelectList(_context.Movies, "Id", "Title");
            ViewData["Cinemas"] = new SelectList(_context.Cinemas, "Id", "Name");
            return View();
        }

        // POST: Sessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MovieId, ScheduleId, IsPrivate, IsActive")] Session session)
        {
            if (ModelState.IsValid)
            {
                session.CreatedAt = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());

                _context.Add(session);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["Movies"] = new SelectList(_context.Movies, "Id", "Title", session.MovieId);
            ViewData["Cinemas"] = new SelectList(_context.Cinemas, "Id", "Name");
            return View(session);
        }

        // User Create Session
        // GET: Display the initial form
        public IActionResult CreateUser()
        {
            var model = new CreateSessionViewModel
            {
                Movies = _context.Movies.ToList(),   // Отримати список фільмів з БД
                Cinemas = _context.Cinemas.ToList(), // Отримати список кінотеатрів з БД
                Halls = new List<Hall>(),            // Порожній список, оновиться при виборі кінотеатру
                Schedules = new List<Schedule>()     // Так само порожній список
            };

            model.Movies = _context.Movies.ToList();
            model.Cinemas = _context.Cinemas.ToList();
            model.Halls = model.Halls ?? new List<Hall>();
            model.Schedules = model.Schedules ?? new List<Schedule>();

            return View(model);
        }


        [HttpPost]
        public IActionResult CreateUser(CreateSessionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdown lists in case of validation errors
                model.Movies = _context.Movies.ToList();
                model.Cinemas = _context.Cinemas.ToList();
                model.Halls = _context.Halls.Where(h => h.CinemaId == model.SelectedCinemaId).ToList();
                model.Schedules = _context.Schedules.Where(s => s.HallId == model.SelectedHallId).ToList();
                return View(model);
            }

            var session = new Session
            {
                MovieId = model.SelectedMovieId.Value,
                ScheduleId = model.SelectedScheduleId.Value,
                IsActive = BitConverter.GetBytes(true)
            };

            _context.Sessions.Add(session);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }




        // Admin Create Session
        public IActionResult CreateAdmin()
        {
            ViewBag.Movies = new SelectList(_context.Movies, "Id", "Title");
            ViewBag.Cinemas = new SelectList(_context.Cinemas, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdmin(Session session)
        {
            if (ModelState.IsValid)
            {
                session.CreatedAt = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(session);
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
    }
}
