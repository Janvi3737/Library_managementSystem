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

        // ADD THIS
        public int AvailableCopies { get; set; }
    }
}
