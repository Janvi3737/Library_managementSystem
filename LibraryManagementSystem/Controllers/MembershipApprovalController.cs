using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MembershipApprovalController : Controller
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser>
            _userManager;

        public MembershipApprovalController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;

            _userManager = userManager;
        }

        // ============================================
        // MEMBERSHIP PAYMENTS LIST
        // ============================================

        public async Task<IActionResult> Index()
        {
            var payments =
                await _context.MembershipPayments

                .Include(x => x.Membership)

                .ThenInclude(x => x.Member)

                .OrderByDescending(x => x.PaymentDate)

                .ToListAsync();

            return View(payments);
        }

        // ============================================
        // APPROVE MEMBERSHIP
        // ============================================

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var payment =
                await _context.MembershipPayments

                .Include(x => x.Membership)

                .ThenInclude(x => x.Member)

                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            // =============================
            // UPDATE PAYMENT STATUS
            // =============================

            payment.PaymentStatus =
                "Approved";

            // =============================
            // ACTIVATE MEMBERSHIP
            // =============================

            payment.Membership.IsActive =
                true;

            // =============================
            // GET USER
            // =============================

            var member =
                payment.Membership.Member;

            var user =
                await _userManager.FindByIdAsync(
                    member.ApplicationUserId);

            if (user != null)
            {
                // REMOVE USER ROLE

                if (await _userManager.IsInRoleAsync(
                    user,
                    "User"))
                {
                    await _userManager
                        .RemoveFromRoleAsync(
                            user,
                            "User");
                }

                // ADD MEMBER ROLE

                if (!await _userManager.IsInRoleAsync(
                    user,
                    "Member"))
                {
                    await _userManager
                        .AddToRoleAsync(
                            user,
                            "Member");
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Membership approved successfully.";

            return RedirectToAction("Index");
        }

        // ============================================
        // REJECT MEMBERSHIP
        // ============================================

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var payment =
                await _context.MembershipPayments

                .Include(x => x.Membership)

                .ThenInclude(x => x.Member)

                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            // =============================
            // UPDATE STATUS
            // =============================

            payment.PaymentStatus =
                "Rejected";

            payment.Membership.IsActive =
                false;

            await _context.SaveChangesAsync();

            TempData["Error"] =
                "Membership rejected.";

            return RedirectToAction("Index");
        }
    }
}