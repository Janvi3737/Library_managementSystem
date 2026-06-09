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
        public async Task<IActionResult> BuyToken()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var token = await _context.UserTokens
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            int borrowCount = token?.TotalBorrowCount ?? 0;

            // ❌ Block if limit already reached
            if (borrowCount >= 3)
            {
                TempData["Error"] =
                    "Your token limit is over. Please purchase a membership.";

                return RedirectToAction("Index", "Membership");
            }

            ViewBag.TotalBorrowCount = borrowCount;
            ViewBag.RemainingBorrows = 3 - borrowCount;

            ViewBag.HasToken = token?.IsApproved ?? false;

            return View();
        }

        // =========================
        // CHECKOUT
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout()
        {
            TempData["Amount"] = "200";
            TempData.Keep();

            return RedirectToAction(nameof(Payment));
        }

        // =========================
        // PAYMENT PAGE
        // =========================

        [HttpGet]
        public IActionResult Payment()
        {
            ViewBag.Amount = TempData["Amount"];
            TempData.Keep();

            return View();
        }

        // =========================
        // PAYMENT SUCCESS
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentSuccess(
    IFormFile screenshot,
    string paymentMethod,
    string transactionId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var token = await _context.UserTokens
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (token != null && token.TotalBorrowCount >= 3)
            {
                TempData["Error"] =
                    "Your token limit is over. Please purchase membership.";

                return RedirectToAction("Index", "Membership");
            }

            var pending = await _context.TokenPayments
                .Include(x => x.UserToken)
                .FirstOrDefaultAsync(x =>
                    x.UserToken.UserId == user.Id &&
                    x.PaymentStatus == "Pending");

            if (pending != null)
            {
                TempData["Error"] =
                    "You already have a pending token request.";

                return RedirectToAction(nameof(BuyToken));
            }

            // ================= SAVE SCREENSHOT =================
            string screenshotPath = null;

            if (screenshot != null && screenshot.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/tokenproof");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(screenshot.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await screenshot.CopyToAsync(stream);
                }

                screenshotPath = "/tokenproof/" + fileName;
            }

            // ================= TOKEN CREATE =================
            if (token == null)
            {
                token = new UserToken
                {
                    UserId = user.Id,
                    AvailableTokens = 0,
                    TotalBorrowCount = 0,
                    DepositAmount = 200,
                    PurchaseDate = DateTime.Now,
                    IsApproved = false
                };

                _context.UserTokens.Add(token);
                await _context.SaveChangesAsync();
            }

            // ================= PAYMENT =================
            var payment = new TokenPayment
            {
                UserTokenId = token.Id,
                Amount = 200,
                PaymentMethod = paymentMethod,
                TransactionId = string.IsNullOrWhiteSpace(transactionId)
                    ? Guid.NewGuid().ToString()
                    : transactionId,
                PaymentStatus = "Pending",
                ScreenshotPath = screenshotPath,
                PaymentDate = DateTime.Now
            };

            _context.TokenPayments.Add(payment);

            _context.Notifications.Add(new Notification
            {
                MemberId = user.Id,
                Message = "⏳ Token request submitted and waiting for admin approval.",
                Link = "/Member/Notifications/Index",
                IsRead = false,
                CreatedOn = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Token request submitted successfully.";

            return RedirectToAction(nameof(MyTokens));
        }

        // =========================
        // MY TOKENS
        // =========================

        [HttpGet]
        public async Task<IActionResult> MyTokens()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var token = await _context.UserTokens
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            return View(token);
        }

        // =========================
        // SUCCESS PAGE
        // =========================

        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }
    }
}
