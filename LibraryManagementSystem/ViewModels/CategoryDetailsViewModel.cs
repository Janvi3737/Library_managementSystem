using LibraryManagementSystem.ClassLibrary.Models;

namespace LibraryManagementSystem.ViewModels
{
    public class CategoryDetailsViewModel
    {
        public int Id { get; set; }
        public Category Category { get; set; }

        public List<Book> Books { get; set; } = new List<Book>();
    }
}

