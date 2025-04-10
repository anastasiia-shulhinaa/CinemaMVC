namespace CinemaInfrastructure.ViewModels;
public class SeatViewModel
{
    public int Id { get; set; }
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}