using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ClassLibrary.Models;


namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BorrowController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PdfReceiptService _pdfService;
        private readonly ExportService _exportService;

        public BorrowController(AppDbContext context, PdfReceiptService pdfService, ExportService exportService)
        {
            _context = context;
            _pdfService = pdfService;
            _exportService = exportService;
        }
        // ================= INDEX =================

        public async Task<IActionResult> Index()
        {
            var records = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .OrderByDescending(b => b.IssuedOn)
                .Take(50)
                .ToListAsync();

            foreach (var r in records)
            {
                CalculateFine(r); 
            }

            return View(records);
        }

        // ================= ISSUE GET =================
        [HttpGet]
        public async Task<IActionResult> Issue()
        {
            ViewBag.Books = new SelectList(
                await _context.Books.ToListAsync(),
                "Id",
                "Title"
            );

            ViewBag.Members = new SelectList(
                await _context.Members.ToListAsync(),
                "Id",
                "Name"
            );

            // NEW: Borrow Duration Options
            ViewBag.Durations = new List<SelectListItem>
    {
        new SelectListItem { Text = "3 Days", Value = "3" },
        new SelectListItem { Text = "7 Days", Value = "7" },
        new SelectListItem { Text = "15 Days", Value = "15" },
        new SelectListItem { Text = "1 Month", Value = "30" }
    };

            return View();
        }

        // ================= ISSUE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(BorrowRecord Record, int borrowDays)
        {
            var book = await _context.Books.FindAsync(Record.BookId);

            if (book == null)
            {
                TempData["Error"] = "Book not found.";
                return RedirectToAction(nameof(Issue));
            }

            if (book.AvailableCopies <= 0)
            {
                TempData["Error"] = "❌ Book not available.";
                return RedirectToAction(nameof(Issue));
            }

            if (borrowDays <= 0)
            {
                TempData["Error"] = "Invalid borrow duration.";
                return RedirectToAction(nameof(Issue));
            }

            Record.IssuedOn = DateTime.Now;
            Record.DueDate = DateTime.Now.AddDays(borrowDays);
            Record.Status = "Issued";

            Record.FinePerDay = 10;
            Record.FineAmount = 0;
            Record.DaysLate = 0;

            book.AvailableCopies--;

            _context.BorrowRecords.Add(Record);
            _context.Books.Update(book);

            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Book issued successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ================= RETURN GET =================
        [HttpGet]
        public async Task<IActionResult> Return(int id)
        {
            var record = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (record == null)
                return NotFound();

            // FINE CALCULATION
            if (record.ReturnedOn == null && DateTime.Now > record.DueDate)
            {
                CalculateFine(record);
            }

            return View(record);
        }

        // ================= RETURN POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnConfirmed(int id)
        {
            var record = await _context.BorrowRecords
                .Include(b => b.Book)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (record == null)
                return NotFound();

            // RETURN DATE
            record.ReturnedOn = DateTime.Now;

            // FINE CALCULATION
            if (record.ReturnedOn > record.DueDate)
            {
                record.DaysLate =
                    (record.ReturnedOn.Value - record.DueDate).Days;

                record.FineAmount =
                    record.DaysLate * record.FinePerDay;
            }
            else
            {
                record.DaysLate = 0;
                record.FineAmount = 0;
            }

            record.Status = "Returned";

            // INCREASE STOCK
            if (record.Book != null)
            {
                record.Book.AvailableCopies += 1;
            }

            _context.Update(record);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "✅ Book returned successfully!";

            return RedirectToAction(nameof(Index));
        }

        // ================= HISTORY =================
        public async Task<IActionResult> History(int memberId)
        {
            var records = await _context.BorrowRecords
                .AsNoTracking()
                .Include(b => b.Book)
                .Include(b => b.Member)
                .Where(b => b.MemberId == memberId)
                .OrderByDescending(b => b.IssuedOn)
                .ToListAsync();

            foreach (var r in records)
            {
                CalculateFine(r);
            }

            ViewBag.MemberName = records.FirstOrDefault()?.Member?.Name;

            return View(records);
        }

        private void CalculateFine(BorrowRecord record)
        {
            if (record.FinePerDay == 0)
            {
                record.FinePerDay = 10;
            }

            DateTime endDate = record.ReturnedOn ?? DateTime.Now;

            if (endDate > record.DueDate)
            {
                record.DaysLate = (endDate.Date - record.DueDate.Date).Days;
                record.FineAmount = record.DaysLate * record.FinePerDay;
            }
            else
            {
                record.DaysLate = 0;
                record.FineAmount = 0;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMemberMembership(int memberId)
        {
            var membership = await _context.Memberships
                .Where(m => m.MemberId == memberId && m.IsActive && m.EndDate > DateTime.Now)
                .FirstOrDefaultAsync();

            if (membership == null)
                return Json(new { hasMembership = false });

            return Json(new
            {
                hasMembership = true,
                durationMonths = membership.DurationMonths,
                endDate = membership.EndDate.ToString("yyyy-MM-dd")
            });
        }

        public async Task<IActionResult> DownloadReceipt(int id)
        {
            var record = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (record == null)
                return NotFound();

            var pdf = _pdfService.GenerateIssueReceipt(record);

            return File(
                pdf,
                "application/pdf",
                $"Receipt_{record.Id}.pdf"
            );
        }

        public IActionResult ExportExcel()
        {
            var borrows = _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .ToList();

            var file = _exportService.ExportBorrows(borrows);

            return File(file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BorrowRecords.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew(int id)
        {
            var record = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record == null)
                return NotFound();

            if (record.Status == "Returned")
            {
                TempData["Error"] = "Book already returned.";
                return RedirectToAction(nameof(Index));
            }

            if (DateTime.Now.Date > record.DueDate.Date)
            {
                TempData["Error"] = "Cannot renew overdue book.";
                return RedirectToAction(nameof(Index));
            }

            if (record.RenewCount >= 2)
            {
                TempData["Error"] = "Maximum renew limit reached.";
                return RedirectToAction(nameof(Index));
            }

            record.DueDate = record.DueDate.AddDays(7);

            record.RenewCount++;

            _context.Update(record);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Book renewed successfully for 7 days.";

            return RedirectToAction(nameof(Index));
        }
    }
}
