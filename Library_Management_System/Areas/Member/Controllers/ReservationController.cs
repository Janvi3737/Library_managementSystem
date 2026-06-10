using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member,User")]
    public class ReservationController : Controller
    {
        private readonly AppDbContext _context;

        public ReservationController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // MY RESERVATIONS
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
        // RESERVE PAGE
        // =========================

        [HttpGet]
        public async Task<IActionResult> Create(int bookId, int quantity = 1)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                return NotFound();

            if (quantity < 1)
                quantity = 1;

            if (quantity > book.AvailableCopies)
                quantity = book.AvailableCopies;

            ViewBag.Quantity = quantity;

            return View(book);
        }

        // =========================
        // SAVE RESERVATION
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReservation(
    int bookId,
    int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var book = await _context.Books.FindAsync(bookId);

            if (book == null)
                return NotFound();

            // =========================
            // GET USER TOKEN
            // =========================

            var token = await _context.UserTokens
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (token == null)
            {
                TempData["Error"] =
                    "You don't have any token. Please purchase a token first.";

                return RedirectToAction(
                    "BuyToken",
                    "Token",
                    new { area = "", bookId = bookId });
            }

            // =========================
            // TOKEN LIMIT CHECK
            // =========================

            if (token.TotalBorrowCount >= 3)
            {
                TempData["Error"] =
                    "You have already used all 3 tokens. Please purchase membership.";

                return RedirectToAction(
                    "Index",
                    "Membership",
                    new { area = "" });
            }

            // =========================
            // AVAILABLE TOKEN CHECK
            // =========================

            if (token.AvailableTokens <= 0)
            {
                TempData["Error"] =
                    "No available tokens found. Please purchase another token.";

                return RedirectToAction(
                    "BuyToken",
                    "Token",
                    new { area = "", bookId = bookId });
            }

            // =========================
            // QUANTITY VALIDATION
            // =========================

            if (quantity < 1)
                quantity = 1;

            if (quantity > book.AvailableCopies)
                quantity = book.AvailableCopies;

            // =========================
            // DUPLICATE CHECK
            // =========================

            bool alreadyReserved = await _context.Reservations
                .AnyAsync(r =>
                    r.BookId == bookId &&
                    r.MemberId == userId &&
                    r.Status == ReservationStatus.Waiting);

            if (alreadyReserved)
            {
                TempData["Error"] =
                    "You have already reserved this book.";

                return RedirectToAction(nameof(Index));
            }

            // =========================
            // CREATE RESERVATION
            // =========================

            var reservation = new Reservation
            {
                BookId = bookId,
                MemberId = userId,
                Quantity = quantity,
                ReservedOn = DateTime.Now,
                Status = ReservationStatus.Waiting
            };

            _context.Reservations.Add(reservation);

            // =========================
            // CONSUME TOKEN
            // =========================

            token.AvailableTokens -= 1;
            token.TotalBorrowCount += 1;

            _context.UserTokens.Update(token);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Book reserved successfully. Remaining Tokens: {token.AvailableTokens}";

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

            if (reservation.Status != ReservationStatus.Waiting)
            {
                TempData["Error"] =
                    "Only waiting reservations can be cancelled.";

                return RedirectToAction(nameof(Index));
            }

            _context.Reservations.Remove(reservation);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Reservation cancelled successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
