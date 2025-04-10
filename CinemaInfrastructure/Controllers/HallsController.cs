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

            ViewData["HallId"] = hall.Id; // Передаємо в представлення

            return View(hall);
        }

        // This method generates the seats for the hall based on total seats
        private void GenerateSeatsForHall(int hallId, int totalSeats)
        {
            var seats = new List<Seat>();

            // Define the seat layouts
            switch (totalSeats)
            {
                case 43:
                    // 6 rows: 1st row has 6 seats, 2-5 have 7 seats, 6th has 9 seats
                    for (int i = 1; i <= 43; i++)
                    {
                        int rowNumber = (i == 1) ? 1 : (i <= 30 ? (i - 1) / 7 + 1 : 6);
                        seats.Add(new Seat
                        {
                            HallId = hallId,
                            SeatNumber = i,
                            RowNumber = rowNumber
                        });
                    }
                    break;

                case 36:
                    // 5 rows: 1-4 have 7 seats, 5th row has 8 seats
                    for (int i = 1; i <= 36; i++)
                    {
                        int rowNumber = (i <= 28) ? (i - 1) / 7 + 1 : 5;
                        seats.Add(new Seat
                        {
                            HallId = hallId,
                            SeatNumber = i,
                            RowNumber = rowNumber
                        });
                    }
                    break;

                case 24:
                    // 4 rows with 6 seats each
                    for (int i = 1; i <= 24; i++)
                    {
                        int rowNumber = (i - 1) / 6 + 1;
                        seats.Add(new Seat
                        {
                            HallId = hallId,
                            SeatNumber = i,
                            RowNumber = rowNumber
                        });
                    }
                    break;

                default:
                    throw new ArgumentException("Invalid total seat count.");
            }

            _context.Seats.AddRange(seats);
            _context.SaveChanges();
        }

        // GET: Halls/Create
        public IActionResult Create(int? cinemaId)
        {
            if (cinemaId == null)
            {
                return RedirectToAction("Index", "Cinemas");
            }
            // Create a list of cinemas with the already selected cinemaId.
            ViewData["CinemaId"] = new SelectList(_context.Cinemas, "Id", "Name", cinemaId);
            return View();
        }

        // POST: Halls/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CinemaId,Name,TotalSeats,Id")] Hall hall)
        {
            var cinema = await _context.Cinemas.FindAsync(hall.CinemaId);
            hall.Cinema = cinema;

            ModelState.Clear();
            TryValidateModel(hall);

            if (hall.Cinema == null)
            {
                hall.Cinema = await _context.Cinemas.FirstOrDefaultAsync(c => c.Id == hall.CinemaId);
                ModelState.Clear();
                TryValidateModel(hall);
            }

            if (ModelState.IsValid)
            {
                _context.Add(hall);
                await _context.SaveChangesAsync();

                // Generate seats based on the selected total seats
                GenerateSeatsForHall(hall.Id, hall.TotalSeats);

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