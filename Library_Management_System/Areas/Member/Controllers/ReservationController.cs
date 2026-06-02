using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member")]
    public class ReservationController : Controller
    {
        private readonly AppDbContext _context;

        public ReservationController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // MY BOOK RESERVATIONS
        // =========================

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reservations = await _context.Reservations
                .Include(r => r.Book)
                    .ThenInclude(b => b.Author)
                .Where(r => r.MemberId == userId)
                .OrderByDescending(r => r.ReservedOn)
                .ToListAsync();

            ViewBag.QueuePositions = reservations.ToDictionary(
                r => r.Id,
                r => _context.Reservations.Count(x =>
                    x.BookId == r.BookId &&
                    x.ReservedOn < r.ReservedOn &&
                    x.Status == ReservationStatus.Waiting) + 1
            );

            return View(reservations);
        }

        // =========================
        // RESERVE BOOK PAGE
        // =========================

        public async Task<IActionResult> Create(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                return NotFound();

            return View(book);
        }

        // =========================
        // SAVE BOOK RESERVATION
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReservation(int bookId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var alreadyReserved = await _context.Reservations
                .AnyAsync(r =>
                    r.BookId == bookId &&
                    r.MemberId == userId &&
                    r.Status == ReservationStatus.Waiting);

            if (alreadyReserved)
            {
                TempData["Error"] = "You already reserved this book.";

                return RedirectToAction(nameof(Index));
            }

            var reservation = new Reservation
            {
                BookId = bookId,
                MemberId = userId,
                ReservedOn = DateTime.Now,
                Status = ReservationStatus.Waiting
            };

            _context.Reservations.Add(reservation);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Book reserved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // CANCEL RESERVATION
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.MemberId == userId);

            if (reservation == null)
                return NotFound();

            // A reservation that's already been completed (book issued) or
            // already cancelled isn't a legitimate target for "cancel" — the
            // physical book transaction has already happened.
            if (reservation.Status != ReservationStatus.Waiting)
            {
                TempData["Error"] =
                    "Only waiting reservations can be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            _context.Reservations.Remove(reservation);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Reservation cancelled.";

            return RedirectToAction(nameof(Index));
        }

        // Note: "Approve" (mark Waiting -> Completed and issue a BorrowRecord)
        // is an admin/librarian workflow and lives in the admin app's
        // ReservationsController. Members cannot self-approve their own
        // reservations from this controller — doing so would let them issue
        // themselves books and decrement AvailableCopies without any human
        // review (horizontal privilege escalation).
    }
}
