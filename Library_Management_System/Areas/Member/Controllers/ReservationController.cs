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

            _context.Reservations.Remove(reservation);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Reservation cancelled.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // APPROVE RESERVATION
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            if (reservation.Status != ReservationStatus.Waiting)
            {
                TempData["Error"] = "Reservation already processed.";

                return RedirectToAction(nameof(Index));
            }

            if (reservation.Book.AvailableCopies <= 0)
            {
                TempData["Error"] = "No copies available.";

                return RedirectToAction(nameof(Index));
            }

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == reservation.MemberId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (memberId == 0)
            {
                TempData["Error"] = "Member not found.";

                return RedirectToAction(nameof(Index));
            }

            var borrow = new BorrowRecord
            {
                BookId = reservation.BookId,
                MemberId = memberId,
                IssuedOn = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14),
                FinePerDay = 5
            };

            _context.BorrowRecords.Add(borrow);

            reservation.Book.AvailableCopies--;

            reservation.Status = ReservationStatus.Completed;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Book issued successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
