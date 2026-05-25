using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using LibraryManagementSystem.ClassLibrary.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // OVERDUE BOOKS REPORT
        public async Task<IActionResult> OverdueBooks()
        {
            var overdueBooks = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .Where(b =>
                    b.Status == "Issued" &&
                    b.DueDate < DateTime.Now)
                .OrderBy(b => b.DueDate)
                .ToListAsync();

            return View(overdueBooks);
        }

        // TOP BORROWERS REPORT
        public async Task<IActionResult> TopBorrowers()
        {
            var data = await _context.BorrowRecords
                .Include(b => b.Member)
                .Where(b => b.Member != null)
                .GroupBy(b => new { b.MemberId, b.Member.Name })
                .Select(g => new TopBorrowerViewModel
                {
                    MemberName = g.Key.Name,
                    TotalBooks = g.Count()
                })
                .OrderByDescending(x => x.TotalBooks)
                .Take(10)
                .ToListAsync();

            return View(data);
        }
    }
}