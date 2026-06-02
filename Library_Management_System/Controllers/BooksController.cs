using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    /// <summary>
    /// Serves book PDFs to the user-facing app. The files physically live in
    /// the ADMIN app's wwwroot (LibraryManagementSystem/wwwroot/uploads/pdfs)
    /// — this controller resolves the cross-app path so the user app's
    /// /Books/ViewPdf/{id} and /Books/ViewPreview/{id} URLs actually work.
    /// </summary>
    public class BooksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public BooksController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // GET: /Books/ViewPreview/5  — anyone can read the preview PDF
        [AllowAnonymous]
        public async Task<IActionResult> ViewPreview(int id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null || string.IsNullOrEmpty(book.PreviewPdfUrl))
                return NotFound();

            var path = ResolvePdfPath(book.PreviewPdfUrl);
            if (path == null)
                return NotFound();

            // Inline so it renders inside the <iframe> rather than downloading.
            return PhysicalFile(path, "application/pdf");
        }

        // GET: /Books/ViewPdf/5  — full PDF, members only
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> ViewPdf(int id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null || string.IsNullOrEmpty(book.PdfUrl))
                return NotFound();

            var path = ResolvePdfPath(book.PdfUrl);
            if (path == null)
                return NotFound();

            return PhysicalFile(path, "application/pdf");
        }

        // GET: /Books/DownloadPdf/5  — same as ViewPdf but as attachment
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null || string.IsNullOrEmpty(book.PdfUrl))
                return NotFound();

            var path = ResolvePdfPath(book.PdfUrl);
            if (path == null)
                return NotFound();

            var fileName = $"{book.Title}.pdf";
            return PhysicalFile(path, "application/pdf", fileName);
        }

        // PDF files are uploaded by the admin app into ITS wwwroot. From the
        // user app we look in our own wwwroot first (in case a deployment
        // copied them), then fall back to the sibling admin wwwroot.
        private string? ResolvePdfPath(string pdfUrl)
        {
            var relPath = pdfUrl.TrimStart('/')
                                .Replace('/', Path.DirectorySeparatorChar);

            // 1. Try user app's own wwwroot
            var local = Path.Combine(_env.WebRootPath, relPath);
            if (System.IO.File.Exists(local))
                return local;

            // 2. Try the admin app's wwwroot (sibling directory)
            //    user-app ContentRoot = .../Library_Management_System
            //    admin-app wwwroot    = .../LibraryManagementSystem/wwwroot
            var adminWwwroot = Path.GetFullPath(Path.Combine(
                _env.ContentRootPath, "..",
                "LibraryManagementSystem", "wwwroot"));

            var admin = Path.Combine(adminWwwroot, relPath);
            return System.IO.File.Exists(admin) ? admin : null;
        }
    }
}
