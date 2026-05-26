using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
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

        // GET: /Notifications
        public async Task<IActionResult> Index()
        {
            var list = await _context.Notifications
                .Include(n => n.Member)
                .OrderByDescending(n => n.CreatedOn)
                .Take(200)
                .ToListAsync();

            return View(list);
        }

        // POST: /Notifications/Broadcast
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Broadcast(string message, string? link)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Message required.";
                return RedirectToAction(nameof(Index));
            }

            // Target every Member (and User). Skip admins so they don't drown
            // in their own broadcasts.
            var memberRoleUsers = await _userManager.GetUsersInRoleAsync("Member");
            var userRoleUsers = await _userManager.GetUsersInRoleAsync("User");
            var recipients = memberRoleUsers
                .Concat(userRoleUsers)
                .DistinctBy(u => u.Id)
                .ToList();

            foreach (var u in recipients)
            {
                _context.Notifications.Add(new Notification
                {
                    MemberId = u.Id,
                    Message = message.Trim(),
                    Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim()
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Broadcast sent to {recipients.Count} user(s).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var n = await _context.Notifications.FindAsync(id);
            if (n != null)
            {
                _context.Notifications.Remove(n);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
