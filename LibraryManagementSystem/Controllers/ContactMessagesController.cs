using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ContactMessagesController : Controller
    {
        private readonly AppDbContext _context;

        public ContactMessagesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /ContactMessages
        public async Task<IActionResult> Index()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.ReceivedOn)
                .ToListAsync();

            ViewBag.UnreadCount = messages.Count(m => !m.IsRead);
            return View(messages);
        }

        // GET: /ContactMessages/Details/5  (also marks read)
        public async Task<IActionResult> Details(int id)
        {
            var msg = await _context.ContactMessages
                .FirstOrDefaultAsync(m => m.Id == id);

            if (msg == null) return NotFound();

            if (!msg.IsRead)
            {
                msg.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(msg);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRead(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();

            msg.IsRead = !msg.IsRead;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return RedirectToAction(nameof(Index));

            _context.ContactMessages.Remove(msg);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Message deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
