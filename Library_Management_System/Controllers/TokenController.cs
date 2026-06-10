using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    [Authorize(Roles = "User,Member")]
    public class TokenController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================
        // BUY TOKEN PAGE
        // =========================

        [HttpGet]
        public async Task<IActionResult> BuyToken(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Get selected book
            var book = await _context.Books
                .FirstOrDefaultAsync(x => x.Id == bookId);

            if (book == null)
                return NotFound();

            // Check active membership
            var member = await _context.Members
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            bool hasMembership = false;

            if (member != null)
            {
                hasMembership = await _context.Memberships
                    .AnyAsync(x =>
                        x.MemberId == member.Id &&
                        x.IsActive &&
                        x.EndDate >= DateTime.UtcNow);
            }

            if (hasMembership)
            {
                TempData["Info"] =
                    "You already have an active membership. Tokens are not required.";

                return RedirectToAction("Details", "Catalog", new { id = bookId });
            }

            // Create UserToken record if not exists
            var userToken = await _context.UserTokens
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (userToken == null)
            {
                userToken = new UserToken
                {
                    UserId = user.Id,
                    AvailableTokens = 0,
                    TotalBorrowCount = 0,
                    DepositAmount = 0,
                    PurchaseDate = DateTime.Now,
                    IsApproved = false
                };

                _context.UserTokens.Add(userToken);
                await _context.SaveChangesAsync();
            }

            // Total Purchased Tokens
            int totalPurchasedTokens = await _context.TokenPayments
                .Include(x => x.UserToken)
                .CountAsync(x =>
                    x.UserToken.UserId == user.Id &&
                    x.PaymentStatus == "Approved");

            // Maximum 3 purchases allowed
            if (totalPurchasedTokens >= 3)
            {
                TempData["Error"] =
                    "You have reached the maximum limit of 3 token purchases. Please purchase a membership.";

                return RedirectToAction("Index", "Membership");
            }

            // Check pending request
            bool hasPendingRequest = await _context.TokenPayments
                .Include(x => x.UserToken)
                .AnyAsync(x =>
                    x.UserToken.UserId == user.Id &&
                    x.PaymentStatus == "Pending");

            if (hasPendingRequest)
            {
                TempData["Error"] =
                    "You already have a pending token request awaiting admin approval.";

                return RedirectToAction(nameof(MyTokens));
            }

            // Statistics
            int availableTokens = userToken.AvailableTokens;

            int usedTokens = userToken.TotalBorrowCount;

            int remainingPurchases = 3 - totalPurchasedTokens;

            ViewBag.BookId = book.Id;
            ViewBag.BookTitle = book.Title;
            ViewBag.DepositAmount = book.DepositAmount;

            ViewBag.AvailableTokens = availableTokens;
            ViewBag.UsedTokens = usedTokens;
            ViewBag.TotalPurchasedTokens = totalPurchasedTokens;
            ViewBag.RemainingPurchases = remainingPurchases;

            ViewBag.IsLimitReached = totalPurchasedTokens >= 3;

            return View();
        }

        // =========================
        // CHECKOUT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int bookId)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(x => x.Id == bookId);

            if (book == null)
                return NotFound();

            TempData["BookId"] = book.Id.ToString();
            TempData["Amount"] = book.DepositAmount.ToString();
            TempData["BookTitle"] = book.Title;

            return RedirectToAction(nameof(Payment));
        }

        // =========================
        // PAYMENT PAGE
        // =========================

        [HttpGet]
        public IActionResult Payment()
        {
            if (TempData["BookId"] == null ||
                TempData["Amount"] == null)
            {
                TempData["Error"] = "Invalid payment request.";
                return RedirectToAction("Index", "Catalog");
            }

            ViewBag.BookId = TempData["BookId"]?.ToString();
            ViewBag.Amount = TempData["Amount"]?.ToString();
            ViewBag.BookTitle = TempData["BookTitle"]?.ToString();

            TempData.Keep();

            return View();
        }

        // =========================
        // PAYMENT SUCCESS
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentSuccess(
    int bookId,
    IFormFile screenshot,
    string paymentMethod,
    string transactionId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var book = await _context.Books
                .FirstOrDefaultAsync(x => x.Id == bookId);

            if (book == null)
            {
                TempData["Error"] = "Book not found.";
                return RedirectToAction(nameof(MyTokens));
            }

            var userToken = await _context.UserTokens
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (userToken == null)
            {
                userToken = new UserToken
                {
                    UserId = user.Id,
                    AvailableTokens = 0,
                    TotalBorrowCount = 0,
                    DepositAmount = 0,
                    PurchaseDate = DateTime.Now,
                    IsApproved = false
                };

                _context.UserTokens.Add(userToken);
                await _context.SaveChangesAsync();
            }

            // Max 3 token purchases
            int totalPurchasedTokens = await _context.TokenPayments
                .Include(x => x.UserToken)
                .CountAsync(x =>
                    x.UserToken.UserId == user.Id &&
                    x.PaymentStatus == "Approved");

            if (totalPurchasedTokens >= 3)
            {
                TempData["Error"] =
                    "You have already purchased 3 tokens. Please purchase a membership.";

                return RedirectToAction("Index", "Membership");
            }

            // Pending request check
            bool hasPendingRequest = await _context.TokenPayments
                .Include(x => x.UserToken)
                .AnyAsync(x =>
                    x.UserToken.UserId == user.Id &&
                    x.PaymentStatus == "Pending");

            if (hasPendingRequest)
            {
                TempData["Error"] =
                    "You already have a pending token request awaiting approval.";

                return RedirectToAction(nameof(MyTokens));
            }

            // Upload Screenshot
            string? screenshotPath = null;

            if (screenshot != null && screenshot.Length > 0)
            {
                string folder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/tokenproof");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName =
                    Guid.NewGuid() + Path.GetExtension(screenshot.FileName);

                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await screenshot.CopyToAsync(stream);
                }

                screenshotPath = "/tokenproof/" + fileName;
            }

            // Create Payment Request
            var payment = new TokenPayment
            {
                UserTokenId = userToken.Id,
                Amount = book.DepositAmount,
                PaymentMethod = paymentMethod,
                TransactionId = string.IsNullOrWhiteSpace(transactionId)
                    ? Guid.NewGuid().ToString()
                    : transactionId,
                ScreenshotPath = screenshotPath,
                PaymentStatus = "Pending",
                PaymentDate = DateTime.Now
            };

            _context.TokenPayments.Add(payment);

            // Notification
            _context.Notifications.Add(new Notification
            {
                MemberId = user.Id,
                Message =
                    $"⏳ Token request submitted for '{book.Title}' and is awaiting admin approval.",
                Link = "/Member/Notifications/Index",
                IsRead = false,
                CreatedOn = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Token purchase request submitted successfully. Deposit Amount ₹{book.DepositAmount}";

            return RedirectToAction(nameof(MyTokens));
        }

        // =========================
        // MY TOKEN PAGE (FULL DETAILS)
        // =========================

        [HttpGet]
        public async Task<IActionResult> MyTokens()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var userToken = await _context.UserTokens
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (userToken == null)
            {
                userToken = new UserToken
                {
                    UserId = user.Id,
                    AvailableTokens = 0,
                    TotalBorrowCount = 0
                };
            }

            int totalPurchasedTokens = await _context.TokenPayments
                .Include(x => x.UserToken)
                .CountAsync(x =>
                    x.UserToken.UserId == user.Id &&
                    x.PaymentStatus == "Approved");

            int availableTokens = userToken.AvailableTokens;

            int usedTokens = userToken.TotalBorrowCount;

            int remainingPurchases = Math.Max(0, 3 - totalPurchasedTokens);

            bool limitReached = totalPurchasedTokens >= 3;

            var tokenHistory = await _context.TokenPayments
                .Include(x => x.UserToken)
                .Where(x => x.UserToken.UserId == user.Id)
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();

            ViewBag.AvailableTokens = availableTokens;
            ViewBag.UsedTokens = usedTokens;
            ViewBag.TotalPurchasedTokens = totalPurchasedTokens;
            ViewBag.RemainingPurchases = remainingPurchases;
            ViewBag.IsLimitReached = limitReached;

            ViewBag.TokenHistory = tokenHistory;

            return View(userToken);
        }

        // =========================
        // SUCCESS PAGE
        // =========================

        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        // =========================
        // TOKEN REDIRECT DECIDER
        // =========================
        [HttpGet]
        public async Task<IActionResult> RedirectToken(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Count used/approved tokens
            int totalTokens = await _context.TokenPayments
                .Include(x => x.UserToken)
                .CountAsync(x =>
                    x.UserToken.UserId == user.Id &&
                    (x.PaymentStatus == "Approved" ||
                     x.PaymentStatus == "Used"));

            // User reached token limit
            if (totalTokens >= 3)
            {
                TempData["Error"] =
                    "You have already used all 3 token borrows. Please purchase membership.";

                return RedirectToAction("Index", "Membership");
            }

            // Check pending request
            bool hasPending = await _context.TokenPayments
                .Include(x => x.UserToken)
                .AnyAsync(x =>
                    x.UserToken.UserId == user.Id &&
                    x.PaymentStatus == "Pending");

            if (hasPending)
            {
                return RedirectToAction(nameof(MyTokens));
            }

            // Can buy another token
            return RedirectToAction(nameof(BuyToken), new { bookId });
        }


        [HttpGet]
        public async Task<IActionResult> GetDepositAmount(int bookId)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(x => x.Id == bookId);

            if (book == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Book not found."
                });
            }

            return Json(new
            {
                success = true,
                depositAmount = book.DepositAmount
            });
        }
    }
}
