using System.Security.Claims;
using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    public class CatalogController : Controller
    {
        private readonly AppDbContext _context;

        public CatalogController(AppDbContext context)
        {
            _context = context;
        }

        // 📚 INDEX 
        public async Task<IActionResult> Index(
            string? searchQuery,
            int? categoryId,
            int? authorId,
            string? sortBy,
            string? availability,
            int pageNumber = 1)
        {
            const int pageSize = 8;

            var booksQuery = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrEmpty(searchQuery))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(searchQuery) ||
                    (b.Description != null && b.Description.Contains(searchQuery)));
            }

            // CATEGORY FILTER
            if (categoryId.HasValue)
                booksQuery = booksQuery.Where(b => b.CategoryId == categoryId.Value);

            // AUTHOR FILTER
            if (authorId.HasValue)
                booksQuery = booksQuery.Where(b => b.AuthorId == authorId.Value);

            // AVAILABILITY
            if (!string.IsNullOrEmpty(availability))
            {
                if (availability == "available")
                    booksQuery = booksQuery.Where(b => b.AvailableCopies > 0);
                else if (availability == "unavailable")
                    booksQuery = booksQuery.Where(b => b.AvailableCopies <= 0);
            }

            // SORTING
            booksQuery = sortBy switch
            {
                "title" => booksQuery.OrderBy(b => b.Title),
                "newest" => booksQuery.OrderByDescending(b => b.CreatedAt),
                "popular" => booksQuery.OrderByDescending(b => b.BorrowCount),
                _ => booksQuery.OrderByDescending(b => b.Id),
            };

            int totalBooks = await booksQuery.CountAsync();

            var books = await booksQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // USER WISHLIST
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            List<int> wishlistIds = new();

            if (userId != null)
            {
                wishlistIds = await _context.Wishlists
                 .Where(w => w.MemberId == userId && w.BookId != null)
                 .Select(w => w.BookId.Value)
                 .ToListAsync();
            }

            var vm = new CatalogViewModel
            {
                SearchQuery = searchQuery,
                CategoryId = categoryId,
                AuthorId = authorId,
                SortBy = sortBy,
                Availability = availability,
                PageNumber = pageNumber,
                TotalPages = (int)Math.Ceiling(totalBooks / (double)pageSize),
                PagedBooks = books,
                Categories = await _context.Categories.ToListAsync(),
                Authors = await _context.Authors.ToListAsync()
            };

            ViewBag.WishlistIds = wishlistIds;

            return View(vm);
        }

        // 📖 DETAILS PAGE
        public async Task<IActionResult> Details(int id)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            var reviews = await _context.BookReviews
                .Include(r => r.Member)
                .Where(r => r.BookId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            double avgRating = 0;

            if (reviews.Any())
            {
                avgRating = reviews.Average(r => r.Rating);
            }

            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            bool isWishlisted = false;

            if (userId != null)
            {
                isWishlisted = await _context.Wishlists
                    .AnyAsync(w => w.BookId == id && w.MemberId == userId);
            }

            bool hasMembership = false;

            if (!string.IsNullOrEmpty(userId))
            {
                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.ApplicationUserId == userId);

                if (member != null)
                {
                    hasMembership = await _context.Memberships
                        .AnyAsync(m =>
                            m.MemberId == member.Id &&
                            m.IsActive &&
                            m.EndDate >= DateTime.UtcNow);
                }
            }

            var vm = new BookDetailsViewModel
            {
                Id = book.Id,
                Title = book.Title,
                Description = book.Description,
                CoverImageUrl = book.CoverImageUrl,
                PdfUrl = book.PdfUrl,
                PreviewPdfUrl = book.PreviewPdfUrl,
                AuthorName = book.Author?.Name,
                CategoryName = book.Category?.Name,

                IsAvailable = book.AvailableCopies > 0,
                AvailableCopies = book.AvailableCopies,
                IsWishlisted = isWishlisted,

                AverageRating = avgRating,
                TotalReviews = reviews.Count,
                Reviews = reviews,

                HasMembership = hasMembership
            };

            return View(vm);
        }
        // 📚 BY CATEGORY
        public async Task<IActionResult> ByCategory()
        {
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .OrderBy(b => b.Category.Name)
                .ToListAsync();

            var vm = new CatalogViewModel
            {
                PagedBooks = books,
                Categories = await _context.Categories.ToListAsync(),
                Authors = await _context.Authors.ToListAsync()
            };

            return View(vm);
        }

        // 👨‍🏫 BY AUTHOR
        public async Task<IActionResult> ByAuthor()
        {
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .OrderBy(b => b.Author.Name)
                .ToListAsync();

            var vm = new CatalogViewModel
            {
                PagedBooks = books,
                Categories = await _context.Categories.ToListAsync(),
                Authors = await _context.Authors.ToListAsync()
            };

            return View(vm);
        }

        // 🆕 NEW ARRIVALS (LAST 30 DAYS)
        public async Task<IActionResult> NewArrivals()
        {
            // Book.CreatedAt is written with DateTime.UtcNow (see Book.cs); use
            // the matching base so the 30-day window agrees with HomeController.
            var date = DateTime.UtcNow.AddDays(-30);

            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Where(b => b.CreatedAt >= date)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var vm = new CatalogViewModel
            {
                PagedBooks = books,
                Categories = await _context.Categories.ToListAsync(),
                Authors = await _context.Authors.ToListAsync()
            };

            return View(vm);
        }


        // 🔍 SEARCH PAGE (OPTIONAL)
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return RedirectToAction("Index");

            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Where(b => b.Title.Contains(q))
                .ToListAsync();

            return View("Index", new CatalogViewModel
            {
                SearchQuery = q,
                PagedBooks = books,
                Categories = await _context.Categories.ToListAsync(),
                Authors = await _context.Authors.ToListAsync()
            });
        }

        public async Task<IActionResult> MostPopularBooks()
        {
            var books = await _context.BorrowRecords
                .Include(x => x.Book)
                    .ThenInclude(b => b.Author)
                .Include(x => x.Book)
                    .ThenInclude(b => b.Category)
                .GroupBy(x => x.BookId)
                .Select(g => new MostPopularBookViewModel
                {
                    BookId = g.Key,
                    Title = g.First().Book.Title,
                    ISBN = g.First().Book.ISBN,
                    AuthorName = g.First().Book.Author.Name,
                    CategoryName = g.First().Book.Category.Name,
                    CoverImageUrl = g.First().Book.CoverImageUrl,
                    TotalBorrows = g.Count()
                })
                .OrderByDescending(x => x.TotalBorrows)
                .Take(10)
                .ToListAsync();

            return View(books);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview(int bookId,int rating,string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var review = new BookReview
            {
                BookId = bookId,
                MemberId = userId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.BookReviews.Add(review);

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = bookId });
        }
    }
}
