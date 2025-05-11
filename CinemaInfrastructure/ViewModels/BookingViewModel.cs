namespace CinemaInfrastructure.ViewModels
{
    public class BookingViewModel
    {
        public int Id { get; set; } // Унікальний ідентифікатор бронювання
        public int SessionId { get; set; } // Ідентифікатор сеансу
        public string MovieTitle { get; set; } // Назва фільму
        public int? NumberOfSeats { get; set; } // Кількість заброньованих місць (може бути null)
        public bool IsPrivateBooking { get; set; } // Прапорець приватного бронювання
        public decimal? PrivateBookingPrice { get; set; } // Ціна для приватного бронювання (може бути null)
        public decimal PricePerSeat { get; set; } // Ціна за одне місце (для розрахунків)
        public decimal CalculatedPrice { get; set; } // Розрахункова ціна для не приватних бронювань
        public string UserEmail { get; set; } // Пошта користувача
        public DateTime BookingDate { get; set; } // Дата створення бронювання
        public DateTime SessionDate { get; set; } // Дата сеансу
    }
}
