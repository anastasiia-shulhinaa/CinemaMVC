using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CinemaInfrastructure.ViewComponents
{
    public class CinemaDropdownViewComponent : ViewComponent
    {
        private readonly DbcinemaContext _context;

        public CinemaDropdownViewComponent(DbcinemaContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var cinemas = _context.Cinemas
                .GroupBy(c => c.Name)
                .Select(g => g.First())
                .ToList();

            return View(cinemas);
        }
    }
}
