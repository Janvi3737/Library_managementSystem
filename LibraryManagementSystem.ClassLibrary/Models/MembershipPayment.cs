using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class MembershipPayment
    {
        public int Id { get; set; }

        public int MembershipId { get; set; }

        public Membership Membership { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        [Required]
        public string PaymentStatus { get; set; } = "Pending"; 
        // Pending / Approved / Rejected

        public string TransactionId { get; set; }

        // Public path under wwwroot, e.g. "/paymentproof/<guid>.jpg". Populated
        public string? ScreenshotPath { get; set; }

        public DateTime PaymentDate { get; set; }
            = DateTime.Now;

            
    }
}
