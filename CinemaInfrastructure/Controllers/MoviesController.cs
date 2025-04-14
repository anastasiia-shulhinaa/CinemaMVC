using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using X.PagedList;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using CinemaInfrastructure.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CinemaInfrastructure.Models;

namespace CinemaInfrastructure
{
    public class MoviesController : Controller
    {
        private readonly DbcinemaContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<User> _userManager;
        public DbSet<HallSchedule> HallSchedules { get; set; }
        public MoviesController(DbcinemaContext context, IWebHostEnvironment webHostEnvironment, UserManager<User> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        // GET: Movies by Category \ Release year \ Rating \ Title
        [HttpGet]
        public async Task<IActionResult> Filter(string category, int? year, double? rating, string title, int? page)
        {
            int pageSize = 18; // 6 movies per row, 3 rows per page
            int pageNumber = page ?? 1;

            // Start with the query and include Categories for filtering.
            var moviesQuery = _context.Movies
                .Include(m => m.Categories)
                .AsQueryable();

            // Apply filters that can run in the database.
            if (!string.IsNullOrEmpty(category))
            {
                moviesQuery = moviesQuery.Where(m => m.Categories.Any(c => c.Name == category));
            }
            if (year.HasValue)
            {
                moviesQuery = moviesQuery.Where(m => m.ReleaseYear == year.Value);
            }
            if (!string.IsNullOrEmpty(title))
            {
                moviesQuery = moviesQuery.Where(m => m.Title.Contains(title));
            }

            List<Movie> filteredMovies;
            if (rating.HasValue)
            {
                // If rating filter is provided, we need to filter in memory.
                // First, fetch all movies matching the other filters.
                var moviesList = await moviesQuery.OrderBy(m => m.Title).ToListAsync();
                // Then filter by rating.
                filteredMovies = moviesList.Where(m =>
                    !string.IsNullOrEmpty(m.Rating) &&
                    double.TryParse(m.Rating, out double movieRating) &&
                    movieRating >= rating.Value).ToList();
            }
            else
            {
                // Otherwise, just fetch the movies ordered by title.
                filteredMovies = await moviesQuery.OrderBy(m => m.Title).ToListAsync();
            }

            // Total count after filtering.
            int totalMovies = filteredMovies.Count;

            // Apply pagination on the filtered list.
            var moviesPage = filteredMovies
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Wrap the result in a StaticPagedList to implement IPagedList<Movie>.
            var pagedMovies = new StaticPagedList<Movie>(moviesPage, pageNumber, pageSize, totalMovies);

            // Reload filter options (if needed) for the view.
            ViewBag.Categories = await _context.Categories
                .Select(c => c.Name)
                .Distinct()
                .ToListAsync();

            ViewBag.Years = await _context.Movies
                .Select(m => m.ReleaseYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            ViewBag.Ratings = await _context.Movies
                .Where(m => !string.IsNullOrEmpty(m.Rating))
                .Select(m => m.Rating)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            return PartialView("_MoviesListPartial", pagedMovies);
        }
        // GET: Movies by Category \ Release year \ Rating 
        // Об'єднаний метод для відображення фільтрованого списку фільмів з пагінацією
        public async Task<IActionResult> Index(
            string category,
            int? year,
            double? rating,
            string title,
            int? page)
        {

            int pageSize = 18; // 6 фільмів в ряд, 3 рядки на сторінці
            int pageNumber = page ?? 1;

            // Формуємо запит із врахуванням фільтрів
            var moviesQuery = _context.Movies
                .Include(m => m.Categories)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                moviesQuery = moviesQuery.Where(m => m.Categories.Any(c => c.Name == category));
            }
            if (year.HasValue)
            {
                moviesQuery = moviesQuery.Where(m => m.ReleaseYear == year.Value);
            }
            if (!string.IsNullOrEmpty(title))
            {
                moviesQuery = moviesQuery.Where(m => m.Title.Contains(title));
            }
            if (rating.HasValue)
            {
                moviesQuery = moviesQuery
                    .AsEnumerable()
                    .Where(m => !string.IsNullOrEmpty(m.Rating) &&
                                double.TryParse(m.Rating, out double movieRating) &&
                                movieRating >= rating.Value)
                    .AsQueryable();
            }

            // Сортуємо та отримуємо дані із бази
            var moviesList = await moviesQuery.OrderBy(m => m.Title).ToListAsync();
            int totalMovies = moviesList.Count;

            // Пагінація: отримуємо лише дані для поточної сторінки
            var moviesPage = moviesList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Створюємо об'єкт StaticPagedList для передачі у View
            var pagedMovies = new StaticPagedList<Movie>(moviesPage, pageNumber, pageSize, totalMovies);

            // Передаємо додаткові дані для фільтрів у ViewBag (якщо потрібно)
            ViewBag.Categories = await _context.Categories
                .Select(c => c.Name)
                .Distinct()
                .ToListAsync();

            ViewBag.Years = await _context.Movies
                .Select(m => m.ReleaseYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            ViewBag.Ratings = await _context.Movies
                .Where(m => !string.IsNullOrEmpty(m.Rating))
                .Select(m => m.Rating)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            return View(pagedMovies);
        }



        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies
                .Include(m => m.Categories)
                .Include(m => m.Actors)
                .Include(m => m.Directors)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            ViewBag.Actors = new SelectList(await _context.Actors.ToListAsync(), "Id", "FullName");
            ViewBag.Directors = new SelectList(await _context.Directors.ToListAsync(), "Id", "FullName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie movie, int[] selectedCategories, string actorNames, string directorNames, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
                return View(movie);
            }

            if (imageFile != null)
                movie.BannerUrl = await SaveImageAsync(imageFile);

            // Assign selected categories
            movie.Categories = await _context.Categories.Where(c => selectedCategories.Contains(c.Id)).ToListAsync();

            // Handle dynamic actor input
            if (!string.IsNullOrWhiteSpace(actorNames))
            {
                var actorList = actorNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(name => name.Trim())
                                          .ToList();

                movie.Actors = new List<Actor>();
                foreach (var actorName in actorList)
                {
                    var existingActor = await _context.Actors.FirstOrDefaultAsync(a => a.FullName == actorName);
                    if (existingActor != null)
                        movie.Actors.Add(existingActor);
                    else
                        movie.Actors.Add(new Actor { FullName = actorName });
                }
            }

            // Handle dynamic director input
            if (!string.IsNullOrWhiteSpace(directorNames))
            {
                var directorList = directorNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(name => name.Trim())
                                                .ToList();

                movie.Directors = new List<Director>();
                foreach (var directorName in directorList)
                {
                    var existingDirector = await _context.Directors.FirstOrDefaultAsync(d => d.FullName == directorName);
                    if (existingDirector != null)
                        movie.Directors.Add(existingDirector);
                    else
                        movie.Directors.Add(new Director { FullName = directorName });
                }
            }

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Movie movie, IFormFile? imageFile)
        {
            if (id != movie.Id || !ModelState.IsValid) return View(movie);

            try
            {
                if (imageFile != null)
                    movie.BannerUrl = await SaveImageAsync(imageFile);

                _context.Update(movie);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Movies.AnyAsync(m => m.Id == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Movies/DetailsWithSession/5
        // GET: Movies/DetailsWithSession/5
        [Authorize]
        public async Task<IActionResult> DetailsWithSession(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.Categories)
                .Include(m => m.Directors)
                .Include(m => m.Actors)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            string? selectedCinemaId = HttpContext.Session.GetString("SelectedCinemaId");
            int? cinemaId = string.IsNullOrEmpty(selectedCinemaId) || !int.TryParse(selectedCinemaId, out int parsedId) ? null : parsedId;

            var cinemas = await _context.Cinemas
                .Include(c => c.Halls)
                .ToListAsync();

            var viewModel = new MovieWithSessionViewModel
            {
                Movie = movie,
                Cinemas = cinemas,
                SelectedCinemaId = cinemaId
            };

            return View("Details", viewModel);
        }

        // POST: Movies/DetailsWithSession (Create Session)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSession(int movieId, int? cinemaId, int hallId, DateTime startDate, string startTime, bool isPrivate, string selectedSeatIds)
        {
            if (!cinemaId.HasValue && string.IsNullOrEmpty(HttpContext.Session.GetString("SelectedCinemaId")))
            {
                return BadRequest("Будь ласка, оберіть кінотеатр.");
            }

            if (!DateTime.TryParseExact(startTime, "HH:mm", null, System.Globalization.DateTimeStyles.None, out var timeOfDay))
            {
                return BadRequest("Неправильний формат часу.");
            }

            var startDateTime = startDate.Date.Add(timeOfDay.TimeOfDay);

            if (!cinemaId.HasValue)
            {
                string? selectedCinemaId = HttpContext.Session.GetString("SelectedCinemaId");
                if (string.IsNullOrEmpty(selectedCinemaId) || !int.TryParse(selectedCinemaId, out int parsedId))
                {
                    return BadRequest("Будь ласка, оберіть кінотеатр.");
                }
                cinemaId = parsedId;
            }

            var hall = await _context.Halls
                .Include(h => h.Cinema)
                .FirstOrDefaultAsync(h => h.Id == hallId && h.CinemaId == cinemaId);
            if (hall == null)
            {
                return BadRequest("Вибраний зал недійсний або не належить обраному кінотеатру.");
            }

            // Find the existing schedule
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.HallId == hallId && s.StartTime == startDateTime && s.IsActive);
            if (schedule == null)
            {
                return BadRequest("Обраний час недоступний.");
            }

            // Check if a session already exists for this schedule
            var existingSession = await _context.Sessions
                .AnyAsync(s => s.ScheduleId == schedule.Id && s.IsActive);
            if (existingSession)
            {
                return BadRequest("Цей час уже зайнятий іншим сеансом.");
            }

            var session = new Session
            {
                MovieId = movieId,
                ScheduleId = schedule.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            var seats = await _context.Seats
                .Where(seat => seat.HallId == hallId)
                .ToListAsync();

            decimal pricePerSeat = isPrivate ? 1200m / hall.TotalSeats : 170m;

            var sessionSeats = seats.Select(seat => new SessionSeat
            {
                SessionId = session.Id,
                SeatId = seat.Id,
                Price = pricePerSeat
            }).ToList();

            _context.SessionSeats.AddRange(sessionSeats);
            await _context.SaveChangesAsync();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (isPrivate)
            {
                var booking = new Booking
                {
                    SessionId = session.Id,
                    IsPrivateBooking = true,
                    PrivateBookingPrice = 1200m,
                    BookingDate = DateTime.UtcNow,
                    UserId = userId,
                    NumberOfSeats = seats.Count
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                foreach (var sessionSeat in sessionSeats)
                {
                    sessionSeat.BookingId = booking.Id;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(selectedSeatIds))
                {
                    return BadRequest("Будь ласка, оберіть місця для бронювання.");
                }

                var seatIds = selectedSeatIds.Split(',').Select(int.Parse).ToList();
                var seatsToBook = sessionSeats.Where(ss => seatIds.Contains(ss.SeatId)).ToList();

                if (!seatsToBook.Any())
                {
                    return BadRequest("Обрані місця недійсні.");
                }

                // Check if any of the selected seats are already booked
                var bookedSeatIds = await _context.SessionSeats
                    .Where(ss => ss.SessionId == session.Id && ss.BookingId != null && seatIds.Contains(ss.SeatId))
                    .Select(ss => ss.SeatId)
                    .ToListAsync();

                if (bookedSeatIds.Any())
                {
                    return BadRequest("Деякі з обраних місць уже заброньовані.");
                }

                var booking = new Booking
                {
                    SessionId = session.Id,
                    IsPrivateBooking = false,
                    PrivateBookingPrice = 0m,
                    BookingDate = DateTime.UtcNow,
                    UserId = userId,
                    NumberOfSeats = seatsToBook.Count
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                foreach (var sessionSeat in seatsToBook)
                {
                    sessionSeat.BookingId = booking.Id;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MyBookings", "Sessions");
        }

        // GET: Movies/GetHalls
        [HttpGet]
        public IActionResult GetHalls(int cinemaId)
        {
            var halls = _context.Halls
                .Where(h => h.CinemaId == cinemaId)
                .Select(h => new { id = h.Id, name = h.Name })
                .ToList();
            return Json(halls);
        }

        // GET: Movies/GetHallSeatCount
        [HttpGet]
        public async Task<IActionResult> GetHallSeatCount(int hallId)
        {
            var seatCount = await _context.Seats
                .CountAsync(seat => seat.HallId == hallId);
            return Json(new { seatCount = seatCount });
        }

        // GET: Movies/GetAvailableTimes

        // GET: Movies/GetAvailableTimes
        [HttpGet]
        public async Task<IActionResult> GetAvailableTimes(int hallId, DateTime selectedDate)
        {
            // Get all active schedules for the hall
            var schedules = await _context.Schedules
                .Where(s => s.HallId == hallId && s.IsActive)
                .ToListAsync();

            if (!schedules.Any())
            {
                return Json(new List<string>());
            }

            // Get existing sessions for the hall on the selected date
            var existingSessions = await _context.Sessions
                .Include(s => s.Schedule)
                .Where(s => s.Schedule.HallId == hallId && s.IsActive && s.Schedule.StartTime.Date == selectedDate.Date)
                .Select(s => s.ScheduleId)
                .ToListAsync();

            // Filter schedules to only include those on the selected date and not booked
            var availableTimes = schedules
                .Where(s => s.StartTime.Date == selectedDate.Date && !existingSessions.Contains(s.Id))
                .Select(s => s.StartTime.ToString("HH:mm"))
                .OrderBy(t => t)
                .ToList();

            return Json(availableTimes);
        }

        // GET: Movies/GetSeatsForHall
        [HttpGet]
        public async Task<IActionResult> GetSeatsForHall(int hallId, DateTime selectedDate, string startTime)
        {
            if (!DateTime.TryParseExact(startTime, "HH:mm", null, System.Globalization.DateTimeStyles.None, out var timeOfDay))
            {
                return BadRequest("Неправильний формат часу.");
            }

            var startDateTime = selectedDate.Date.Add(timeOfDay.TimeOfDay);

            // Find the schedule that matches the selected date and time
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.HallId == hallId && s.StartTime == startDateTime && s.IsActive);

            if (schedule == null)
            {
                return Json(new List<object>());
            }

            // Check for existing sessions at this schedule to determine booked seats
            var bookedSeatIds = await _context.SessionSeats
                .Include(ss => ss.Session)
                .Where(ss => ss.Session.ScheduleId == schedule.Id && ss.Session.IsActive && ss.BookingId != null)
                .Select(ss => ss.SeatId)
                .ToListAsync();

            // Get all seats for the hall
            var seats = await _context.Seats
                .Where(s => s.HallId == hallId)
                .Select(s => new
                {
                    id = s.Id,
                    row = s.RowNumber,
                    number = s.SeatNumber,
                    isAvailable = !bookedSeatIds.Contains(s.Id) // Seat is available if it's not booked
                })
                .OrderBy(s => s.row)
                .ThenBy(s => s.number)
                .ToListAsync();

            return Json(seats);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Movie List
        public async Task<IActionResult> AdminIndex()
        {
            var movies = await _context.Movies.ToListAsync();
            return View(movies);
        }

        private async Task<string?> SaveImageAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img");
            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
                await imageFile.CopyToAsync(fileStream);

            return "/img/" + uniqueFileName;
        }

        private bool MovieExists(int id)
        {
            return _context.Movies.Any(e => e.Id == id);
        }
    }
}

