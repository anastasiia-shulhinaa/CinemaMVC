using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using X.PagedList;
using X.PagedList.Mvc.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;



namespace CinemaInfrastructure.Controllers
{
    public class MoviesController : Controller
    {
        private readonly DbcinemaContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MoviesController(DbcinemaContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
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
