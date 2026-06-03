using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EventReservationController : Controller
    {
        private readonly AppDbContext _context;

        public EventReservationController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // ALL RESERVATIONS
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _context.EventReservations
                .Include(x => x.Event)
                .Include(x => x.Member)
                .OrderByDescending(x => x.ReservedOn)
                .ToListAsync();

            return View(data);
        }

        // =========================
        // APPROVE RESERVATION
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var reservation = await _context.EventReservations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (reservation == null)
                return NotFound();

            reservation.Status = "Approved";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Reservation approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // REJECT RESERVATION
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var reservation = await _context.EventReservations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (reservation == null)
                return NotFound();

            reservation.Status = "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Reservation rejected successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
