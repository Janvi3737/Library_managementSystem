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
        public int Id { get; set; } = 1;

        // BORROWING
        public int DefaultLoanDays { get; set; } = 14;
        public int MaxRenewals { get; set; } = 2;

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinePerDay { get; set; } = 5m;

        // MEMBERSHIP FEES (1-month vs annual). Premium/Regular/Student.
        [Column(TypeName = "decimal(18,2)")]
        public decimal StudentMonthly { get; set; } = 99m;
        [Column(TypeName = "decimal(18,2)")]
        public decimal StudentAnnual { get; set; } = 1000m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RegularMonthly { get; set; } = 149m;
        [Column(TypeName = "decimal(18,2)")]
        public decimal RegularAnnual { get; set; } = 1500m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PremiumMonthly { get; set; } = 299m;
        [Column(TypeName = "decimal(18,2)")]
        public decimal PremiumAnnual { get; set; } = 3000m;

        [StringLength(120)]
        public string LibraryName { get; set; } = "BookVerse";
    }
}
