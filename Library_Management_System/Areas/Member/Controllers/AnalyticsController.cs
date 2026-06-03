using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member")]
    public class AnalyticsController : Controller
    {
        private readonly AppDbContext _context;

        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var member = await _context.Members
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == userId);

            if (member == null)
                return RedirectToAction("Login", "Account");

            var borrows = await _context.BorrowRecords
                .Include(x => x.Book)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Book)
                    .ThenInclude(x => x.Author)
                .Where(x => x.MemberId == member.Id)
                .ToListAsync();

            var totalBorrowed = borrows.Count;

            var currentBorrowed = borrows
                .Count(x => x.ReturnedOn == null);

            var returnedBooks = borrows
                .Count(x => x.ReturnedOn != null);

            var overdueBooks = borrows
                .Count(x =>
                    x.ReturnedOn == null &&
                    x.DueDate < DateTime.Now);

            var totalFine = borrows.Sum(x =>
                x.DueDate < DateTime.Now
                ? (decimal)((DateTime.Now - x.DueDate).Days *
                  (x.FinePerDay > 0 ? x.FinePerDay : 5))
                : 0);

            var booksThisMonth = borrows.Count(x =>
                x.IssuedOn.Month == DateTime.Now.Month &&
                x.IssuedOn.Year == DateTime.Now.Year);

            var wishlistCount = await _context.Wishlists
                .CountAsync(x => x.MemberId == userId);

            var favoriteCategory = borrows
                .GroupBy(x => x.Book.Category.Name)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .FirstOrDefault() ?? "N/A";

            var favoriteAuthor = borrows
                .GroupBy(x => x.Book.Author.Name)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .FirstOrDefault() ?? "N/A";

            double completionRate = totalBorrowed == 0
                ? 0
                : ((double)returnedBooks / totalBorrowed) * 100;

            var today = DateTime.Today;

            var monthlyData = Enumerable.Range(0, 6)
                .Select(i =>
                {
                    var monthDate = new DateTime(
                        today.Year,
                        today.Month,
                        1).AddMonths(-(5 - i));

                    var monthRecords = borrows.Where(x =>
                        x.IssuedOn.Year == monthDate.Year &&
                        x.IssuedOn.Month == monthDate.Month);

                    return new MonthlyAnalyticsViewModel
                    {
                        Month = monthDate.ToString("MMM yyyy"),

                        Borrowed = monthRecords.Count(),

                        Returned = monthRecords.Count(x =>
                            x.ReturnedOn != null),

                        Fine = monthRecords.Sum(x =>
                            x.DueDate < DateTime.Now
                            ? (decimal)((DateTime.Now - x.DueDate).Days *
                                (x.FinePerDay > 0 ? x.FinePerDay : 5))
                            : 0)
                    };
                })
                .ToList();

            var model = new MemberAnalyticsViewModel
            {
                TotalBorrowed = totalBorrowed,
                CurrentBorrowed = currentBorrowed,
                TotalReturned = returnedBooks,
                OverdueBooks = overdueBooks,
                TotalFine = totalFine,
                BooksThisMonth = booksThisMonth,
                TotalWishlist = wishlistCount,
                FavoriteCategory = favoriteCategory,
                FavoriteAuthor = favoriteAuthor,
                CompletionRate = Math.Round(completionRate, 2),
                MonthlyData = monthlyData
            };

            return View(model);
        }
    }
}
