using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member,User")]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =====================================
        // LIST NOTIFICATIONS
        // =====================================

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" });
            }

            var list = await _context.Notifications
                .Where(n => n.MemberId == user.Id)
                .OrderByDescending(n => n.CreatedOn)
                .Take(100)
                .ToListAsync();

            var unread = list
                .Where(n => !n.IsRead)
                .ToList();

            if (unread.Any())
            {
                foreach (var item in unread)
                {
                    item.IsRead = true;
                }

                await _context.SaveChangesAsync();
            }

            return View(list);
        }

        // =====================================
        // DELETE ONE
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.MemberId == user.Id);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // DELETE ALL
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var user = await _userManager.GetUserAsync(User);

            var notifications = await _context.Notifications
                .Where(x => x.MemberId == user.Id)
                .ToListAsync();

            if (notifications.Any())
            {
                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // UNREAD COUNT
        // =====================================

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Json(0);

            var count = await _context.Notifications
                .CountAsync(x =>
                    x.MemberId == user.Id &&
                    !x.IsRead);

            return Json(count);
        }
    }
}
