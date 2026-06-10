using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TokenManagementController : Controller
    {
        private readonly AppDbContext _context;

        public TokenManagementController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // TOKEN PAYMENTS LIST
        // =========================

        public async Task<IActionResult> Index()
        {
            var payments = await _context.TokenPayments
                .Include(x => x.UserToken)
                .ThenInclude(x => x.User)
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();

            ViewBag.TotalRequests = payments.Count;

            ViewBag.PendingRequests = payments.Count(x =>
                x.PaymentStatus == "Pending");

            ViewBag.ApprovedRequests = payments.Count(x =>
                x.PaymentStatus == "Approved");

            ViewBag.RejectedRequests = payments.Count(x =>
                x.PaymentStatus == "Rejected");

            ViewBag.TotalRevenue = payments
                .Where(x => x.PaymentStatus == "Approved")
                .Sum(x => x.Amount);

            return View(payments);
        }

        // =========================
        // APPROVE TOKEN
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var payment = await _context.TokenPayments
                .Include(x => x.UserToken)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment == null)
                return NotFound();

            // Only pending payments can be approved
            if (payment.PaymentStatus != "Pending")
            {
                TempData["Error"] =
                    $"This payment is already {payment.PaymentStatus.ToLower()}.";

                return RedirectToAction(nameof(Index));
            }

            // Safety check
            if (payment.UserToken == null)
            {
                TempData["Error"] = "User token record not found.";
                return RedirectToAction(nameof(Index));
            }

            // Count total approved token purchases
            int totalPurchasedTokens = await _context.TokenPayments
                .Include(x => x.UserToken)
                .CountAsync(x =>
                    x.UserToken.UserId == payment.UserToken.UserId &&
                    x.PaymentStatus == "Approved");

            // Maximum 3 purchases
            if (totalPurchasedTokens >= 3)
            {
                payment.PaymentStatus = "Rejected";

                _context.Notifications.Add(new Notification
                {
                    MemberId = payment.UserToken.UserId,
                    Message = "❌ Token request rejected because the maximum limit of 3 token purchases has been reached.",
                    Link = "/Token/MyTokens",
                    IsRead = false,
                    CreatedOn = DateTime.Now
                });

                await _context.SaveChangesAsync();

                TempData["Error"] =
                    "User has already reached the maximum token purchase limit.";

                return RedirectToAction(nameof(Index));
            }

            // Approve payment
            payment.PaymentStatus = "Approved";

            payment.UserToken.IsApproved = true;

            // Add 1 token
            payment.UserToken.AvailableTokens += 1;

            if (payment.UserToken.TotalBorrowCount < 0)
            {
                payment.UserToken.TotalBorrowCount = 0;
            }

            // Notification
            _context.Notifications.Add(new Notification
            {
                MemberId = payment.UserToken.UserId,
                Message =
                    $"🎉 Your token payment of ₹{payment.Amount} has been approved. 1 borrow token has been added to your account.",
                Link = "/Token/MyTokens",
                IsRead = false,
                CreatedOn = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Token approved successfully for {payment.UserToken.User?.FullName ?? "User"}.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // REJECT TOKEN
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var payment = await _context.TokenPayments
                .Include(x => x.UserToken)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment == null)
                return NotFound();

            // Only pending payments can be rejected
            if (payment.PaymentStatus != "Pending")
            {
                TempData["Error"] =
                    $"This payment is already {payment.PaymentStatus.ToLower()}.";

                return RedirectToAction(nameof(Index));
            }

            // Mark payment as rejected
            payment.PaymentStatus = "Rejected";

            // Notification
            if (payment.UserToken != null)
            {
                _context.Notifications.Add(new Notification
                {
                    MemberId = payment.UserToken.UserId,
                    Message =
                        $"❌ Your token payment of ₹{payment.Amount} has been rejected. Please verify your payment details and submit a new request.",
                    Link = "/Token/MyTokens",
                    IsRead = false,
                    CreatedOn = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Token request rejected for {payment.UserToken?.User?.FullName ?? "User"}.";

            return RedirectToAction(nameof(Index));
        }

    }
}
