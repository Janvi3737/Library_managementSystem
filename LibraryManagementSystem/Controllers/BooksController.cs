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
        public async Task<IActionResult> Create(Book book)
        {
            ValidateBook(book);

            bool isbnExists = await _context.Books
                .AnyAsync(b => b.ISBN == book.ISBN);

            if (isbnExists)
            {
                ModelState.AddModelError(
                    "ISBN",
                    "This ISBN already exists."
                );
            }

            book.AvailableCopies = book.TotalCopies;

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
                .FindAsync(id);

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
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            // VALIDATION
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

            // CHECK AVAILABLE COPIES
            if (book.AvailableCopies > book.TotalCopies)
            {
                ModelState.AddModelError(
                    "AvailableCopies",
                    "Available copies cannot exceed total copies.");
            }

            // MODEL VALIDATION
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
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (existingBook == null)
                {
                    return NotFound();
                }

                // UPDATE
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
    }
}
