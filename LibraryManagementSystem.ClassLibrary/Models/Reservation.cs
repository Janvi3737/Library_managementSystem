using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibraryManagementSystem.ClassLibrary.Models;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class Reservation
    {
        public int? Id { get; set; }

        [StringLength(450)]
        public string? MemberId { get; set; }

        [ForeignKey("MemberId")]
        public ApplicationUser? Member { get; set; }

        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }

        // NEW
        public int Quantity { get; set; }

        public DateTime ReservedOn { get; set; } = DateTime.Now;

        public ReservationStatus Status { get; set; }
            = ReservationStatus.Waiting;

        public bool PaymentRequired { get; set; }

        public bool IsPaymentCompleted { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }


    }
}
