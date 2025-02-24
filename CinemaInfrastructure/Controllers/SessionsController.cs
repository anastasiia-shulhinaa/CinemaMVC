using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;

namespace CinemaInfrastructure.Controllers
{
    public class SessionsController : Controller
    {
        private readonly DbcinemaContext _context;

        public SessionsController(DbcinemaContext context)
        {
            _context = context;
        }

        // Select Cinema
        public IActionResult SelectCinema()
        {
            var cinemas = _context.Cinemas.ToList();

            if (cinemas == null || !cinemas.Any())
            {
                return PartialView("_SelectTheater", new List<Cinema>()); 
            }

            return PartialView("_SelectTheater", cinemas);
        }

        // Load Halls for Selected Cinema
        public async Task<IActionResult> SelectHall(int cinemaId)
        {
            var halls = await _context.Halls.Where(h => h.CinemaId == cinemaId).ToListAsync();
            return PartialView("_SelectHall", halls);
        }

        // Load Available Times for Selected Hall
        public async Task<IActionResult> SelectTime(int hallId)
        {
            var schedules = await _context.Schedules.Where(s => s.HallId == hallId).ToListAsync();
            return PartialView("_SelectTime", schedules);
        }

        // Select type of Booking (private or public)
        public IActionResult SelectBookingType()
        {
            return PartialView("_SelectBookingType");
        }

        // GET: Sessions
        public async Task<IActionResult> Index()
        {
            var dbcinemaContext = _context.Sessions.Include(s => s.Movie).Include(s => s.Schedule);
            return View(await dbcinemaContext.ToListAsync());
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

        // GET: Sessions/Create
        public IActionResult Create()
        {
            var cinemas = _context.Cinemas.ToList();
            return View(cinemas);
        }

        // POST: Sessions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MovieId,ScheduleId,CreatedAt,IsActive,Id")] Session session)
        {
            if (ModelState.IsValid)
            {
                _context.Add(session);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Language", session.MovieId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "Id", "Id", session.ScheduleId);
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
