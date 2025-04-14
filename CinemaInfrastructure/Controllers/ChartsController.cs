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
            .GroupBy(session => session.Movie.Title) // Group by movie title
            .Select(group => new // Create anonymous type for response
            {
                MovieTitle = group.Key, // The title of the movie
                SessionCount = group.Count() // Count of sessions for that movie
            })
            .OrderByDescending(item => item.SessionCount) // Order by session count descending
            .Take(10) // Take only the top 10 movies
            .ToListAsync(cancellationToken); // Execute the query asynchronously

        return new JsonResult(responseItems); // Return the results as JSON
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

        // Ensure all days of the week are represented, even if there are zero bookings
        var daysOfWeek = Enum.GetNames(typeof(DayOfWeek)).ToList();
        var result = daysOfWeek.Select(day => new
        {
            DayOfWeek = day,
            BookingCount = bookingsByDay.FirstOrDefault(b => b.DayOfWeek == day)?.BookingCount ?? 0
        }).OrderBy(d => (int)Enum.Parse<DayOfWeek>(d.DayOfWeek)).ToList();

        return Ok(result);
    }
}


