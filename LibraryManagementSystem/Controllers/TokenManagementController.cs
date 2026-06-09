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

            if (payment.PaymentStatus == "Approved")
            {
                TempData["Error"] = "This payment is already approved.";
                return RedirectToAction(nameof(Index));
            }

            if (payment.PaymentStatus == "Rejected")
            {
                TempData["Error"] = "Rejected payments cannot be approved.";
                return RedirectToAction(nameof(Index));
            }

            payment.PaymentStatus = "Approved";

            if (payment.UserToken != null)
            {
                payment.UserToken.IsApproved = true;

                payment.UserToken.AvailableTokens = 1;

                if (payment.UserToken.TotalBorrowCount < 0)
                {
                    payment.UserToken.TotalBorrowCount = 0;
                }

                // Notification
                _context.Notifications.Add(new Notification
                {
                    MemberId = payment.UserToken.UserId,
                    Message = "🎉 Your token payment has been approved. 1 borrow token has been added to your account.",
                    Link = "/Member/Token/Index",
                    IsRead = false,
                    CreatedOn = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Token approved successfully.";

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

            if (payment.PaymentStatus == "Rejected")
            {
                TempData["Error"] = "This payment is already rejected.";
                return RedirectToAction(nameof(Index));
            }

            if (payment.PaymentStatus == "Approved")
            {
                TempData["Error"] = "Approved payments cannot be rejected.";
                return RedirectToAction(nameof(Index));
            }

            payment.PaymentStatus = "Rejected";

            if (payment.UserToken != null)
            {
                _context.Notifications.Add(new Notification
                {
                    MemberId = payment.UserToken.UserId,
                    Message = "❌ Your token payment has been rejected. Please check your payment screenshot and try again.",
                    Link = "/Member/Token/Index",
                    IsRead = false,
                    CreatedOn = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Error"] = "Token payment rejected.";

            return RedirectToAction(nameof(Index));
        }


    }
}
