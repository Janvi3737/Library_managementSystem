using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class TokenRefund
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BorrowRecordId { get; set; }

        [ForeignKey(nameof(BorrowRecordId))]
        public BorrowRecord? BorrowRecord { get; set; }

        [Required]
        public int UserTokenId { get; set; }

        [ForeignKey(nameof(UserTokenId))]
        public UserToken? UserToken { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; } = 200;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FineAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string BookCondition { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string RefundStatus { get; set; } = "Pending";
        // Pending, Approved, Rejected

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? ProcessedOn { get; set; }
    }
}
