using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class Author
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Bio { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();

        public object ToLower()
        {
            throw new NotImplementedException();
        }
    }
}