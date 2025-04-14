using CinemaDomain.Model;
namespace CinemaInfrastructure.ViewModels
{
    public class HallSchedule
    {
        public int Id { get; set; }
        public int HallId { get; set; }
        public Hall Hall { get; set; }
        public TimeSpan StartTime { get; set; } // e.g., 09:00, 12:00, 15:00
        public bool IsActive { get; set; } // To enable/disable specific time slots
    }
}