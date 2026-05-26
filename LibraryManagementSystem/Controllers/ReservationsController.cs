using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Reservations  (?status=Waiting|Fulfilled|Cancelled)
        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.Reservations
                .Include(r => r.Book)
                    .ThenInclude(b => b!.Author)
                .Include(r => r.Member)
                .AsQueryable();

            if (Enum.TryParse<ReservationStatus>(status, out var s))
                query = query.Where(r => r.Status == s);

            var list = await query
                .OrderByDescending(r => r.ReservedOn)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.WaitingCount = await _context.Reservations
                .CountAsync(r => r.Status == ReservationStatus.Waiting);

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fulfill(int id)
        {
            var r = await _context.Reservations
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null) return NotFound();

            r.Status = ReservationStatus.Completed;

            // Notify the member their book is ready. The user app's bell icon
            // will pick this up on next page load.
            if (!string.IsNullOrEmpty(r.MemberId) && r.Book != null)
            {
                _context.Notifications.Add(new Notification
                {
                    MemberId = r.MemberId,
                    Message = $"Your reserved book \"{r.Book.Title}\" is now available — please pick it up.",
                    Link = "/Member/Reservation/Index"
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Reservation marked fulfilled and member notified.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var r = await _context.Reservations.FindAsync(id);
            if (r == null) return NotFound();

            r.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reservation cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
