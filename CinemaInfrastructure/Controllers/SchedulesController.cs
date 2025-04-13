using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

        // GET: Schedules/Index
        public async Task<IActionResult> Index(int? hallId)
        {
            var halls = await _context.Halls
                .Include(h => h.Cinema)
                .Select(h => new { h.Id, Name = h.Name, CinemaName = h.Cinema.Name })
                .ToListAsync();
            ViewBag.Halls = halls;
            ViewBag.SelectedHallId = hallId;
            return View(new List<Schedule>());
        }

        // GET: Schedules/GetSchedules
        [HttpGet]
        public async Task<IActionResult> GetSchedules(int hallId)
        {
            var schedules = await _context.Schedules
                .Where(s => s.HallId == hallId)
                .Select(s => new
                {
                    id = s.Id,
                    title = s.StartTime.ToString("HH:mm"),
                    start = s.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = s.StartTime.AddHours(3).ToString("yyyy-MM-ddTHH:mm:ss"), // Compute EndTime here
                    isActive = s.IsActive
                })
                .ToListAsync();
            return Json(schedules);
        }

        // POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateScheduleDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.HallId) || !dto.StartTimes.Any())
            {
                return BadRequest("Неправильні дані: виберіть зал і часи.");
            }

            if (!int.TryParse(dto.HallId, out int hallId))
            {
                return BadRequest("Неправильний ID залу.");
            }

            var hall = await _context.Halls.Include(h => h.Cinema).FirstOrDefaultAsync(h => h.Id == hallId);
            if (hall == null || hall.Cinema == null)
            {
                return BadRequest("Зал або пов’язаний кінотеатр не знайдено.");
            }

            var schedules = new List<Schedule>();
            var today = DateTime.Today;
            foreach (var timeStr in dto.StartTimes)
            {
                if (!TimeSpan.TryParse(timeStr, out var time))
                {
                    continue;
                }

                for (int i = 0; i < 30; i++)
                {
                    var startDateTime = today.AddDays(i).Add(time);
                    var endDateTime = startDateTime.AddHours(3);

                    // Check for overlaps: new schedule's range must not intersect with existing schedules
                    var existingSchedules = await _context.Schedules
                        .Where(s => s.HallId == hallId && s.IsActive)
                        .Where(s => s.StartTime < endDateTime && s.StartTime.AddHours(3) > startDateTime)
                        .ToListAsync();

                    if (!existingSchedules.Any())
                    {
                        schedules.Add(new Schedule
                        {
                            HallId = hallId,
                            StartTime = startDateTime,
                            IsActive = dto.IsActive
                        });
                    }
                }
            }

            if (schedules.Any())
            {
                _context.Schedules.AddRange(schedules);
                await _context.SaveChangesAsync();
                return Ok();
            }

            return BadRequest("Не вдалося створити розклад: конфлікт часу з іншими сеансами.");
        }

        // POST: Schedules/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromBody] EditScheduleDto dto)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound("Сеанс не знайдено.");
            }

            if (!TimeSpan.TryParse(dto.StartTime, out var time))
            {
                return BadRequest("Неправильний формат часу.");
            }

            var newStartTime = schedule.StartTime.Date.Add(time);
            var newEndTime = newStartTime.AddHours(3);

            // Check for overlaps excluding the current schedule
            var existingSchedules = await _context.Schedules
                .Where(s => s.HallId == schedule.HallId && s.Id != id && s.IsActive)
                .Where(s => s.StartTime < newEndTime && s.StartTime.AddHours(3) > newStartTime)
                .ToListAsync();

            if (existingSchedules.Any())
            {
                return BadRequest("Новий час конфліктує з іншими сеансами.");
            }

            schedule.StartTime = newStartTime;
            schedule.IsActive = dto.IsActive;

            _context.Update(schedule);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: Schedules/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound("Сеанс не знайдено.");
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class CreateScheduleDto
    {
        public string HallId { get; set; }
        public List<string> StartTimes { get; set; }
        public bool IsActive { get; set; }
    }

    public class EditScheduleDto
    {
        public string StartTime { get; set; }
        public bool IsActive { get; set; }
    }
}