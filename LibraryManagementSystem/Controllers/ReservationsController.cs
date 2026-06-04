using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Reservations  (?status=Waiting|Fulfilled|Cancelled)
        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.Reservations
                .Include(r => r.Book)
                    .ThenInclude(b => b!.Author)
                .Include(r => r.Member)
                .AsQueryable();

            if (Enum.TryParse<ReservationStatus>(status, out var s))
                query = query.Where(r => r.Status == s);

            var list = await query
                .OrderByDescending(r => r.ReservedOn)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.WaitingCount = await _context.Reservations
                .CountAsync(r => r.Status == ReservationStatus.Waiting);

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fulfill(int id)
        {
            var r = await _context.Reservations
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null) return NotFound();

            if (r.Status != ReservationStatus.Waiting)
            {
                TempData["Error"] = "Only waiting reservations can be fulfilled.";
                return RedirectToAction(nameof(Index));
            }

            // Look up the Member.Id (int) from the reservation's ApplicationUserId
            // (string). BorrowRecord.MemberId is an int FK to Members.
            var memberId = await _context.Members
                .Where(m => m.ApplicationUserId == r.MemberId)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync();

            if (memberId == null || r.Book == null)
            {
                TempData["Error"] = "Cannot fulfill — member or book record missing.";
                return RedirectToAction(nameof(Index));
            }

            var quantity = r.Quantity > 0 ? r.Quantity : 1;
            if (r.Book.AvailableCopies < quantity)
            {
                TempData["Error"] = $"Only {r.Book.AvailableCopies} cop{(r.Book.AvailableCopies == 1 ? "y" : "ies")} available; cannot issue {quantity}.";
                return RedirectToAction(nameof(Index));
            }

            // Loan defaults — read from admin-editable settings when present.
            var settings = await _context.LibrarySettings.FirstOrDefaultAsync()
                           ?? new LibrarySettings();

            // Create one BorrowRecord per quantity requested. This is what
            // makes the user's Borrow History page actually show the issued
            // book — without this the reservation just got stamped Completed
            // and the user had no record of being issued anything.
            for (int i = 0; i < quantity; i++)
            {
                _context.BorrowRecords.Add(new BorrowRecord
                {
                    BookId = r.BookId,
                    MemberId = memberId.Value,
                    IssuedOn = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(settings.DefaultLoanDays),
                    FinePerDay = settings.FinePerDay,
                    FineAmount = 0,
                    DaysLate = 0,
                    Status = "Issued"
                });
            }

            r.Book.AvailableCopies -= quantity;
            r.Status = ReservationStatus.Completed;

            // Notify the member their book is ready. The user app's bell icon
            // will pick this up on next page load.
            if (!string.IsNullOrEmpty(r.MemberId))
            {
                _context.Notifications.Add(new Notification
                {
                    MemberId = r.MemberId,
                    Message = $"Your reserved book \"{r.Book.Title}\" (qty {quantity}) has been issued — check Borrow History.",
                    Link = "/Member/BorrowHistory/Index"
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Reservation fulfilled — {quantity} cop{(quantity == 1 ? "y" : "ies")} issued.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var r = await _context.Reservations.FindAsync(id);
            if (r == null) return NotFound();

            r.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reservation cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
