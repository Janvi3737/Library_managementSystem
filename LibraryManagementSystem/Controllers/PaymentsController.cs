using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PaymentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Payments  (optional ?status=Pending|Approved|Rejected)
        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.MembershipPayments
                .Include(p => p.Membership)
                    .ThenInclude(m => m.Member)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.PaymentStatus == status);
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.PendingCount = await _context.MembershipPayments
                .CountAsync(p => p.PaymentStatus == "Pending");

            return View(payments);
        }

        // POST: /Payments/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var payment = await _context.MembershipPayments
                .Include(p => p.Membership)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            payment.PaymentStatus = "Approved";

            // Approval also makes the membership active in case it was rejected
            // earlier and then re-approved.
            if (payment.Membership != null)
                payment.Membership.IsActive = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment approved.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Payments/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var payment = await _context.MembershipPayments
                .Include(p => p.Membership)
                    .ThenInclude(m => m.Member)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            payment.PaymentStatus = "Rejected";

            if (payment.Membership != null)
            {
                payment.Membership.IsActive = false;

                // Demote the user back to "User" so they lose Member-area
                // access. They keep the membership row for audit history.
                var appUserId = payment.Membership.Member?.ApplicationUserId;
                if (!string.IsNullOrEmpty(appUserId))
                {
                    var user = await _userManager.FindByIdAsync(appUserId);
                    if (user != null)
                    {
                        if (await _userManager.IsInRoleAsync(user, "Member"))
                            await _userManager.RemoveFromRoleAsync(user, "Member");

                        if (!await _userManager.IsInRoleAsync(user, "User"))
                            await _userManager.AddToRoleAsync(user, "User");
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment rejected. Membership deactivated and user demoted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
