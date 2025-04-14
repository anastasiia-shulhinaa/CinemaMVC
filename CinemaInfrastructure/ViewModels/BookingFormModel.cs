using CinemaDomain.Model;
using System;
using System.Collections.Generic;

namespace CinemaInfrastructure.ViewModels
{
    public class BookingFormModel
    {
        public int SessionId { get; set; }
        public Movie Movie { get; set; }
        public List<TimeOption> AvailableTimes { get; set; } = new List<TimeOption>();
        public Dictionary<Cinema, List<TimeOption>> GroupedTimes { get; set; } // Added for grouped sessions
        public List<SeatViewModel> SessionSeats { get; set; }
        public bool IsPrivate { get; set; }
        public decimal? PrivateBookingPrice { get; set; }
        public string SelectedSeats { get; set; }
    }

    public class TimeOption
    {
        public int SessionId { get; set; }
        public DateTime StartTime { get; set; }
    }


}