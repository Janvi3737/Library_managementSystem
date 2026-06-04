using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.ClassLibrary.Models;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member")]
    public class MyMembershipController : Controller
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser>
            _userManager;

        public MyMembershipController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;

            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            var member =
                await _context.Members
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == user.Id);

            if (member == null)
            {
                return NotFound();
            }

            var membership =
                await _context.Memberships

                .Where(x => x.MemberId == member.Id)

                .OrderByDescending(x => x.EndDate)

                .FirstOrDefaultAsync();

            return View(membership);
        }
    }
}