using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TokenRefundController : Controller
    {
        private readonly AppDbContext _context;

        public TokenRefundController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // ALL REFUNDS
        // =========================================

        public async Task<IActionResult> Index()
        {
            var refunds = await _context.TokenRefunds
                .Include(x => x.BorrowRecord)
                    .ThenInclude(x => x.Book)
                .Include(x => x.UserToken)
                    .ThenInclude(x => x.User)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            return View(refunds);
        }

        // =========================================
        // PENDING REFUNDS
        // =========================================

        public async Task<IActionResult> Pending()
        {
            var refunds = await _context.TokenRefunds
                .Include(x => x.BorrowRecord)
                    .ThenInclude(x => x.Book)
                .Include(x => x.UserToken)
                    .ThenInclude(x => x.User)
                .Where(x => x.RefundStatus == "Pending")
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            return View("Index", refunds);
        }

        // =========================================
        // APPROVED REFUNDS
        // =========================================

        public async Task<IActionResult> Approved()
        {
            var refunds = await _context.TokenRefunds
                .Include(x => x.BorrowRecord)
                    .ThenInclude(x => x.Book)
                .Include(x => x.UserToken)
                    .ThenInclude(x => x.User)
                .Where(x => x.RefundStatus == "Approved")
                .OrderByDescending(x => x.ProcessedOn)
                .ToListAsync();

            return View("Index", refunds);
        }

        // =========================================
        // REJECTED REFUNDS
        // =========================================

        public async Task<IActionResult> Rejected()
        {
            var refunds = await _context.TokenRefunds
                .Include(x => x.BorrowRecord)
                    .ThenInclude(x => x.Book)
                .Include(x => x.UserToken)
                    .ThenInclude(x => x.User)
                .Where(x => x.RefundStatus == "Rejected")
                .OrderByDescending(x => x.ProcessedOn)
                .ToListAsync();

            return View("Index", refunds);
        }

        // =========================================
        // DETAILS
        // =========================================

        public async Task<IActionResult> Details(int id)
        {
            var refund = await _context.TokenRefunds
                .Include(x => x.BorrowRecord)
                    .ThenInclude(x => x.Book)
                .Include(x => x.UserToken)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (refund == null)
                return NotFound();

            return View(refund);
        }

        // =========================================
        // APPROVE REFUND
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int id,
            string condition)
        {
            var refund = await _context.TokenRefunds
                .Include(x => x.BorrowRecord)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (refund == null)
                return NotFound();

            if (refund.RefundStatus != "Pending")
            {
                TempData["Error"] =
                    "Refund already processed.";

                return RedirectToAction(nameof(Index));
            }

            decimal refundAmount = 0;

            switch (condition)
            {
                case "Excellent":
                    refundAmount = 200;
                    break;

                case "Good":
                    refundAmount = 150;
                    break;

                case "Damaged":
                    refundAmount = 50;
                    break;

                case "Lost":
                    refundAmount = 0;
                    break;
            }

            refundAmount -= refund.FineAmount;

            if (refundAmount < 0)
                refundAmount = 0;

            refund.BookCondition = condition;
            refund.RefundAmount = refundAmount;
            refund.RefundStatus = "Approved";
            refund.ProcessedOn = DateTime.Now;

            if (refund.BorrowRecord != null)
            {
                refund.BorrowRecord.BookCondition =
                    condition;

                refund.BorrowRecord.RefundAmount =
                    refundAmount;

                refund.BorrowRecord.RefundProcessed =
                    true;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Refund approved successfully. Refund Amount: ₹{refundAmount}";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // REJECT REFUND
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? remarks)
        {
            var refund = await _context.TokenRefunds
                .Include(x => x.BorrowRecord)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (refund == null)
                return NotFound();

            if (refund.RefundStatus != "Pending")
            {
                TempData["Error"] =
                    "Refund already processed.";

                return RedirectToAction(nameof(Index));
            }

            refund.RefundStatus = "Rejected";
            refund.ProcessedOn = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                refund.Remarks = remarks;
            }

            if (refund.BorrowRecord != null)
            {
                refund.BorrowRecord.RefundProcessed =
                    true;

                refund.BorrowRecord.RefundAmount = 0;

                refund.BorrowRecord.BookCondition =
                    "Rejected";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Refund request rejected.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // DELETE REFUND RECORD
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var refund = await _context.TokenRefunds
                .FirstOrDefaultAsync(x => x.Id == id);

            if (refund == null)
                return NotFound();

            _context.TokenRefunds.Remove(refund);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Refund record deleted.";

            return RedirectToAction(nameof(Index));
        }
    }
}
