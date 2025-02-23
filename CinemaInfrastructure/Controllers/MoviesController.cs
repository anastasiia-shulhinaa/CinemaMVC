using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;


namespace CinemaInfrastructure.Controllers
{
    public class MoviesController : Controller
    {
        private readonly DbcinemaContext _context;

        public MoviesController(DbcinemaContext context)
        {
            _context = context;
        }

        // GET: Movies by Title
        public IActionResult SearchByTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return RedirectToAction("Index");
            }

            var movies = _context.Movies
                                 .Where(m => m.Title.Contains(title)) // contains helps us handle partial match
                                 .ToList();

            return View("Index", movies);
        }

        // GET: Movies by Category \ Release year \ Rating 
        public IActionResult Index(string category, int? year, double? rating)
        {
            var moviesQuery = _context.Movies
        .Include(m => m.Categories)
        .AsQueryable();

            // Apply filters (if any)
            if (!string.IsNullOrEmpty(category))
            {
                moviesQuery = moviesQuery.Where(m => m.Categories.Any(c => c.Name == category));
            }
            if (year.HasValue)
            {
                moviesQuery = moviesQuery.Where(m => m.ReleaseYear == year.Value);
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

            var movies = moviesQuery.ToList();

            ViewBag.Categories = _context.Categories
                                        .Select(c => c.Name)
                                        .Distinct()
                                        .ToList();
            ViewBag.Years = _context.Movies
                                    .Select(m => m.ReleaseYear)
                                    .Distinct()
                                    .OrderByDescending(y => y)
                                    .ToList();
            ViewBag.Ratings = _context.Movies
                                      .Where(m => !string.IsNullOrEmpty(m.Rating))
                                      .Select(m => m.Rating)
                                      .Distinct()
                                      .OrderBy(r => r)
                                      .ToList();

            return View(movies);
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
