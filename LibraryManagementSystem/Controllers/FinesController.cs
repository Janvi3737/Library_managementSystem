using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FinesController : Controller
    {
        private readonly AppDbContext _context;

        public FinesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Fines  (?status=outstanding|paid|all)
        public async Task<IActionResult> Index(string? status = "outstanding")
        {
            var query = _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .Where(b => b.FineAmount > 0);

            if (status == "outstanding")
                query = query.Where(b => !b.FinePaid);
            else if (status == "paid")
                query = query.Where(b => b.FinePaid);

            var rows = await query
                .OrderByDescending(b => b.DueDate)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.OutstandingTotal = await _context.BorrowRecords
                .Where(b => b.FineAmount > 0 && !b.FinePaid)
                .SumAsync(b => b.FineAmount);

            return View(rows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var record = await _context.BorrowRecords.FindAsync(id);
            if (record == null) return NotFound();

            record.FinePaid = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Fine of ₹{record.FineAmount:0.00} marked paid.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Waive(int id)
        {
            var record = await _context.BorrowRecords.FindAsync(id);
            if (record == null) return NotFound();

            // Waiving zeroes the fine AND marks it settled so it doesn't show
            // up in "outstanding" again if days-late recompute ever runs.
            record.FineAmount = 0m;
            record.FinePaid = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Fine waived.";
            return RedirectToAction(nameof(Index));
        }
    }
}
