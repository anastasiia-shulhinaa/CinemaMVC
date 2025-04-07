using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Model
{
    public partial class Schedule : Entity
    {
        // Store the full date and time
        [Required(ErrorMessage = "Поле не повинно бути порожнім")]
        [Display(Name = "Час початку")]
        public DateTime StartTime { get; set; } // Change to DateTime

        // Automatically calculated property for end time
        [Display(Name = "Час завершення")]
        public DateTime EndTime => StartTime.AddHours(3); // Automatically reserves 3 hours

        [Required]
        public int HallId { get; set; }
        public bool IsActive { get; set; }

        public virtual Hall Hall { get; set; } = null!;
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}

