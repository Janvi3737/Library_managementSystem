using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditController : Controller
    {
        private readonly AppDbContext _context;

        public AuditController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? user, string? controller)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(user))
                query = query.Where(a => a.UserEmail != null && a.UserEmail.Contains(user));
            if (!string.IsNullOrWhiteSpace(controller))
                query = query.Where(a => a.Controller == controller);

            var rows = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(500)
                .ToListAsync();

            ViewBag.UserFilter = user;
            ViewBag.ControllerFilter = controller;

            return View(rows);
        }
    }
}
