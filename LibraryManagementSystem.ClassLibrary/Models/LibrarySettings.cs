using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    /// <summary>
    /// Singleton row of library-wide knobs that used to be hardcoded.
    /// Seeded with a default row on first run (see DbSeeder); admin Settings
    /// page edits this single row.
    /// </summary>
    public class LibrarySettings
    {
        public int Id { get; set; } 

        // BORROWING
        public int DefaultLoanDays { get; set; } 
        public int MaxRenewals { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinePerDay { get; set; } 

        // MEMBERSHIP FEES (1-month vs annual). Premium/Regular/Student.
        [Column(TypeName = "decimal(18,2)")]
        public decimal StudentMonthly { get; set; } 
        [Column(TypeName = "decimal(18,2)")]
        public decimal StudentAnnual { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal RegularMonthly { get; set; } 
        [Column(TypeName = "decimal(18,2)")]
        public decimal RegularAnnual { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal PremiumMonthly { get; set; } 
        [Column(TypeName = "decimal(18,2)")]
        public decimal PremiumAnnual { get; set; }

        [StringLength(120)]
        public string LibraryName { get; set; } = "BookVerse";
    }
}
