using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaInfrastructure.Controllers;
[Route("api/[controller]")]
[ApiController]
public class ChartsController : ControllerBase
{
    private record CountByYearResponseItem(string Year, int Count);

    private readonly DbcinemaContext cinemaContext;

    public ChartsController(DbcinemaContext cinemaContext)
    {
        this.cinemaContext = cinemaContext;
    }

    [HttpGet("countByMovie")]
    public async Task<JsonResult> GetCountByMovieAsync(CancellationToken cancellationToken)
    {
        var responseItems = await cinemaContext
            .Sessions
            .GroupBy(session => session.Movie.Title)
            .Select(group => new { MovieTitle = group.Key, SessionCount = group.Count() })
            .ToListAsync(cancellationToken);

        return new JsonResult(responseItems);
    }
}
