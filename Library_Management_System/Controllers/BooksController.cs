using System.Security.Claims;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    /// <summary>
    /// Serves book PDFs to the user-facing app.
    /// The PDF files live in the ADMIN app's wwwroot — this controller
    /// resolves the cross-app path so /Books/ViewPdf/{id} works.
    ///
    /// Access policy (set by the user, not the framework):
    ///   - Anyone can hit the actions (no [Authorize] gate).
    ///   - If the user has an active Membership, they get the FULL book.
    ///   - Otherwise they get the PREVIEW PDF (limited pages).
    /// </summary>
    public class BooksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BooksController(
            AppDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Books/ViewPdf/5  — inline render (iframe / target=_blank)
        public async Task<IActionResult> ViewPdf(int id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            var hasMembership = await CurrentUserHasMembershipAsync();

            // Member -> full PDF. Anyone else -> preview PDF if it exists, else
            // fall back to full PDF only if there's no preview (so the page
            // still shows something useful for guests).
            string? source =
                hasMembership
                    ? book.PdfUrl
                    : (book.PreviewPdfUrl ?? book.PdfUrl);

            if (string.IsNullOrEmpty(source)) return NotFound();

            var path = ResolvePdfPath(source);
            if (path == null) return NotFound();

            return PhysicalFile(path, "application/pdf");
        }

        // GET: /Books/ViewPreview/5  — explicit preview endpoint
        public async Task<IActionResult> ViewPreview(int id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            var source = book.PreviewPdfUrl ?? book.PdfUrl;
            if (string.IsNullOrEmpty(source)) return NotFound();

            var path = ResolvePdfPath(source);
            if (path == null) return NotFound();

            return PhysicalFile(path, "application/pdf");
        }

        // GET: /Books/DownloadPdf/5  — attachment with the book title as filename
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            var hasMembership = await CurrentUserHasMembershipAsync();

            string? source =
                hasMembership
                    ? book.PdfUrl
                    : (book.PreviewPdfUrl ?? book.PdfUrl);

            if (string.IsNullOrEmpty(source)) return NotFound();

            var path = ResolvePdfPath(source);
            if (path == null) return NotFound();

            var suffix = hasMembership ? "" : "-preview";
            var fileName = $"{book.Title}{suffix}.pdf";
            return PhysicalFile(path, "application/pdf", fileName);
        }

        // ───── helpers ─────

        private async Task<bool> CurrentUserHasMembershipAsync()
        {
            if (User.Identity?.IsAuthenticated != true) return false;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return false;

            var memberId = await _context.Members
                .Where(m => m.ApplicationUserId == userId)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync();

            if (memberId == null) return false;

            return await _context.Memberships.AnyAsync(m =>
                m.MemberId == memberId &&
                m.IsActive &&
                m.EndDate >= DateTime.UtcNow);
        }

        // PDF files are uploaded by the admin app into ITS wwwroot. From the
        // user app we look in our own wwwroot first (in case a deployment
        // copied them), then fall back to the sibling admin wwwroot.
        private string? ResolvePdfPath(string pdfUrl)
        {
            var relPath = pdfUrl.TrimStart('/')
                                .Replace('/', Path.DirectorySeparatorChar);

            var local = Path.Combine(_env.WebRootPath, relPath);
            if (System.IO.File.Exists(local))
                return local;

            // user-app ContentRoot = .../Library_Management_System
            // admin-app wwwroot    = .../LibraryManagementSystem/wwwroot
            var adminWwwroot = Path.GetFullPath(Path.Combine(
                _env.ContentRootPath, "..",
                "LibraryManagementSystem", "wwwroot"));

            var admin = Path.Combine(adminWwwroot, relPath);
            return System.IO.File.Exists(admin) ? admin : null;
        }
    }
}
