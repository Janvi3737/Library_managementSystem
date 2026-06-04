// using Library_Management_System.ViewModels;

// namespace Library_Management_System.Areas.Member.Controllers

//         public BorrowHistoryController(AppDbContext context)

//         public async Task<IActionResult> Index(string status)

//             if (string.IsNullOrEmpty(userId))

//             // MEMBER ID

//             var memberId = await _context.Members

//             // QUERY

//             var query = _context.BorrowRecords

//             // FILTERS

//             if (!string.IsNullOrEmpty(status))

//             // DATA

//             var history = await query

//                     BookTitle = x.Book.Title,

//                     Author = x.Book.Author.Name,

//                     BorrowDate = x.IssuedOn,

//                     DueDate = x.DueDate,

//                     ReturnDate = x.ReturnedOn,

//                     FineAmount = x.ReturnedOn == null &&

//                     Status = x.ReturnedOn != null

//             ViewBag.CurrentStatus = status;

//             return View(history);

//             if (borrow == null)

//             return View(borrow);

//         // [HttpPost]

//         //     var borrow = await _context.BorrowRecords

//         //     if (borrow == null)

//         //     // Already returned check

//         //     if (borrow.ReturnedOn != null)

//         //         return RedirectToAction(nameof(Index));

//         //     // Return process

//         //     borrow.ReturnedOn = DateTime.Now;

//         //     // Increase available copies

//         //     borrow.Book.AvailableCopies += 1;

//         //     await _context.SaveChangesAsync();

//         //     TempData["Success"] = "Book returned successfully.";

//         //     return RedirectToAction(nameof(Index));

//         [HttpPost]

//     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//     var memberId = await _context.Members

//     // Borrow Record

//     var borrow = await _context.BorrowRecords

//     if (borrow == null)

//     // Already Returned

//     if (borrow.ReturnedOn != null)

//         return RedirectToAction(nameof(Index));

//     // RETURN DATE

//     borrow.ReturnedOn = DateTime.Now;

//     // STATUS

//     borrow.Status = "Returned";

//     // FINE PER DAY

//     borrow.FinePerDay = 10;

//     // CALCULATE LATE DAYS

//     int lateDays =

//     if (lateDays > 0)

//         borrow.FineAmount =

//         borrow.FinePaid = false;

//         borrow.FineAmount = 0;

//         borrow.FinePaid = true;

//     // Increase Book Stock

//     if (borrow.Book != null)

//     await _context.SaveChangesAsync();

//     // SUCCESS MESSAGE

//     if (borrow.FineAmount > 0)

//     return RedirectToAction(nameof(Index));

// }

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

        // HISTORY

        public async Task<IActionResult> Index(string status)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var query = _context.BorrowRecords
                .Include(x => x.Book)
                .ThenInclude(x => x.Author)
                .Where(x => x.MemberId == memberId)
                .AsQueryable();

            // FILTERS

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                {
                    query = query.Where(x =>
                        x.ReturnedOn == null);
                }
                else if (status == "returned")
                {
                    query = query.Where(x =>
                        x.ReturnedOn != null);
                }
                else if (status == "overdue")
                {
                    query = query.Where(x =>
                        x.ReturnedOn == null &&
                        x.DueDate < DateTime.Now);
                }
            }

            // DATA

            var history = await query
                .OrderByDescending(x => x.IssuedOn)
                .Select(x => new BorrowHistoryViewModel
                {
                    Id = x.Id,

                    BookTitle = x.Book.Title,

                    Author = x.Book.Author.Name,

                    BorrowDate = x.IssuedOn,

                    DueDate = x.DueDate,

                    ReturnDate = x.ReturnedOn,

                    DaysLate = x.DaysLate,

                    FinePerDay = x.FinePerDay,

                    FineAmount = x.FineAmount,

                    FinePaid = x.FinePaid,

                    Status = x.ReturnedOn != null
                        ? "Returned"
                        : x.DueDate < DateTime.Now
                            ? "Overdue"
                            : "Active"
                })
                .ToListAsync();

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

            // Already Returned

            if (borrow.ReturnedOn != null)
            {
                TempData["Error"] =
                    "Book already returned.";

                return RedirectToAction(nameof(Index));
            }

            // RETURN DATE

            borrow.ReturnedOn = DateTime.Now;

            // STATUS

            borrow.Status = "Returned";

            // FINE PER DAY

            borrow.FinePerDay = 10;

            // CALCULATE LATE DAYS

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

            // Increase Stock

            if (borrow.Book != null)
            {
                borrow.Book.AvailableCopies += 1;
            }

            await _context.SaveChangesAsync();

            if (borrow.FineAmount > 0)
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
