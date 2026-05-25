using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using LibraryManagementSystem.ClassLibrary.Models;

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

        // OVERDUE BOOKS REPORT
        public async Task<IActionResult> OverdueBooks()
        {
            var overdueBooks = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .Where(b =>
                    b.Status == "Issued" &&
                    b.DueDate < DateTime.Now)
                .OrderBy(b => b.DueDate)
                .ToListAsync();

            return View(overdueBooks);
        }

        // TOP BORROWERS REPORT
        public async Task<IActionResult> TopBorrowers()
        {
            var data = await _context.BorrowRecords
                .Include(b => b.Member)
                .Where(b => b.Member != null)
                .GroupBy(b => new { b.MemberId, b.Member.Name })
                .Select(g => new TopBorrowerViewModel
                {
                    MemberName = g.Key.Name,
                    TotalBooks = g.Count()
                })
                .OrderByDescending(x => x.TotalBooks)
                .Take(10)
                .ToListAsync();

            return View(data);
        }

        // BOOKS THAT HAVE NEVER BEEN BORROWED
        public async Task<IActionResult> NeverBorrowed()
        {
            var borrowedBookIds = _context.BorrowRecords.Select(b => b.BookId);

            var books = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Where(b => !borrowedBookIds.Contains(b.Id))
                .OrderBy(b => b.Title)
                .ToListAsync();

            return View(books);
        }

        // MOST WISHLISTED — purchasing-decision support
        public async Task<IActionResult> MostWishlisted()
        {
            var rows = await _context.Wishlists
                .Where(w => w.BookId != null)
                .GroupBy(w => w.BookId!.Value)
                .Select(g => new
                {
                    BookId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(20)
                .ToListAsync();

            var bookIds = rows.Select(r => r.BookId).ToList();
            var books = await _context.Books
                .Include(b => b.Author)
                .Where(b => bookIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id);

            var vm = rows
                .Where(r => books.ContainsKey(r.BookId))
                .Select(r => new Tuple<LibraryManagementSystem.ClassLibrary.Models.Book, int>(
                    books[r.BookId], r.Count))
                .ToList();

            return View(vm);
        }

        // REVENUE — by month, from approved membership payments + paid fines.
        public async Task<IActionResult> Revenue()
        {
            var paymentsByMonth = await _context.MembershipPayments
                .Where(p => p.PaymentStatus == "Approved")
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    PaymentRevenue = g.Sum(p => p.Amount)
                })
                .ToListAsync();

            var finesByMonth = await _context.BorrowRecords
                .Where(b => b.FineAmount > 0 && b.FinePaid && b.ReturnedOn != null)
                .GroupBy(b => new { b.ReturnedOn!.Value.Year, b.ReturnedOn!.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    FineRevenue = g.Sum(b => b.FineAmount)
                })
                .ToListAsync();

            // Merge both streams on (year, month).
            var allKeys = paymentsByMonth.Select(p => (p.Year, p.Month))
                .Concat(finesByMonth.Select(f => (f.Year, f.Month)))
                .Distinct()
                .OrderByDescending(k => k.Year).ThenByDescending(k => k.Month)
                .ToList();

            var rows = allKeys.Select(k =>
            {
                var p = paymentsByMonth.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month);
                var f = finesByMonth.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month);
                return new ViewModels.RevenueRowViewModel
                {
                    Year = k.Year,
                    Month = k.Month,
                    MembershipRevenue = p?.PaymentRevenue ?? 0m,
                    FineRevenue = f?.FineRevenue ?? 0m
                };
            }).ToList();

            return View(rows);
        }
    }
}