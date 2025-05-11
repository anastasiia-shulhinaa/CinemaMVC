namespace CinemaInfrastructure.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;




[Route("api/[controller]")]
[ApiController]
public class ChartsController : ControllerBase
{
    private readonly DbcinemaContext cinemaContext;

    public ChartsController(DbcinemaContext cinemaContext)
    {
        this.cinemaContext = cinemaContext;
    }

    [HttpGet("topMovies")]
    public async Task<JsonResult> GetTopMoviesAsync(CancellationToken cancellationToken)
    {
        var responseItems = await cinemaContext
            .Sessions
            .GroupBy(session => session.Movie.Title) 
            .Select(group => new 
            {
                MovieTitle = group.Key, 
                SessionCount = group.Count() 
            })
            .OrderByDescending(item => item.SessionCount) 
            .Take(10) 
            .ToListAsync(cancellationToken); 

        return new JsonResult(responseItems);
    }

    [HttpGet("bookingsByDayOfWeek")]
    public async Task<IActionResult> GetBookingsByDayOfWeek()
    {
        var bookingsByDay = await cinemaContext.Bookings
            .GroupBy(b => b.BookingDate.DayOfWeek)
            .Select(g => new
            {
                DayOfWeek = g.Key.ToString(),
                BookingCount = g.Count()
            })
            .ToListAsync();

        var daysOfWeek = Enum.GetNames(typeof(DayOfWeek)).ToList();
        var result = daysOfWeek.Select(day => new
        {
            DayOfWeek = day,
            BookingCount = bookingsByDay.FirstOrDefault(b => b.DayOfWeek == day)?.BookingCount ?? 0
        }).OrderBy(d => (int)Enum.Parse<DayOfWeek>(d.DayOfWeek)).ToList();

        return Ok(result);
    }
}