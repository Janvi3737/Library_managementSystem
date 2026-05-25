using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class BorrowRecord
    {
        public int Id { get; set; }


        [Required]
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }


        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }


        [Required]
        public DateTime IssuedOn { get; set; } = DateTime.Now;

        [Required]
        public DateTime DueDate { get; set; }

        public int RenewCount { get; set; } = 0;

        public DateTime? ReturnedOn { get; set; }


        public int DaysLate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinePerDay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FineAmount { get; set; }

        // Toggled by admin when the fine is paid OR waived. Outstanding fines
        // are FineAmount > 0 AND FinePaid == false.
        public bool FinePaid { get; set; }


        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Issued";
    }
}