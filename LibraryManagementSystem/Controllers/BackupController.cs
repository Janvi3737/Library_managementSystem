using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BackupController : Controller
    {
        private readonly AppDbContext _context;

        public BackupController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();

        // GET: /Backup/Download — streams a copy of the SQLite .db file.
        public async Task<IActionResult> Download()
        {
            if (!_context.Database.IsSqlite())
            {
                TempData["Error"] = "Backup download is only available on the SQLite (Mac/Linux dev) provider. " +
                                    "On SQL Server, use SSMS or BACKUP DATABASE.";
                return RedirectToAction(nameof(Index));
            }

            var conn = _context.Database.GetDbConnection();
            var dataSource = ParseDataSource(conn.ConnectionString);

            if (string.IsNullOrEmpty(dataSource) || !System.IO.File.Exists(dataSource))
            {
                TempData["Error"] = "Could not locate the SQLite database file.";
                return RedirectToAction(nameof(Index));
            }

            // Read into memory so we don't hold a file handle that conflicts
            // with the running app's connection.
            byte[] bytes;
            using (var fs = new FileStream(dataSource, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var ms = new MemoryStream())
            {
                await fs.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var name = $"LibraryManagementDB-{DateTime.Now:yyyyMMdd-HHmmss}.db";
            return File(bytes, "application/octet-stream", name);
        }

        private static string? ParseDataSource(string connStr)
        {
            foreach (var part in connStr.Split(';'))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 &&
                    kv[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
                    return kv[1].Trim();
            }
            return null;
        }
    }
}
