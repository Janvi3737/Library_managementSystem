using System.Security.Claims;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

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
    ///   - Otherwise they get the first 20 pages of the book ONLY
    ///     (extracted server-side via PdfSharpCore so the full file
    ///     never reaches the client).
    /// </summary>
    public class BooksController : Controller
    {
        // Non-members get this many pages of any book, full stop.
        // Tweak here (or move to LibrarySettings later) if 20 isn't right.
        private const int PreviewPageCount = 20;

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
            if (book == null)
                return PlaceholderHtml($"Book #{id} not found.");

            var hasMembership = await CurrentUserHasMembershipAsync();

            // We always serve from book.PdfUrl — for non-members we shrink
            // it to PreviewPageCount on the fly. No separate preview upload
            // is needed. (PreviewPdfUrl is still respected when it exists,
            // letting an admin force a custom preview if they want.)
            string? source = (!hasMembership && !string.IsNullOrEmpty(book.PreviewPdfUrl))
                ? book.PreviewPdfUrl
                : book.PdfUrl;

            if (string.IsNullOrEmpty(source))
                return PlaceholderHtml(
                    $"\"{book.Title}\" has no PDF uploaded yet. " +
                    "Open the admin app, edit this book, and upload a PDF file under \"Pdf File\".");

            var resolved = ResolvePdfPathWithDiag(source);
            if (resolved.Found == null)
                return PlaceholderHtml(
                    $"PDF for \"{book.Title}\" could not be located on disk.\n\n" +
                    resolved.Diagnostic);

            // Members get the file as-is.
            if (hasMembership)
                return PhysicalFile(resolved.Found, "application/pdf");

            // Non-members get the first PreviewPageCount pages, copied into
            // a fresh PDF in memory and streamed back.
            try
            {
                var trimmedBytes = ExtractFirstPages(resolved.Found, PreviewPageCount);
                return File(trimmedBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return PlaceholderHtml(
                    $"Could not generate preview for \"{book.Title}\".\n\n{ex.Message}");
            }
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
            if (book == null)
                return PlaceholderHtml($"Book #{id} not found.");

            var hasMembership = await CurrentUserHasMembershipAsync();

            string? source = (!hasMembership && !string.IsNullOrEmpty(book.PreviewPdfUrl))
                ? book.PreviewPdfUrl
                : book.PdfUrl;

            if (string.IsNullOrEmpty(source))
                return PlaceholderHtml(
                    $"\"{book.Title}\" has no PDF available to download.");

            var path = ResolvePdfPath(source);
            if (path == null)
                return PlaceholderHtml(
                    $"PDF file recorded but missing on disk: {source}");

            var suffix = hasMembership ? "" : "-preview";
            var fileName = $"{book.Title}{suffix}.pdf";

            // Members download the full file. Non-members can ONLY download
            // the first PreviewPageCount pages — extracted server-side so
            // the full PDF never crosses the wire.
            if (hasMembership)
                return PhysicalFile(path, "application/pdf", fileName);

            try
            {
                var trimmedBytes = ExtractFirstPages(path, PreviewPageCount);
                return File(trimmedBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return PlaceholderHtml(
                    $"Could not generate preview for \"{book.Title}\".\n\n{ex.Message}");
            }
        }

        // Reads the PDF on disk and returns a new in-memory PDF containing
        // only the first `count` pages. Used to enforce the non-member
        // preview cap WITHOUT ever streaming the full file to the browser.
        private static byte[] ExtractFirstPages(string sourcePath, int count)
        {
            using var input = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
            using var output = new PdfDocument();
            output.Info.Title = input.Info.Title;
            output.Info.Subject = "Preview - first " + count + " pages";

            var pages = Math.Min(count, input.PageCount);
            for (int i = 0; i < pages; i++)
                output.AddPage(input.Pages[i]);

            using var ms = new MemoryStream();
            output.Save(ms, false);
            return ms.ToArray();
        }

        // Renders a minimal HTML page so the message is visible inside the
        // iframe instead of a bare 404 / blank PDF reader.
        private IActionResult PlaceholderHtml(string message)
        {
            var html = $@"<!doctype html>
<html><head><meta charset='utf-8'><title>PDF unavailable</title>
<style>
  body {{ margin:0; font-family: -apple-system, Segoe UI, Roboto, sans-serif;
          background:#0f172a; color:#e2e8f0; display:flex;
          align-items:flex-start; justify-content:center; min-height:100vh; padding:36px 24px; }}
  .box {{ max-width:760px; padding:32px; background:#1e293b; border-radius:16px; }}
  h2 {{ margin:0 0 16px; font-size:20px; color:#fbbf24; }}
  pre {{ margin:0; white-space:pre-wrap; word-break:break-all;
         font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
         font-size:13px; line-height:1.6; color:#cbd5e1; }}
</style></head><body><div class='box'>
<h2>PDF not available</h2><pre>{System.Net.WebUtility.HtmlEncode(message)}</pre>
</div></body></html>";
            return Content(html, "text/html");
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
        private (string? Found, string Diagnostic) ResolvePdfPathWithDiag(string pdfUrl)
        {
            var diag = new System.Text.StringBuilder();
            diag.AppendLine($"DB value: {pdfUrl}");

            // The admin SHOULD store relative paths like "/uploads/pdfs/abc.pdf",
            // but defensively handle absolute URLs too (older rows often
            // contain "https://localhost:7113/uploads/pdfs/abc.pdf" because
            // the field was edited via a tool that captured the full URL).
            if (Uri.TryCreate(pdfUrl, UriKind.Absolute, out var uri))
            {
                diag.AppendLine($"Parsed as absolute URI -> AbsolutePath: {uri.AbsolutePath}");
                pdfUrl = uri.AbsolutePath;
            }
            else
            {
                diag.AppendLine("Treated as relative path.");
            }

            var relPath = pdfUrl.TrimStart('/')
                                .Replace('/', Path.DirectorySeparatorChar);

            var local = Path.Combine(_env.WebRootPath, relPath);
            diag.AppendLine($"Tried user-app wwwroot: {local} (exists={System.IO.File.Exists(local)})");
            if (System.IO.File.Exists(local))
                return (local, diag.ToString());

            // user-app ContentRoot = .../Library_Management_System
            // admin-app wwwroot    = .../LibraryManagementSystem/wwwroot
            var adminWwwroot = Path.GetFullPath(Path.Combine(
                _env.ContentRootPath, "..",
                "LibraryManagementSystem", "wwwroot"));

            var admin = Path.Combine(adminWwwroot, relPath);
            diag.AppendLine($"Tried admin-app wwwroot: {admin} (exists={System.IO.File.Exists(admin)})");
            return (System.IO.File.Exists(admin) ? admin : null, diag.ToString());
        }

        // Convenience wrapper that drops the diagnostic.
        private string? ResolvePdfPath(string pdfUrl) =>
            ResolvePdfPathWithDiag(pdfUrl).Found;
    }
}
