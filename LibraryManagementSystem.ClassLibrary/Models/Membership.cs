using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class Membership
    {
        public int Id { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member Member { get; set; }

        [Required]
        public string MembershipType { get; set; }

        [Required]
        public int DurationMonths { get; set; }

        [Required]
        public DateTime StartDate { get; set; } = DateTime.Now;
        [Required]
        public DateTime EndDate { get; set; }
        
        // public bool IsActive { get; set; } = true;
        // [Required]
        // public decimal Fee { get; set; }

         // ✅ UPDATED: only true after admin approval
        public bool IsActive { get; set; } = false;

        public bool IsRenewed { get; set; } = false;

        [Required]
        public decimal Fee { get; set; }
    }
}