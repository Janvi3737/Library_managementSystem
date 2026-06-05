using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // MAIN REPORT PAGE

        public async Task<IActionResult> Index()
        {
            var model = new ReportViewModel();

            // TOTAL COUNTS

            model.TotalBooks = await _context.Books.CountAsync();

            model.TotalUsers = await _context.Users.CountAsync();

            model.TotalIssuedBooks = await _context.BorrowRecords
                .CountAsync(x => x.ReturnedOn == null);

            model.TotalOverdueBooks = await _context.BorrowRecords
                .CountAsync(x =>
                    x.ReturnedOn == null &&
                    x.DueDate < DateTime.Now);

            // TOTAL FINE

            var fineRecords = await _context.BorrowRecords
                .Where(x => x.FinePaid)
                .ToListAsync();

            model.TotalFineCollection =
                fineRecords.Sum(x => x.FineAmount);

            // MOST BORROWED BOOKS

            model.MostBorrowedBooks = await _context.BorrowRecords
                .Include(x => x.Book)
                .GroupBy(x => x.Book.Title)
                .Select(g => new MostBorrowedBookVM
                {
                    BookName = g.Key,
                    BorrowCount = g.Count()
                })
                .OrderByDescending(x => x.BorrowCount)
                .Take(10)
                .ToListAsync();

            // ISSUED BOOKS

            model.IssuedBooks = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.Member)
                .Where(x => x.ReturnedOn == null)
                .Select(x => new IssuedBookVM
                {
                    BookName = x.Book.Title,

                    MemberName = x.Member.Name,

                    IssuedOn = x.IssuedOn,

                    DueDate = x.DueDate
                })
                .OrderByDescending(x => x.IssuedOn)
                .Take(10)
                .ToListAsync();

            // RETURNED BOOKS

            model.ReturnedBooks = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.Member)
                .Where(x => x.ReturnedOn != null)
                .Select(x => new ReturnedBookVM
                {
                    BookName = x.Book.Title,

                    MemberName = x.Member.Name,

                    IssuedOn = x.IssuedOn,

                    ReturnedOn = x.ReturnedOn.Value,

                    FineAmount = x.FineAmount
                })
                .OrderByDescending(x => x.ReturnedOn)
                .Take(10)
                .ToListAsync();

            // LATE RETURNS

            var lateData = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.Member)
                .Where(x =>
                    x.ReturnedOn != null &&
                    x.ReturnedOn > x.DueDate)
                .ToListAsync();

            model.LateReturns = lateData
                .Select(x => new LateReturnVM
                {
                    BookName = x.Book.Title,

                    MemberName = x.Member.Name,

                    DueDate = x.DueDate,

                    ReturnedOn = x.ReturnedOn.Value,

                    LateDays =
                        (x.ReturnedOn.Value - x.DueDate).Days,

                    FineAmount = x.FineAmount
                })
                .OrderByDescending(x => x.LateDays)
                .Take(10)
                .ToList();

            // PENDING RESERVATIONS

            // model.PendingReservations = await _context.Reservations

            //         MemberName =

            //         ReservedOn = x.ReservedOn

            // PENDING RESERVATIONS

            var reservationData = await _context.Reservations
                .Include(r => r.Book)
                .Include(r => r.Member)
                .ToListAsync();

            model.PendingReservations = reservationData
                .Where(r => r.Status != null &&
                            r.Status.ToString().ToLower() == "pending")
                .Select(r => new PendingReservationVM
                {
                    BookName = r.Book != null ? r.Book.Title : "",
                    MemberName = r.Member != null ? r.Member.FullName : "",
                    ReservedOn = r.ReservedOn
                })
                .ToList();

            // BORROW CHART

            var borrowChart = await _context.BorrowRecords
                .GroupBy(x => x.IssuedOn.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            model.BorrowChartLabels = borrowChart
                .Select(x =>
                    new DateTime(1, x.Month, 1)
                    .ToString("MMM"))
                .ToList();

            model.BorrowChartData = borrowChart
                .Select(x => x.Count)
                .ToList();

            // CATEGORY CHART

            var categoryChart = await _context.Books
                .Include(x => x.Category)
                .GroupBy(x => x.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            model.CategoryLabels = categoryChart
                .Select(x => x.Category)
                .ToList();

            model.CategoryData = categoryChart
                .Select(x => x.Count)
                .ToList();

            // FINE CHART

            // FINE CHART

            var fineChartRaw = await _context.BorrowRecords
                .Where(x => x.FinePaid && x.ReturnedOn != null)
                .ToListAsync();

            var fineChart = fineChartRaw
                .GroupBy(x => x.ReturnedOn!.Value.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Sum(x => x.FineAmount)
                })
                .OrderBy(x => x.Month)
                .ToList();

            model.FineChartLabels = fineChart
                .Select(x => new DateTime(2025, x.Month, 1).ToString("MMM"))
                .ToList();

            model.FineChartData = fineChart
                .Select(x => x.Total)
                .ToList();
            return View(model);
        }

        public async Task<IActionResult> Reports()
        {
            var model = new ReportViewModel();

            var reviews = await _context.BookReviews
            .AsNoTracking()
            .Include(r => r.Book)
            .Include(r => r.Member)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

            model.TotalReviews = reviews.Count;

            model.AverageRating = reviews.Any()
            ? reviews.Average(r => r.Rating)
            : 0;

            model.BookReviews = reviews.Select(r => new ReviewVM
            {
                BookName = r.Book?.Title ?? "",
                MemberName = r.Member?.FullName ?? "",
                Rating = r.Rating,
                Comment = string.IsNullOrEmpty(r.Comment) ? "-" : r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();

            return View(model);
        }

    }

}
