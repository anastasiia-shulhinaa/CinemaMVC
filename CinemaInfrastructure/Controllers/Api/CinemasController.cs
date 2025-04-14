using CinemaDomain.Model;
using CinemaInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CinemaInfrastructure.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CinemasController : ControllerBase
    {
        private readonly DbcinemaContext _context;

        public CinemasController(DbcinemaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCinemas()
        {
            var cinemas = await _context.Cinemas
                .Select(c => new { c.Id, c.Name })
                .OrderBy(c => c.Name)
                .ToListAsync();
            return Ok(cinemas);
        }
    }
}
