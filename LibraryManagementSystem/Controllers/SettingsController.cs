using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.LibrarySettings.FirstOrDefaultAsync()
                           ?? new LibrarySettings();

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            [Bind("Id,DefaultLoanDays,MaxRenewals,FinePerDay,StudentMonthly,StudentAnnual,RegularMonthly,RegularAnnual,PremiumMonthly,PremiumAnnual,LibraryName")]
            LibrarySettings model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _context.LibrarySettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                _context.LibrarySettings.Add(model);
            }
            else
            {
                existing.DefaultLoanDays = model.DefaultLoanDays;
                existing.MaxRenewals = model.MaxRenewals;
                existing.FinePerDay = model.FinePerDay;
                existing.StudentMonthly = model.StudentMonthly;
                existing.StudentAnnual = model.StudentAnnual;
                existing.RegularMonthly = model.RegularMonthly;
                existing.RegularAnnual = model.RegularAnnual;
                existing.PremiumMonthly = model.PremiumMonthly;
                existing.PremiumAnnual = model.PremiumAnnual;
                existing.LibraryName = model.LibraryName;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Settings saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
