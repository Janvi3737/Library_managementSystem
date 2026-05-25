using Library_Management_System.Models;
using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;

namespace Library_Management_System.Controllers
{
    public class HomeController : Controller
    {

        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext context,UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomePageViewModel();

            // CONTINUE READING — the user's currently-borrowed books (not yet
            // returned). Falls back to newest books for guests / users with no
            // active borrows so the section isn't empty.
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser != null)
            {
                var memberId = await _context.Members
                    .Where(m => m.ApplicationUserId == currentUser.Id)
                    .Select(m => m.Id)
                    .FirstOrDefaultAsync();

                if (memberId != 0)
                {
                    model.ContinueReadingBooks = await _context.BorrowRecords
                        .Where(br => br.MemberId == memberId && br.ReturnedOn == null && br.Book != null)
                        .Include(br => br.Book)
                            .ThenInclude(b => b.Author)
                        .OrderByDescending(br => br.IssuedOn)
                        .Take(8)
                        .Select(br => br.Book!)
                        .ToListAsync();
                }
            }

            if (model.ContinueReadingBooks == null || model.ContinueReadingBooks.Count == 0)
            {
                model.ContinueReadingBooks = await _context.Books
                    .Include(x => x.Author)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(8)
                    .ToListAsync();
            }

            // POPULAR CATEGORIES
            model.PopularCategories = await _context.Categories
                .Include(c => c.Books)
                .Select(x => new CategoryWithCountViewModel
                {
                    CategoryName = x.Name,
                    BookCount = x.Books.Count(),
                    IconClass =
                        x.Name.Contains("Technology") ? "💻" :
                        x.Name.Contains("Science") ? "🧪" :
                        x.Name.Contains("Business") ? "💼" :
                        x.Name.Contains("History") ? "🏛️" :
                        x.Name.Contains("Programming") ? "🧑‍💻" :
                        "📚"
                })
                .OrderByDescending(x => x.BookCount)
                .Take(6)
                .ToListAsync();

            // NEW ARRIVALS
            var last30Days = DateTime.UtcNow.AddDays(-30);

            model.NewArrivals = await _context.Books
                .Include(x => x.Author)
                .Where(x => x.CreatedAt >= last30Days)
                .OrderByDescending(x => x.CreatedAt)
                .Take(12)
                .ToListAsync();

            // UPCOMING EVENTS (FIXED)
            model.UpcomingEvents = await _context.Events
                .OrderBy(e => e.Date)
                .Take(4)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Location = e.Location,
                    Date = e.Date
                })
                .ToListAsync();
            var user = await _userManager.GetUserAsync(User);

            ViewBag.UserName = user?.UserName;

            return View(model);
        }

        [ResponseCache(Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]

        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                Activity.Current?.Id ??
                HttpContext.TraceIdentifier
            });
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Hours()
        {
            return View();
        }

        public IActionResult Rules()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
