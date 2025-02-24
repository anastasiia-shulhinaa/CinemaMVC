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
    public class HallsController : Controller
    {
        private readonly DbcinemaContext _context;

        public HallsController(DbcinemaContext context)
        {
            _context = context;
        }

        // GET: Halls
        public async Task<IActionResult> Index(int? id, string? name)
        {
            if (id == null) return RedirectToAction("Index", "Cinemas");
            ViewBag.CinemaId = id;
            ViewBag.CinemaName = name;
            var hallByCinema = _context.Halls.Where(h => h.CinemaId == id).Include(h => h.Cinema);
            return View(await hallByCinema.ToListAsync());
        }

        // GET: Halls/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hall = await _context.Halls
                .Include(h => h.Cinema)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hall == null)
            {
                return NotFound();
            }

            return View(hall);
        }

        // GET: Halls/Create
        public IActionResult Create(int? cinemaId)
        {
            if (cinemaId == null)
            {
                return RedirectToAction("Index", "Cinemas");
            }
            // Створюємо список кінотеатрів з уже вибраним значенням cinemaId.
            ViewData["CinemaId"] = new SelectList(_context.Cinemas, "Id", "Name", cinemaId);
            return View();
        }

        // POST: Halls/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CinemaId,Name,TotalSeats,Id")] Hall hall)
        {
            // Якщо агрегована сутність Cinema не встановлена, завантажуємо її з бази даних
            if (hall.Cinema == null)
            {
                hall.Cinema = await _context.Cinemas.FirstOrDefaultAsync(c => c.Id == hall.CinemaId);
                // Очищаємо стан моделі і повторно валідуємо модель
                ModelState.Clear();
                TryValidateModel(hall);
            }

            if (ModelState.IsValid)
            {
                _context.Add(hall);
                await _context.SaveChangesAsync();

                // Для редиректу отримуємо назву кінотеатру (або використовуємо вже завантажену Hall.Cinema)
                string cinemaName = hall.Cinema?.Name ?? "";
                return RedirectToAction("Index", new { id = hall.CinemaId, name = cinemaName });
            }

            ViewData["CinemaId"] = new SelectList(_context.Cinemas, "Id", "Name", hall.CinemaId);
            return View(hall);
        }

        // GET: Halls/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hall = await _context.Halls.FindAsync(id);
            if (hall == null)
            {
                return NotFound();
            }
            ViewData["CinemaId"] = new SelectList(_context.Cinemas, "Id", "Address", hall.CinemaId);
            return View(hall);
        }

        // POST: Halls/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CinemaId,Name,TotalSeats,Id")] Hall hall)
        {
            if (id != hall.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hall);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HallExists(hall.Id))
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
            ViewData["CinemaId"] = new SelectList(_context.Cinemas, "Id", "Address", hall.CinemaId);
            return View(hall);
        }

        // GET: Halls/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hall = await _context.Halls
                .Include(h => h.Cinema)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hall == null)
            {
                return NotFound();
            }

            return View(hall);
        }

        // POST: Halls/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hall = await _context.Halls.FindAsync(id);
            if (hall != null)
            {
                _context.Halls.Remove(hall);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HallExists(int id)
        {
            return _context.Halls.Any(e => e.Id == id);
        }
    }
}
