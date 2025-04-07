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
    public class SchedulesController : Controller
    {
        private readonly DbcinemaContext _context;

        public SchedulesController(DbcinemaContext context)
        {
            _context = context;
        }

        // GET: Schedules
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Hall) // Eager load Hall
                .ToListAsync();

            return View(schedules);
        }

        // GET: Schedules/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var schedule = await _context.Schedules
                .Include(s => s.Hall) // Eager load Hall
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
                return NotFound();

            return View(schedule);
        }

        // GET: Schedules/Create
        public IActionResult Create(int? hallId)
        {
            if (hallId == null)
            {
                return RedirectToAction("Index", "Halls"); // Redirect if hallId is null
            }

            var hall = _context.Halls.Find(hallId);
            if (hall == null)
            {
                return NotFound(); // Return 404 if hall not found
            }

            // Create a list of halls with the selected value
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", hallId);

            var schedule = new Schedule
            {
                HallId = hall.Id,
                Hall = hall // Assign Hall so it's not null
            };

            return View(schedule);
        }

        // POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("HallId, IsActive")] Schedule schedule, DateTime startDate, TimeSpan startTime)
        {
            // Load Hall with its Cinema
            schedule.Hall = await _context.Halls.Include(h => h.Cinema).FirstOrDefaultAsync(h => h.Id == schedule.HallId);

            // Check if the Hall exists and if its Cinema is not null
            if (schedule.Hall == null)
            {
                ModelState.AddModelError("HallId", "The selected hall does not exist.");
            }
            else if (schedule.Hall.Cinema == null)
            {
                ModelState.AddModelError("HallId", "The selected hall must have an associated cinema.");
            }

            ModelState.Clear(); // Clear any existing validation errors
            TryValidateModel(schedule); // Re-validate the model

            // Validate the model state
            if (!ModelState.IsValid)
            {
                // Log the validation errors for debugging
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"ModelState Error - Field: {entry.Key}, Error: {error.ErrorMessage}");
                    }
                }

                // Return the view with the current schedule model
                ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", schedule.HallId);
                return View(schedule);
            }

            // Combine the date and time into a single DateTime for StartTime
            schedule.StartTime = startDate.Date.Add(startTime);

            // Check for existing schedules for the same hall
            var existingSchedules = await _context.Schedules
                .Where(s => s.HallId == schedule.HallId && s.IsActive)
                .ToListAsync();

            // Check if the start time is valid
            foreach (var existingSchedule in existingSchedules)
            {
                var existingEndTime = existingSchedule.EndTime;

                // Ensure the new start time is at least 3 hours after the existing schedule's end time
                if (schedule.StartTime < existingEndTime && schedule.StartTime >= existingSchedule.StartTime)
                {
                    ModelState.AddModelError("StartTime", "Start time must be at least 3 hours after the last schedule's end time.");
                    break;
                }
            }

            if (ModelState.IsValid)
            {
                // Create schedules for the next 30 days
                DateTime currentDate = schedule.StartTime.Date;
                for (int i = 0; i < 30; i++)
                {
                    Schedule newSchedule = new Schedule
                    {
                        StartTime = currentDate.Add(schedule.StartTime.TimeOfDay), // Set the StartTime with the same time
                        HallId = schedule.HallId,
                        IsActive = schedule.IsActive
                    };
                    _context.Schedules.Add(newSchedule);
                    currentDate = currentDate.AddDays(1); // Move to the next day
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed; redisplay the form
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", schedule.HallId);
            return View(schedule);
        }

        // GET: Schedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
                return NotFound();

            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", schedule.HallId);
            return View(schedule);
        }

        // POST: Schedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StartTime,HallId,Id")] Schedule schedule)
        {
            if (id != schedule.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Optional: load and assign Hall if needed
                    schedule.Hall = await _context.Halls.FindAsync(schedule.HallId);

                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(schedule.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", schedule.HallId);
            return View(schedule);
        }

        // GET: Schedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var schedule = await _context.Schedules
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
                return NotFound();

            return View(schedule);
        }

        // POST: Schedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.Id == id);
        }
    }
}
