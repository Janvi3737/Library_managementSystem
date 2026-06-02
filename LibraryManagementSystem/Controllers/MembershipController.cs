using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.ClassLibrary.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MembershipController : Controller
    {
        private readonly AppDbContext _context;

        public MembershipController(AppDbContext context)
        {
            _context = context;
        }

        // ================= LIST =================
        public async Task<IActionResult> Index()
        {
            var data = await _context.Memberships
                .Include(m => m.Member)
                .OrderByDescending(m => m.StartDate)
                .ToListAsync();

            return View(data);
        }

        // ================= CREATE GET =================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Members = new SelectList(
                await _context.Members.ToListAsync(),
                "Id",
                "Name"
            );

            return View();
        }

        // ================= CREATE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Membership membership)
        {
            var alreadyActive = await _context.Memberships
                .AnyAsync(m =>
                    m.MemberId == membership.MemberId &&
                    m.IsActive &&
                    m.EndDate > DateTime.Now);

            if (alreadyActive)
            {
                TempData["Error"] = "Member already has active membership!";
                return RedirectToAction(nameof(Create));
            }

            // START DATE
            membership.StartDate = DateTime.Now;

            // END DATE
            membership.EndDate = DateTime.Now.AddMonths(membership.DurationMonths);

            membership.IsActive = true;

            // ================= FEES =================
            // Pulls from LibrarySettings so the admin-editable Settings page
            // actually controls fees — the previous hardcoded ladder ignored
            // edits and produced different fees from the user-app Buy flow.
            var settings = await _context.LibrarySettings.FirstOrDefaultAsync()
                           ?? new LibrarySettings();

            membership.Fee = (membership.MembershipType, membership.DurationMonths) switch
            {
                ("Student", 1)  => settings.StudentMonthly,
                ("Student", _)  => settings.StudentAnnual,
                ("Regular", 1)  => settings.RegularMonthly,
                ("Regular", _)  => settings.RegularAnnual,
                ("Premium", 1)  => settings.PremiumMonthly,
                ("Premium", _)  => settings.PremiumAnnual,
                _               => 0m
            };

            _context.Memberships.Add(membership);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Membership added successfully!";

            return RedirectToAction(nameof(Index));
        }

        // ================= DETAILS =================
        public async Task<IActionResult> Details(int id)
        {
            var membership = await _context.Memberships
                .Include(m => m.Member)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (membership == null) return NotFound();

            ViewBag.Payments = await _context.MembershipPayments
                .Where(p => p.MembershipId == id)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(membership);
        }

        // ================= EDIT GET =================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var membership = await _context.Memberships
                .Include(m => m.Member)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (membership == null) return NotFound();

            ViewBag.Members = new SelectList(
                await _context.Members.ToListAsync(),
                "Id",
                "Name",
                membership.MemberId);

            return View(membership);
        }

        // ================= EDIT POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,MemberId,MembershipType,DurationMonths,StartDate,EndDate,IsActive,Fee")]
            Membership membership)
        {
            if (id != membership.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Members = new SelectList(
                    await _context.Members.ToListAsync(),
                    "Id",
                    "Name",
                    membership.MemberId);
                return View(membership);
            }

            try
            {
                _context.Update(membership);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Memberships.Any(m => m.Id == membership.Id))
                    return NotFound();
                throw;
            }

            TempData["Success"] = "Membership updated.";
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE GET =================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var membership = await _context.Memberships
                .Include(m => m.Member)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (membership == null) return NotFound();
            return View(membership);
        }

        // ================= DELETE POST =================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var membership = await _context.Memberships.FindAsync(id);
            if (membership == null)
                return RedirectToAction(nameof(Index));

            // MembershipPayments cascade-delete via the FK config, so any
            // payment rows under this membership go away with it.
            _context.Memberships.Remove(membership);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Membership deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}