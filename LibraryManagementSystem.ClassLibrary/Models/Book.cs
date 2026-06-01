
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ISBN { get; set; } = string.Empty;

        [Required]
        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public Author? Author { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        [Range(0, int.MaxValue)]
        public int TotalCopies { get; set; }

        public int TotalPages { get; set; }
        public int AvailableCopies { get; set; }
        public bool IsFeatured { get; set; } = false;

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? CoverImageUrl { get; set; }
        public string? PdfUrl { get; set; }
        public string? PreviewPdfUrl { get; set; }

        public int BorrowCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<BorrowRecord> BorrowRecords { get; set; } = new();

        public ICollection<Reservation?> Reservations { get; set; } = null;
    }
}
