using LibraryManagementSystem.ClassLibrary.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: /Users  (optional ?role=Member|User|Admin&search=...)
        public async Task<IActionResult> Index(string? role, string? search)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(s)) ||
                    (u.FullName != null && u.FullName.Contains(s)));
            }

            var users = await query.OrderBy(u => u.Email).ToListAsync();

            // Hydrate roles per-user. Identity stores role membership in a
            // separate table — there's no FK navigation we can Include.
            var rows = new List<UserRowViewModel>(users.Count);
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                if (!string.IsNullOrEmpty(role) && !roles.Contains(role))
                    continue;

                rows.Add(new UserRowViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    EmailConfirmed = u.EmailConfirmed,
                    LockoutEnd = u.LockoutEnd,
                    Roles = roles.ToList()
                });
            }

            ViewBag.CurrentRole = role;
            ViewBag.Search = search;

            return View(rows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToMember(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "User"))
                await _userManager.RemoveFromRoleAsync(user, "User");

            if (!await _userManager.IsInRoleAsync(user, "Member"))
                await _userManager.AddToRoleAsync(user, "Member");

            TempData["Success"] = $"{user.Email} promoted to Member.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteToUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Member"))
                await _userManager.RemoveFromRoleAsync(user, "Member");

            if (!await _userManager.IsInRoleAsync(user, "User"))
                await _userManager.AddToRoleAsync(user, "User");

            TempData["Success"] = $"{user.Email} demoted to User.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"{user.Email} email marked confirmed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            TempData["Success"] = $"{user.Email} unlocked.";
            return RedirectToAction(nameof(Index));
        }
    }
}
