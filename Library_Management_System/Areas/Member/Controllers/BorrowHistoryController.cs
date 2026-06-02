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

        public async Task<IActionResult> Index(string status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // MEMBER ID

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            // QUERY

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
                    query = query.Where(x => x.ReturnedOn == null);
                }
                else if (status == "returned")
                {
                    query = query.Where(x => x.ReturnedOn != null);
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

                    FineAmount = x.ReturnedOn == null &&
                                 x.DueDate < DateTime.Now
                        ? (DateTime.Now - x.DueDate).Days *
                          (x.FinePerDay > 0 ? x.FinePerDay : 5)
                        : 0,

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBookConfirmed(int id)
        {
            // Confirm the borrow record belongs to the current user — without
            // this, any logged-in member could craft a POST and return
            // somebody else's book (horizontal privilege escalation).
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var borrow = await _context.BorrowRecords
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id && x.MemberId == memberId);

            if (borrow == null)
            {
                return NotFound();
            }

            // Already returned check

            if (borrow.ReturnedOn != null)
            {
                TempData["Error"] = "Book already returned.";

                return RedirectToAction(nameof(Index));
            }

            // Return process

            borrow.ReturnedOn = DateTime.Now;

            // Increase available copies

            borrow.Book.AvailableCopies += 1;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Book returned successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
