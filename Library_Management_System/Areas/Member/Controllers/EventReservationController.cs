using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member")]
    public class EventReservationController : Controller
    {
        private readonly AppDbContext _context;

        public EventReservationController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // EVENT DETAILS PAGE (CREATE VIEW)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create(int eventId)
        {
            var eventData = await _context.Events
                .FirstOrDefaultAsync(x => x.Id == eventId);

            if (eventData == null)
                return NotFound();

            return View(eventData);
        }

        // =========================
        // RESERVE EVENT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReservation(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (memberId == 0)
                return NotFound();

            var eventExists = await _context.Events.AnyAsync(x => x.Id == eventId);
            if (!eventExists)
                return NotFound();

            var already = await _context.EventReservations
                .AnyAsync(x => x.EventId == eventId && x.MemberId == memberId);

            if (!already)
            {
                var reservation = new EventReservation
                {
                    EventId = eventId,
                    MemberId = memberId,
                    ReservedOn = DateTime.Now,
                    Status = "Reserved"
                };

                _context.EventReservations.Add(reservation);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Seat reserved successfully.";

            return RedirectToAction(nameof(MyReservations));
        }

        // =========================
        // MY RESERVATIONS
        // =========================
        [HttpGet]
        public async Task<IActionResult> MyReservations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var data = await _context.EventReservations
                .Include(x => x.Event)
                .Where(x => x.MemberId == memberId)
                .OrderByDescending(x => x.ReservedOn)
                .ToListAsync();

            return View(data);
        }

        // =========================
        // CANCEL RESERVATION
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var reservation = await _context.EventReservations
                .FirstOrDefaultAsync(x => x.Id == id && x.MemberId == memberId);

            if (reservation == null)
                return NotFound();

            _context.EventReservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyReservations));
        }
    }
}
