using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    /// <summary>
    /// Append-only log of admin mutations. Written by an action filter on
    /// the admin app whenever a POST/PUT/DELETE action runs.
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string? UserEmail { get; set; }

        [StringLength(50)]
        public string Action { get; set; } = string.Empty;        // POST /Books/Edit/5

        [StringLength(100)]
        public string? Controller { get; set; }

        [StringLength(100)]
        public string? ActionName { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }                      // optional: id, summary

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
