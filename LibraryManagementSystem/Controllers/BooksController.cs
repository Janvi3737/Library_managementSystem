using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using System.Globalization;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BooksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ExportService _exportService;

        public BooksController(
            AppDbContext context,
            ExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }

        // =========================
        // INDEX + SEARCH
        // =========================
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Department)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    b.Title.Contains(search) ||

                    (b.ISBN != null &&
                     b.ISBN.Contains(search)) ||

                    (b.Author != null &&
                     b.Author.Name.Contains(search)) ||

                    (b.Category != null &&
                     b.Category.Name.Contains(search)) ||

                    (b.Department != null &&
                     b.Department.Name.Contains(search))
                );
            }

            var data = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;

            return View(data);
        }

        // =========================
        // DETAILS
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            var borrowHistory = await _context.BorrowRecords
                .Include(b => b.Member)
                .Where(b => b.BookId == id)
                .OrderByDescending(b => b.IssuedOn)
                .AsNoTracking()
                .ToListAsync();

            var vm = new BookDetailsViewModel
            {
                Book = book,
                BorrowHistory = borrowHistory
            };

            return View(vm);
        }

        // =========================
        // CREATE GET
        // =========================
        [HttpGet]
        public IActionResult Create()
        {
            LoadDropdowns();

            return View();
        }

        // =========================
        // CREATE POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Book book,
            IFormFile? PdfFile)
        {
            ValidateBook(book);

            bool isbnExists = await _context.Books
                .AnyAsync(b => b.ISBN == book.ISBN);

            if (isbnExists)
            {
                ModelState.AddModelError(
                    "ISBN",
                    "This ISBN already exists.");
            }

            book.AvailableCopies = book.TotalCopies;

            // PDF UPLOAD
            if (PdfFile != null && PdfFile.Length > 0)
            {
                // CHECK PDF EXTENSION
                if (Path.GetExtension(PdfFile.FileName)
                    .ToLower() != ".pdf")
                {
                    ModelState.AddModelError(
                        "PdfFile",
                        "Only PDF files are allowed.");
                }
                else
                {
                    string pdfFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "pdfs");

                    // CREATE FOLDER
                    if (!Directory.Exists(pdfFolder))
                    {
                        Directory.CreateDirectory(pdfFolder);
                    }

                    // UNIQUE FILE NAME
                    string fileName =
                        Guid.NewGuid().ToString()
                        + Path.GetExtension(PdfFile.FileName);

                    string filePath =
                        Path.Combine(pdfFolder, fileName);

                    // SAVE FILE
                    using (var stream =
                           new FileStream(filePath, FileMode.Create))
                    {
                        await PdfFile .CopyToAsync(stream);
                    }

                    // SAVE URL
                    book.PdfUrl = "/uploads/pdfs/" + fileName;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    book.CreatedAt = DateTime.UtcNow;

                    _context.Books.Add(book);

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Book added successfully!";

                    return RedirectToAction(nameof(Index));
                }
                catch
                {
                    TempData["Error"] =
                        "Something went wrong while saving.";
                }
            }

            LoadDropdowns(
                book.CategoryId,
                book.AuthorId,
                book.DepartmentId);

            return View(book);
        }

        // =========================
        // EDIT GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            LoadDropdowns(
                book.CategoryId,
                book.AuthorId,
                book.DepartmentId);

            return View(book);
        }

        // =========================
        // EDIT POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Book book,
            IFormFile? PdfFile)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            ValidateBook(book);

            // CHECK DUPLICATE ISBN
            bool isbnExists = await _context.Books
                .AnyAsync(b =>
                    b.ISBN == book.ISBN &&
                    b.Id != book.Id);

            if (isbnExists)
            {
                ModelState.AddModelError(
                    "ISBN",
                    "This ISBN already exists.");
            }

            // AVAILABLE COPIES VALIDATION
            if (book.AvailableCopies > book.TotalCopies)
            {
                ModelState.AddModelError(
                    "AvailableCopies",
                    "Available copies cannot exceed total copies.");
            }

            if (!ModelState.IsValid)
            {
                LoadDropdowns(
                    book.CategoryId,
                    book.AuthorId,
                    book.DepartmentId);

                return View(book);
            }

            try
            {
                var existingBook = await _context.Books
     .FirstOrDefaultAsync(b => b.Id == id);

                if (existingBook == null)
                {
                    return NotFound();
                }

                // UPDATE BOOK
                existingBook.Title = book.Title;
                existingBook.ISBN = book.ISBN;
                existingBook.AuthorId = book.AuthorId;
                existingBook.CategoryId = book.CategoryId;
                existingBook.DepartmentId = book.DepartmentId;
                existingBook.TotalPages = book.TotalPages;
                existingBook.Description = book.Description;
                existingBook.CoverImageUrl = book.CoverImageUrl;
                existingBook.TotalCopies = book.TotalCopies;
                existingBook.AvailableCopies = book.AvailableCopies;
                existingBook.IsFeatured = book.IsFeatured;

                // PDF UPLOAD
                if (PdfFile != null && PdfFile.Length > 0)
                {
                    // VALIDATE PDF
                    if (Path.GetExtension(PdfFile.FileName)
                        .ToLower() != ".pdf")
                    {
                        ModelState.AddModelError(
                            "PdfFile",
                            "Only PDF files are allowed.");

                        LoadDropdowns(
                            book.CategoryId,
                            book.AuthorId,
                            book.DepartmentId);

                        return View(book);
                    }

                    string pdfFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "pdfs");

                    // CREATE FOLDER
                    if (!Directory.Exists(pdfFolder))
                    {
                        Directory.CreateDirectory(pdfFolder);
                    }

                    // DELETE OLD PDF
                    if (!string.IsNullOrWhiteSpace(existingBook.PdfUrl))
                    {
                        string oldPdfPath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            existingBook.PdfUrl.TrimStart('/'));

                        if (System.IO.File.Exists(oldPdfPath))
                        {
                            System.IO.File.Delete(oldPdfPath);
                        }
                    }

                    // NEW FILE NAME
                    string fileName =
                        Guid.NewGuid().ToString()
                        + Path.GetExtension(PdfFile.FileName);

                    string filePath =
                        Path.Combine(pdfFolder, fileName);

                    // SAVE NEW PDF
                    using (var stream =
                           new FileStream(filePath, FileMode.Create))
                    {
                        await PdfFile.CopyToAsync(stream);
                        if (!System.IO.File.Exists(filePath))
                        {
                            throw new Exception("PDF not saved.");
                        }
                    }

                    // UPDATE PDF URL
                    existingBook.PdfUrl = "/uploads/pdfs/" + fileName;
                }

                _context.Books.Update(existingBook);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Book updated successfully!";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                LoadDropdowns(
                    book.CategoryId,
                    book.AuthorId,
                    book.DepartmentId);

                return View(book);
            }
        }

        // =========================
        // DELETE GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        // =========================
        // DELETE POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Book model)
        {
            var book = await _context.Books
                .Include(b => b.BorrowRecords)
                .FirstOrDefaultAsync(b => b.Id == model.Id);

            if (book == null)
                return NotFound();

            if (book.BorrowRecords != null &&
                book.BorrowRecords.Any())
            {
                TempData["Error"] =
                    "Cannot delete book with borrow history!";

                return RedirectToAction(nameof(Index));
            }

            // DELETE PDF
            if (!string.IsNullOrWhiteSpace(book.PdfUrl))
            {
                string pdfPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    book.PdfUrl.TrimStart('/'));

                if (System.IO.File.Exists(pdfPath))
                {
                    System.IO.File.Delete(pdfPath);
                }
            }

            _context.Books.Remove(book);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Book deleted successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EXPORT EXCEL
        // =========================
        public async Task<IActionResult> ExportExcel()
        {
            var books = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Department)
                .AsNoTracking()
                .ToListAsync();

            var file = _exportService.ExportBooks(books);

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Books.xlsx"
            );
        }

        // =========================
        // IMPORT GET
        // =========================
        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        // =========================
        // BULK DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No books selected.";
                return RedirectToAction(nameof(Index));
            }

            int deleted = 0;
            int skipped = 0;

            foreach (var bookId in ids)
            {
                try
                {
                    var b = await _context.Books
                        .FindAsync(bookId);

                    if (b == null)
                        continue;

                    bool inUse = await _context.BorrowRecords
                        .AnyAsync(br => br.BookId == bookId);

                    if (inUse)
                    {
                        skipped++;
                        continue;
                    }

                    // DELETE PDF
                    if (!string.IsNullOrWhiteSpace(b.PdfUrl))
                    {
                        string pdfPath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            b.PdfUrl.TrimStart('/'));

                        if (System.IO.File.Exists(pdfPath))
                        {
                            System.IO.File.Delete(pdfPath);
                        }
                    }

                    _context.Books.Remove(b);

                    await _context.SaveChangesAsync();

                    deleted++;
                }
                catch
                {
                    skipped++;
                }
            }

            TempData["Success"] = skipped > 0
                ? $"Deleted {deleted}. Skipped {skipped}."
                : $"Deleted {deleted}.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // IMPORT POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(
            BookImportViewModel model)
        {
            try
            {
                if (model.CsvFile == null ||
                    model.CsvFile.Length == 0)
                {
                    TempData["Error"] =
                        "Please upload a CSV file.";

                    return View(model);
                }

                var books = new List<Book>();

                using var stream =
                    new StreamReader(
                        model.CsvFile.OpenReadStream());

                using var csv =
                    new CsvReader(
                        stream,
                        CultureInfo.InvariantCulture);

                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    try
                    {
                        int authorId =
                            csv.GetField<int>("AuthorId");

                        int categoryId =
                            csv.GetField<int>("CategoryId");

                        int? departmentId = null;

                        string? departmentText = null;

                        try
                        {
                            departmentText =
                                csv.GetField("DepartmentId");
                        }
                        catch
                        {
                            departmentText = null;
                        }

                        if (!string.IsNullOrWhiteSpace(departmentText))
                        {
                            departmentId =
                                int.Parse(departmentText);
                        }

                        bool authorExists =
                            await _context.Authors
                                .AnyAsync(a => a.Id == authorId);

                        bool categoryExists =
                            await _context.Categories
                                .AnyAsync(c => c.Id == categoryId);

                        bool departmentExists = true;

                        if (departmentId.HasValue)
                        {
                            departmentExists =
                                await _context.Departments
                                    .AnyAsync(d =>
                                        d.Id == departmentId.Value);
                        }

                        if (!authorExists ||
                            !categoryExists ||
                            !departmentExists)
                        {
                            continue;
                        }

                        string isbn =
                            csv.GetField("ISBN");

                        bool isbnExists =
                            await _context.Books
                                .AnyAsync(b => b.ISBN == isbn);

                        if (isbnExists)
                        {
                            continue;
                        }

                        var book = new Book
                        {
                            Title = csv.GetField("Title"),
                            ISBN = isbn,
                            AuthorId = authorId,
                            CategoryId = categoryId,
                            DepartmentId = departmentId,
                            TotalCopies = csv.GetField<int>("TotalCopies"),
                            AvailableCopies = csv.GetField<int>("AvailableCopies"),
                            TotalPages = csv.GetField<int>("TotalPages"),
                            Description = csv.GetField("Description"),
                            CoverImageUrl = csv.GetField("CoverImageUrl"),
                            IsFeatured = csv.GetField<bool>("IsFeatured"),
                            CreatedAt = DateTime.UtcNow
                        };

                        books.Add(book);
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (books.Any())
                {
                    await _context.Books.AddRangeAsync(books);

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        $"{books.Count} books imported successfully!";
                }
                else
                {
                    TempData["Error"] =
                        "No valid records found in CSV.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return View(model);
            }
        }

        // =========================
        // PRIVATE METHODS
        // =========================

        private void LoadDropdowns(
            int? categoryId = null,
            int? authorId = null,
            int? departmentId = null)
        {
            ViewBag.CategoryList =
                new SelectList(
                    _context.Categories,
                    "Id",
                    "Name",
                    categoryId);

            ViewBag.AuthorList =
                new SelectList(
                    _context.Authors,
                    "Id",
                    "Name",
                    authorId);

            ViewBag.DepartmentList =
                new SelectList(
                    _context.Departments,
                    "Id",
                    "Name",
                    departmentId);
        }

        private void ValidateBook(Book book)
        {
            if (book.CategoryId == 0)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Select Category");
            }

            if (book.AuthorId == 0)
            {
                ModelState.AddModelError(
                    "AuthorId",
                    "Select Author");
            }

            if (book.TotalCopies <= 0)
            {
                ModelState.AddModelError(
                    "TotalCopies",
                    "Enter valid total copies");
            }

            if (book.TotalPages <= 0)
            {
                ModelState.AddModelError(
                    "TotalPages",
                    "Enter valid total pages");
            }
        }

        public IActionResult ViewPdf(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);

            if (book == null || string.IsNullOrEmpty(book.PdfUrl))
                return NotFound();

            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                book.PdfUrl.TrimStart('/'));

            return PhysicalFile(path, "application/pdf");
        }
    }
}
