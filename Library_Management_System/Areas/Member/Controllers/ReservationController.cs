using System.Security.Claims;
using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using Microsoft.Extensions.Configuration;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member,User")]
    public class ReservationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public ReservationController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

            // Membership Check
            var member = await _context.Members
    .FirstOrDefaultAsync(m => m.ApplicationUserId == userId);

            ViewBag.HasMembership = false;

            if (member != null)
            {
                ViewBag.HasMembership = await _context.Memberships
                    .AnyAsync(m =>
                        m.MemberId == member.Id &&
                        m.IsActive &&
                        m.EndDate >= DateTime.Now);
            }

            return View(reservations);
        }

        // =========================
        // RESERVE PAGE
        // =========================

        [HttpGet]
        public async Task<IActionResult> Create(int bookId, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

            // KEEP MEMBERSHIP CHECK AS IT IS
            var member = await _context.Members
    .FirstOrDefaultAsync(m => m.ApplicationUserId == userId);

            bool hasMembership = false;

            if (member != null)
            {
                hasMembership = await _context.Memberships
                    .AnyAsync(m =>
                        m.MemberId == member.Id &&
                        m.IsActive &&
                        m.EndDate >= DateTime.Now);
            }

            ViewBag.HasMembership = hasMembership;
            ViewBag.Quantity = quantity;

            // Non-member charges
            ViewBag.BorrowFee = hasMembership
                ? 0
                : (50 * quantity);

            ViewBag.SecurityDeposit = hasMembership
                ? 0
                : (book.DepositAmount * quantity);

            ViewBag.TotalAmount =
                (decimal)ViewBag.BorrowFee +
                (decimal)ViewBag.SecurityDeposit;

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

            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                return NotFound();

            if (book.AvailableCopies <= 0)
            {
                TempData["Error"] = "Book is not available.";
                return RedirectToAction(nameof(Index));
            }

            // Quantity Validation

            if (quantity < 1)
                quantity = 1;

            if (quantity > book.AvailableCopies)
                quantity = book.AvailableCopies;

            // Duplicate Reservation Check

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

            // Membership Check

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.ApplicationUserId == userId);

            bool hasMembership = false;

            if (member != null)
            {
                hasMembership = await _context.Memberships
                    .AnyAsync(m =>
                        m.MemberId == member.Id &&
                        m.IsActive &&
                        m.EndDate >= DateTime.Now);
            }

            // =========================
            // MEMBER USER
            // =========================

            if (hasMembership)
            {
                var reservation = new Reservation
                {
                    BookId = bookId,
                    MemberId = userId,
                    Quantity = quantity,
                    ReservedOn = DateTime.Now,
                    Status = ReservationStatus.Waiting
                };

                _context.Reservations.Add(reservation);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    $"Book reserved successfully. {quantity} copy(s) reserved.";

                return RedirectToAction(nameof(Index));
            }

            // =========================
            // NON-MEMBER USER
            // =========================

            decimal borrowFee = 50m * quantity;
            decimal securityDeposit = book.DepositAmount * quantity;
            decimal totalPayable = borrowFee + securityDeposit;

            TempData["BookId"] = bookId;
            TempData["Quantity"] = quantity;
            TempData["BorrowFee"] = borrowFee.ToString();
            TempData["SecurityDeposit"] = securityDeposit.ToString();
            TempData["TotalPayable"] = totalPayable.ToString();

            return RedirectToAction("Payment", new
            {
                bookId = bookId,
                quantity = quantity
            });
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


        [HttpGet]
        public async Task<IActionResult> Payment(int bookId, int quantity)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                return NotFound();

            decimal borrowFee = 50m * quantity;
            decimal deposit = book.DepositAmount * quantity;
            decimal totalAmount = borrowFee + deposit;

            var razorpayKey = _configuration["Razorpay:Key"];
            var razorpaySecret = _configuration["Razorpay:Secret"];

            RazorpayClient client = new RazorpayClient(
                razorpayKey,
                razorpaySecret);

            Dictionary<string, object> options = new Dictionary<string, object>();

            options.Add("amount", Convert.ToInt32(totalAmount * 100)); 
            options.Add("currency", "INR");
            options.Add("receipt", $"BOOK_{bookId}_{DateTime.Now.Ticks}");

            Order order = client.Order.Create(options);

            var model = new RazorPayViewModel
            {
                Book = book,
                Quantity = quantity,
                BorrowFee = borrowFee,
                SecurityDeposit = deposit,
                TotalAmount = totalAmount,
                RazorpayKey = razorpayKey,
                RazorpayOrderId = order["id"].ToString()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentSuccess(
    int bookId,
    int quantity,
    string razorpayPaymentId,
    string razorpayOrderId,
    string razorpaySignature)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                

                var reservation = new Reservation
                {
                    BookId = bookId,
                    MemberId = userId,
                    Quantity = quantity,
                    ReservedOn = DateTime.Now,
                    Status = ReservationStatus.Waiting
                };

                _context.Reservations.Add(reservation);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    $"Payment Successful. Payment Id: {razorpayPaymentId}";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Payment Verification Failed.";

                return RedirectToAction(nameof(Index));
            }
        }
    }
}
