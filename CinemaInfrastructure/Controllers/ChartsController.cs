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
}


