using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class EventReservation
    {
        [Key]
        public int Id { get; set; }

        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public Event Event { get; set; }

        public int MemberId { get; set; }

        [ForeignKey(nameof(MemberId))]
        public Member Member { get; set; }

        public DateTime ReservedOn { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Reserved";


    }
}
