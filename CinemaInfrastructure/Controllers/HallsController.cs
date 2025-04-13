using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using CinemaInfrastructure;
using CinemaInfrastructure.ViewModels;

namespace CinemaInfrastructure.Controllers
{
    public class HallsController : Controller
    {
        private readonly DbcinemaContext _context;

        public HallsController(DbcinemaContext context)
        {
            _context = context;
        }

        // This method generates the seats for the hall based on total seats
        private void GenerateSeatsForHall(int hallId, int totalSeats)
        {
            var seats = new List<Seat>();

            switch (totalSeats)
            {
                case 43:
                    for (int i = 1; i <= 43; i++)
                    {
                        int rowNumber;
                        if (i <= 6) rowNumber = 1;           // Row 1: Seats 1-6 (6 seats)
                        else if (i <= 13) rowNumber = 2;     // Row 2: Seats 7-13 (7 seats)
                        else if (i <= 20) rowNumber = 3;     // Row 3: Seats 14-20 (7 seats)
                        else if (i <= 27) rowNumber = 4;     // Row 4: Seats 21-27 (7 seats)
                        else if (i <= 34) rowNumber = 5;     // Row 5: Seats 28-34 (7 seats)
                        else rowNumber = 6;                  // Row 6: Seats 35-43 (9 seats)

                        seats.Add(new Seat
                        {
                            HallId = hallId,
                            SeatNumber = i,
                            RowNumber = rowNumber
                        });
                    }
                    break;

                case 36:
                    for (int i = 1; i <= 36; i++)
                    {
                        int rowNumber = (i <= 28) ? (i - 1) / 7 + 1 : 5; // Rows 1-4: 7 seats each, Row 5: 8 seats
                        seats.Add(new Seat
                        {
                            HallId = hallId,
                            SeatNumber = i,
                            RowNumber = rowNumber
                        });
                    }
                    break;

                case 24:
                    for (int i = 1; i <= 24; i++)
                    {
                        int rowNumber = (i - 1) / 6 + 1; // 4 rows, 6 seats each
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

        private IEnumerable<HallSeatPreviewViewModel> GeneratePreviewSeats(int totalSeats)
        {
            var seats = new List<HallSeatPreviewViewModel>();

            switch (totalSeats)
            {
                case 43:
                    for (int i = 1; i <= 43; i++)
                    {
                        int rowNumber;
                        if (i <= 6) rowNumber = 1;           // Row 1: Seats 1-6 (6 seats)
                        else if (i <= 13) rowNumber = 2;     // Row 2: Seats 7-13 (7 seats)
                        else if (i <= 20) rowNumber = 3;     // Row 3: Seats 14-20 (7 seats)
                        else if (i <= 27) rowNumber = 4;     // Row 4: Seats 21-27 (7 seats)
                        else if (i <= 34) rowNumber = 5;     // Row 5: Seats 28-34 (7 seats)
                        else rowNumber = 6;                  // Row 6: Seats 35-43 (9 seats)

                        seats.Add(new HallSeatPreviewViewModel
                        {
                            Id = i,
                            SeatNumber = i,
                            RowNumber = rowNumber
                        });
                    }
                    break;

                case 36:
                    for (int i = 1; i <= 36; i++)
                    {
                        int rowNumber = (i <= 28) ? (i - 1) / 7 + 1 : 5; // Rows 1-4: 7 seats each, Row 5: 8 seats
                        seats.Add(new HallSeatPreviewViewModel
                        {
                            Id = i,
                            SeatNumber = i,
                            RowNumber = rowNumber
                        });
                    }
                    break;

                case 24:
                    for (int i = 1; i <= 24; i++)
                    {
                        int rowNumber = (i - 1) / 6 + 1; // 4 rows, 6 seats each
                        seats.Add(new HallSeatPreviewViewModel
                        {
                            Id = i,
                            SeatNumber = i,
                            RowNumber = rowNumber
                        });
                    }
                    break;

                default:
                    throw new ArgumentException("Invalid total seat count.");
            }

            return seats;
        }

        public IActionResult GetHallPreview(int totalSeats)
        {
            if (!new[] { 24, 36, 43 }.Contains(totalSeats))
            {
                return Content("<p class='text-danger'>Невідома кількість місць.</p>");
            }

            var previewSeats = GeneratePreviewSeats(totalSeats);
            if (previewSeats == null)
            {
                return Content("<p class='text-danger'>Помилка генерації схеми залу.</p>");
            }

            string partialViewName = totalSeats switch
            {
                43 => "_Layout43",
                36 => "_Layout36",
                24 => "_Layout24",
                _ => throw new ArgumentException("Invalid total seat count.")
            };

            return PartialView(partialViewName, previewSeats);
        }

        // GET: Halls/Create
        public IActionResult Create(int? cinemaId)
        {
            if (cinemaId == null)
            {
                return RedirectToAction("Index", "Cinemas");
            }

            // Fetch the cinema to check the hall limit
            var cinema = _context.Cinemas.FirstOrDefault(c => c.Id == cinemaId);
            if (cinema == null)
            {
                return RedirectToAction("Index", "Cinemas");
            }

            // Check the current number of halls for this cinema
            int currentHallCount = _context.Halls.Count(h => h.CinemaId == cinemaId);
            if (currentHallCount >= cinema.TotalCinemaHalls) // Updated to use TotalCinemaHalls
            {
                TempData["Notification"] = $"Ви не можете додати більше залів. Максимальна кількість для цього кінотеатру: {cinema.TotalCinemaHalls}.";
                return RedirectToAction("Index", "Cinemas");
            }

            var hall = new Hall { CinemaId = cinemaId.Value, TotalSeats = 24 };
            var previewSeats = GeneratePreviewSeats(hall.TotalSeats);
            ViewData["CinemaId"] = new SelectList(_context.Cinemas, "Id", "Name", cinemaId);
            ViewData["PreviewSeats"] = previewSeats;
            return View(hall);
        }

        // POST: Halls/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CinemaId,Name,TotalSeats,Id")] Hall hall)
        {
            // Fetch the Cinema to validate CinemaId
            var cinema = await _context.Cinemas.FindAsync(hall.CinemaId);
            if (cinema == null)
            {
                ModelState.AddModelError("CinemaId", "Кінотеатр не знайдено.");
            }
            else
            {
                hall.Cinema = cinema;
            }

            // Validate TotalSeats
            if (!new[] { 24, 36, 43 }.Contains(hall.TotalSeats))
            {
                ModelState.AddModelError("TotalSeats", "Кількість місць має бути 24, 36 або 43.");
            }

            // Clear validation state for Cinema property since it's not part of the form submission
            if (ModelState.ContainsKey("Cinema"))
            {
                ModelState["Cinema"].Errors.Clear();
                ModelState["Cinema"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
            }

            // Log all validation errors for debugging
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"Validation error for {state.Key}: {error.ErrorMessage}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(hall);
                await _context.SaveChangesAsync();
                GenerateSeatsForHall(hall.Id, hall.TotalSeats);
                return RedirectToAction("Index", "Cinemas");
            }

            ViewData["CinemaId"] = new SelectList(_context.Cinemas, "Id", "Name", hall.CinemaId);
            ViewData["PreviewSeats"] = GeneratePreviewSeats(hall.TotalSeats);
            return View(hall);
        }

        // GET: Halls/Schedule/5
        public async Task<IActionResult> Schedule(int? id)
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

            // Placeholder: Redirect to Cinemas/Index with a message
            TempData["Notification"] = "Функціонал розкладу для залу ще не реалізовано.";
            return RedirectToAction("Index", "Cinemas");
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
                return RedirectToAction("Index", "Cinemas");
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
            return RedirectToAction("Index", "Cinemas");
        }

        private bool HallExists(int id)
        {
            return _context.Halls.Any(e => e.Id == id);
        }
    }
}