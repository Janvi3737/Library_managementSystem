using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // CURRENT USER ID

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // MEMBER DETAILS

            var member = await _context.Members
                .FirstOrDefaultAsync(x => x.ApplicationUserId == userId);

            if (member == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // BORROW RECORDS

            var borrowRecords = await _context.BorrowRecords
                .Include(x => x.Book)
                    .ThenInclude(x => x.Author)
                .Where(x => x.MemberId == member.Id)
                .OrderByDescending(x => x.IssuedOn)
                .ToListAsync();

            // ACTIVE BOOKS

            var activeBooks = borrowRecords
                .Where(x => x.ReturnedOn == null)
                .ToList();

            // MY BOOKS

            var myBooks = activeBooks.Select(x => new MyBookViewModel
            {
                Id = x.Id,

                BookTitle = x.Book.Title,

                Author = x.Book.Author.Name,

                IssueDate = x.IssuedOn,

                DueDate = x.DueDate,

                IsReturned = x.ReturnedOn != null,

                FineAmount = x.DueDate < DateTime.Now
                    ? (DateTime.Now - x.DueDate).Days *
                      (x.FinePerDay > 0 ? x.FinePerDay : 5)
                    : 0
            }).ToList();

            // RETURNED BOOKS COUNT

            var returnedBooks = borrowRecords
                .Count(x => x.ReturnedOn != null);

            // OVERDUE BOOKS COUNT

            var overdueBooks = activeBooks
                .Count(x => x.DueDate < DateTime.Now);

            // TOTAL FINE

            decimal totalFine = myBooks.Sum(x => x.FineAmount);

            // WISHLIST COUNT

            // WISHLIST COUNT

            var wishlistCount = await _context.Wishlists
                .CountAsync(x => x.MemberId == userId);

            // MEMBERSHIP DETAILS

            DateTime joinedDate = member.JoinedOn;

            DateTime membershipTill = joinedDate.AddYears(1);

            int daysLeft = (membershipTill - DateTime.Now).Days;

            if (daysLeft < 0)
            {
                daysLeft = 0;
            }

            // RECENT ACTIVITIES

            var activities = borrowRecords
                .Take(6)
                .Select(x => new RecentActivityViewModel
                {
                    Activity = x.ReturnedOn != null
                        ? $"Returned \"{x.Book.Title}\""
                        : $"Borrowed \"{x.Book.Title}\"",

                    ActivityDate = x.ReturnedOn ?? x.IssuedOn
                })
                .ToList();

            // VIEWBAG DATA

            ViewBag.MemberName = member.Name;

            ViewBag.MemberSince = joinedDate;

            ViewBag.MembershipTill = membershipTill;

            ViewBag.DaysLeft = daysLeft;

            // DASHBOARD VIEWMODEL

            var model = new MemberDashboardViewModel
            {
                CurrentBorrows = activeBooks.Count,

                ReturnedBooks = returnedBooks,

                Overdue = overdueBooks,

                FineDue = totalFine,

                WishListCount = wishlistCount,

                MyBooks = myBooks,

                RecentActivity = activities
            };

            return View(model);
        }
    }
}
