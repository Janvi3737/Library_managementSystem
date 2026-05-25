using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LibraryManagementSystem.ClassLibrary.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MembersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ExportService _exportService;

        public MembersController(AppDbContext context, ExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }
        public async Task<IActionResult> Index()
        {
            var members = await _context.Members.ToListAsync();
            return View(members);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var member = await _context.Members
                .Include(m=>m.BorrowRecords)
                .ThenInclude(b=>b.Book)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null) return NotFound();

            return View(member);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Member member)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                member.ApplicationUserId = userId;

                _context.Members.Add(member);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(member);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var member = await _context.Members.FindAsync(id);
            if (member == null) return NotFound();

            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Name,Email,Phone,JoinedOn")] Member member)
        {
            // Whitelist the editable fields. Without [Bind], a crafted POST
            // could set ApplicationUserId (re-pointing the member at another
            // user account). The line below still hardens that explicitly.
            if (id != member.Id) return NotFound();

            var existingMember = await _context.Members.AsNoTracking()
                                    .FirstOrDefaultAsync(m => m.Id == id);

            if (existingMember == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    member.ApplicationUserId = existingMember.ApplicationUserId;

                    _context.Update(member);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MemberExists(member.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(member);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var member = await _context.Members
            .Include(m => m.BorrowRecords)
            .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null) return NotFound();

            return View(member);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _context.Members
                .Include(m => m.BorrowRecords)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
                return RedirectToAction(nameof(Index));

            // BorrowRecord.Member is OnDelete(Restrict) — deleting a member
            // with any history would throw an FK violation 500. Block here
            // with a clear message instead.
            if (member.BorrowRecords != null && member.BorrowRecords.Any())
            {
                TempData["Error"] =
                    "Cannot delete this member — they have borrow history. " +
                    "Reassign or archive borrow records first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Members.Remove(member);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Member deleted.";
            return RedirectToAction(nameof(Index));
        }


        private bool MemberExists(int id)
        {
            return _context.Members.Any(e => e.Id == id);
        }
        public IActionResult ExportExcel()
        {
            var members = _context.Members.ToList();

            var file = _exportService.ExportMembers(members);

            return File(file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Members.xlsx");
        }

    }
}
