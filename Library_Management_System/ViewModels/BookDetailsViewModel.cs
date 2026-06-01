using LibraryManagementSystem.ClassLibrary.Models;

namespace Library_Management_System.ViewModels
{
    public class BookDetailsViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string? Description { get; set; }

        public string? CoverImageUrl { get; set; }

        public string? AuthorName { get; set; }

        public string? CategoryName { get; set; }

        public bool IsAvailable { get; set; }

        public bool IsWishlisted { get; set; }

        public int AvailableCopies { get; set; }

        public double AverageRating { get; set; }

        public int TotalReviews { get; set; }

        public List<BookReview>? Reviews { get; set; }

        public int UserRating { get; set; }

        public string? UserComment { get; set; }
        public string? PdfUrl { get; set; }

        public string? PreviewPdfUrl { get; set; }

        public bool HasMembership { get; set; }
    }
}
