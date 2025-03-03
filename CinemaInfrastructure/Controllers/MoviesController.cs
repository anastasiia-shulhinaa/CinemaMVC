using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using X.PagedList;
using X.PagedList.Mvc.Core;



namespace CinemaInfrastructure.Controllers
{
    public class MoviesController : Controller
    {
        private readonly DbcinemaContext _context;

        public MoviesController(DbcinemaContext context)
        {
            _context = context;
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

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ReleaseYear,Language,Description,Duration,Rating,TrailerLink,Id")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Title,ReleaseYear,Language,Description,Duration,Rating,TrailerLink,Id")] Movie movie)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id))
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
            return View(movie);
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

        private bool MovieExists(int id)
        {
            return _context.Movies.Any(e => e.Id == id);
        }
    }
}
