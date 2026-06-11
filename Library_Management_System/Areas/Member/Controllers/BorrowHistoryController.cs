using Library_Management_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member")]
    public class BorrowHistoryController : Controller
    {
        private readonly AppDbContext _context;

        public BorrowHistoryController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            // LOAD ALL BORROW RECORDS

            var borrowRecords = await _context.BorrowRecords
                .Include(x => x.Book)
                    .ThenInclude(x => x.Author)
                .Where(x => x.MemberId == memberId)
                .OrderByDescending(x => x.IssuedOn)
                .ToListAsync();

            // GROUP SAME BOOKS

            var query = borrowRecords
                .GroupBy(x => x.BookId)
                .Select(g => new
                {
                    Borrow = g.First(),
                    BorrowCount = g.Count()
                })
                .AsQueryable();

            // FILTERS

            if (!string.IsNullOrEmpty(status))
            {
                if (status.ToLower() == "active")
                {
                    query = query.Where(x =>
                        x.Borrow.ReturnedOn == null &&
                        x.Borrow.DueDate >= DateTime.Now);
                }
                else if (status.ToLower() == "returned")
                {
                    query = query.Where(x =>
                        x.Borrow.ReturnedOn != null);
                }
                else if (status.ToLower() == "overdue")
                {
                    query = query.Where(x =>
                        x.Borrow.ReturnedOn == null &&
                        x.Borrow.DueDate < DateTime.Now);
                }
            }

            // VIEW MODEL

            var history = query
                .OrderByDescending(x => x.Borrow.IssuedOn)
                .Select(x => new BorrowHistoryViewModel
                {
                    Id = x.Borrow.Id,

                    BookTitle = x.Borrow.Book != null
                        ? x.Borrow.Book.Title
                        : "",

                    Author = x.Borrow.Book != null &&
                             x.Borrow.Book.Author != null
                        ? x.Borrow.Book.Author.Name
                        : "",

                    BorrowDate = x.Borrow.IssuedOn,

                    DueDate = x.Borrow.DueDate,

                    ReturnDate = x.Borrow.ReturnedOn,

                    DaysLate = x.Borrow.DaysLate,

                    FinePerDay = x.Borrow.FinePerDay,

                    FineAmount = x.Borrow.FineAmount,

                    FinePaid = x.Borrow.FinePaid,

                    BorrowCount = x.BorrowCount,

                    Status = x.Borrow.ReturnedOn != null
                        ? "Returned"
                        : x.Borrow.DueDate < DateTime.Now
                            ? "Overdue"
                            : "Active"
                })
                .ToList();

            ViewBag.CurrentStatus = status;

            return View(history);
        }
        // RETURN PAGE

        [HttpGet]
        public async Task<IActionResult> ReturnBook(int id)
        {
            var borrow = await _context.BorrowRecords
                .Include(x => x.Book)
                .ThenInclude(x => x.Author)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (borrow == null)
            {
                return NotFound();
            }

            return View(borrow);
        }

        // RETURN CONFIRM

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBookConfirmed(int id)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var borrow = await _context.BorrowRecords
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.MemberId == memberId);

            if (borrow == null)
            {
                return NotFound();
            }

            if (borrow.ReturnedOn != null)
            {
                TempData["Error"] =
                    "Book already returned.";

                return RedirectToAction(nameof(Index));
            }

            borrow.ReturnedOn = DateTime.Now;

            borrow.Status = "Returned";

            borrow.FinePerDay = 10;

            int lateDays =
                (borrow.ReturnedOn.Value.Date -
                 borrow.DueDate.Date).Days;

            if (lateDays > 0)
            {
                borrow.DaysLate = lateDays;

                borrow.FineAmount =
                    lateDays * borrow.FinePerDay;

                borrow.FinePaid = false;
            }
            else
            {
                borrow.DaysLate = 0;

                borrow.FineAmount = 0;

                borrow.FinePaid = true;
            }

            // ==========================================
            // NON-MEMBER REFUND CALCULATION
            // ==========================================

            if (borrow.IsNonMemberBorrow)
            {
                // Returned on time
                if (borrow.DaysLate == 0)
                {
                    borrow.RefundAmount =
                        borrow.SecurityDeposit;
                }
                else
                {
                    // Late return
                    borrow.RefundAmount =
                        Math.Max(
                            0,
                            borrow.SecurityDeposit - borrow.FineAmount
                        );
                }

                borrow.RefundProcessed = false;
            }

            // INCREASE STOCK

            if (borrow.Book != null)
            {
                borrow.Book.AvailableCopies += 1;
            }

            await _context.SaveChangesAsync();

            // SUCCESS MESSAGE

            if (borrow.IsNonMemberBorrow)
            {
                TempData["Success"] =
                    $"Book returned successfully. Refund Amount: ₹{borrow.RefundAmount}";
            }
            else if (borrow.FineAmount > 0)
            {
                TempData["Success"] =
                    $"Book returned successfully. Fine: ₹{borrow.FineAmount}";
            }
            else
            {
                TempData["Success"] =
                    "Book returned successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
