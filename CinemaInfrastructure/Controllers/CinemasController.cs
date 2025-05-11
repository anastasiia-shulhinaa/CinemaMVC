using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using CinemaInfrastructure.Enums;
using CinemaInfrastructure.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CinemaInfrastructure.Controllers
{
    [Authorize(Roles = "admin")]
    public class CinemasController : Controller
    {
        private readonly DbcinemaContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly GoogleMapsUrlBuilder _googleMapsUrlBuilder;

        public CinemasController(DbcinemaContext context, IWebHostEnvironment webHostEnvironment, GoogleMapsUrlBuilder googleMapsUrlBuilder)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _googleMapsUrlBuilder = googleMapsUrlBuilder;
        }

        // GET: Cinemas
        public IActionResult Index(string sortBy = "Name", string sortOrder = "asc", string cityFilter = null)
        {
            var cinemasQuery = _context.Cinemas
                .Include(c => c.Halls)
                .AsNoTracking() // Optimize for read-only scenario
                .AsQueryable();

            // Apply city filter
            if (!string.IsNullOrEmpty(cityFilter))
            {
                cinemasQuery = cinemasQuery.Where(c => c.City == cityFilter);
            }

            // Apply sorting
            switch (sortBy)
            {
                case "City":
                    cinemasQuery = sortOrder == "asc"
                        ? cinemasQuery.OrderBy(c => c.City)
                        : cinemasQuery.OrderByDescending(c => c.City);
                    break;
                case "Name":
                default:
                    cinemasQuery = sortOrder == "asc"
                        ? cinemasQuery.OrderBy(c => c.Name)
                        : cinemasQuery.OrderByDescending(c => c.Name);
                    break;
            }

            var cinemas = cinemasQuery.ToList();
            var cinemaViewModels = cinemas.Select(c => new CinemaViewModel
            {
                Cinema = c,
                GoogleMapsEmbedUrl = _googleMapsUrlBuilder.GetEmbedUrl(c)
            }).ToList();

            // Create SelectList for cities dropdown
            var allCities = _context.Cinemas.Select(c => c.City).Distinct().ToList();
            ViewBag.CitiesSelectList = new SelectList(allCities, cityFilter);

            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CityFilter = cityFilter;

            return View(cinemaViewModels);
        }

        // GET: Cinemas/Filter (Updated for AJAX)
        [HttpGet]
        public IActionResult Filter(string city)
        {
            var cinemasQuery = _context.Cinemas
                .Include(c => c.Halls)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(city))
            {
                cinemasQuery = cinemasQuery.Where(c => c.City == city);
            }

            var cinemas = cinemasQuery.OrderBy(c => c.Name).ToList();
            var cinemaViewModels = cinemas.Select(c => new CinemaViewModel
            {
                Cinema = c,
                GoogleMapsEmbedUrl = _googleMapsUrlBuilder.GetEmbedUrl(c)
            }).ToList();

            // Create SelectList for cities dropdown (for partial view refresh)
            var allCities = _context.Cinemas.Select(c => c.City).Distinct().ToList();
            ViewBag.CitiesSelectList = new SelectList(allCities, city);

            return PartialView("_CinemasListPartial", cinemaViewModels);
        }

        // GET: Cinemas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Cinemas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cinema cinema, IFormFile photoFile)
        {
            // Handle phone number: Remove +38 (Ukr) if present
            if (!string.IsNullOrEmpty(cinema.PhoneNumber))
            {
                cinema.PhoneNumber = cinema.PhoneNumber.Replace("+38 (Ukr)", "").Trim();
            }

            // Handle file upload
            if (photoFile != null && photoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/cinemas");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }

                cinema.PhotoUrl = $"/uploads/cinemas/{fileName}";
            }

            if (ModelState.IsValid)
            {
                _context.Add(cinema);
                await _context.SaveChangesAsync();
                ViewData["Notification"] = new Notification
                {
                    Type = NotificationType.Success,
                    Message = $"Кінотеатр \"{cinema.Name}\" успішно створено."
                };
                return RedirectToAction(nameof(Index));
            }

            ViewData["Notification"] = new Notification
            {
                Type = NotificationType.Error,
                Message = "Не вдалося створити кінотеатр. Перевірте введені дані."
            };
            return View(cinema);
        }

        // GET: Cinemas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null)
            {
                return NotFound();
            }

            return View(cinema);
        }

        // POST: Cinemas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cinema cinema, IFormFile? photoFile)
        {
            if (id != cinema.Id)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(cinema.PhoneNumber))
            {
                cinema.PhoneNumber = cinema.PhoneNumber.Replace("+38 (Ukr)", "").Trim();
            }

            var existingCinema = await _context.Cinemas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existingCinema == null)
            {
                return NotFound();
            }

            if (photoFile != null && photoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/cinemas");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(existingCinema.PhotoUrl))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingCinema.PhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                cinema.PhotoUrl = $"/uploads/cinemas/{fileName}";
            }
            else
            {
                cinema.PhotoUrl = existingCinema.PhotoUrl;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cinema);
                    await _context.SaveChangesAsync();
                    ViewData["Notification"] = new Notification
                    {
                        Type = NotificationType.Success,
                        Message = $"Кінотеатр \"{cinema.Name}\" успішно оновлено."
                    };
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CinemaExists(cinema.Id))
                    {
                        ViewData["Notification"] = new Notification
                        {
                            Type = NotificationType.Error,
                            Message = "Кінотеатр не знайдено."
                        };
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    ViewData["Notification"] = new Notification
                    {
                        Type = NotificationType.Error,
                        Message = $"Помилка при оновленні: {ex.Message}"
                    };
                }
            }

            ViewData["Notification"] = new Notification
            {
                Type = NotificationType.Error,
                Message = "Не вдалося оновити кінотеатр. Перевірте введені дані."
            };
            return View(cinema);
        }

        // POST: Cinemas/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema != null)
            {
                // Delete associated photo file
                if (!string.IsNullOrEmpty(cinema.PhotoUrl))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, cinema.PhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Cinemas.Remove(cinema);
                await _context.SaveChangesAsync();

                // Set success notification
                ViewData["Notification"] = new Notification
                {
                    Type = NotificationType.Success,
                    Message = $"Кінотеатр \"{cinema.Name}\" успішно видалено разом із усіма пов'язаними залами."
                };
            }
            else
            {
                ViewData["Notification"] = new Notification
                {
                    Type = NotificationType.Error,
                    Message = "Кінотеатр не знайдено."
                };
            }

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public IActionResult List(int movieId, string cityFilter = null)
        {
            // Start with a query that includes Halls and tracks changes only if needed
            var cinemasQuery = _context.Cinemas
                .Include(c => c.Halls)
                .AsNoTracking() // Optimize for read-only scenario
                .AsQueryable();

            // Apply city filter if provided
            if (!string.IsNullOrEmpty(cityFilter))
            {
                cinemasQuery = cinemasQuery.Where(c => c.City == cityFilter);
            }

            // Execute the query and order by name
            var cinemas = cinemasQuery.OrderBy(c => c.Name).ToList();

            // Map to ViewModel, including Google Maps URL
            var cinemaViewModels = cinemas.Select(c => new CinemaViewModel
            {
                Cinema = c,
                GoogleMapsEmbedUrl = _googleMapsUrlBuilder.GetEmbedUrl(c)
            }).ToList();

            // Prepare city options for the filter dropdown
            var allCities = _context.Cinemas.Select(c => c.City).Distinct().ToList();
            ViewBag.CitiesSelectList = new SelectList(allCities, cityFilter);

            // Pass additional data to the view
            ViewBag.MovieId = movieId;
            ViewBag.CityFilter = cityFilter;

            return View(cinemaViewModels);
        }

        // GET: Cinemas/ListFilter (New action for AJAX filtering)
        [HttpGet]
        public IActionResult ListFilter(string city)
        {
            var cinemasQuery = _context.Cinemas
                .Include(c => c.Halls)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(city))
            {
                cinemasQuery = cinemasQuery.Where(c => c.City == city);
            }

            var cinemas = cinemasQuery.OrderBy(c => c.Name).ToList();
            var cinemaViewModels = cinemas.Select(c => new CinemaViewModel
            {
                Cinema = c,
                GoogleMapsEmbedUrl = _googleMapsUrlBuilder.GetEmbedUrl(c)
            }).ToList();

            // Create SelectList for cities dropdown (for partial view refresh)
            var allCities = _context.Cinemas.Select(c => c.City).Distinct().ToList();
            ViewBag.CitiesSelectList = new SelectList(allCities, city);

            return PartialView("_CinemasListPartial", cinemaViewModels);
        }

        private bool CinemaExists(int id)
        {
            return _context.Cinemas.Any(e => e.Id == id);
        }
    }
}