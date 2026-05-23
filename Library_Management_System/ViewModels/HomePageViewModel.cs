using LibraryManagementSystem.ClassLibrary.Models;

namespace Library_Management_System.ViewModels
{
    public class HomePageViewModel
    {
        public List<Book> ContinueReadingBooks { get; set; } = new();

        public List<Book> NewArrivals { get; set; } = new();

        public List<CategoryWithCountViewModel> PopularCategories { get; set; } = new();

        public List<EventViewModel> UpcomingEvents { get; set; } = new();
    }

    public class CategoryWithCountViewModel
    {
        public string CategoryName { get; set; } = string.Empty;

        public int BookCount { get; set; }

        public string IconClass { get; set; } = "📚";
    }

    public class EventViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public DateTime Date { get; set; }
    }
}
