using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Library_Management_System.Controllers
{
    [Authorize(Roles = "User,Member")]
    public class MembershipController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public MembershipController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }


[HttpGet]
public async Task<IActionResult> Index()
{
    var user =
        await _userManager.GetUserAsync(User);

    if (user == null)
    {
        return RedirectToAction(
            "Login",
            "Account");
    }

    // CHECK ACTIVE MEMBERSHIP FROM DATABASE
    var member =
        await _context.Members
        .FirstOrDefaultAsync(x =>
            x.ApplicationUserId == user.Id);

    if (member != null)
    {
        var activeMembership =
            await _context.Memberships
            .AnyAsync(x =>
                x.MemberId == member.Id &&
                x.IsActive &&
                x.EndDate >= DateTime.Now);

        // ONLY REDIRECT IF MEMBERSHIP APPROVED + ACTIVE
        if (activeMembership)
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Member" });
        }
    }

    return View();
}
        // BUY MEMBERSHIP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Buy(string membershipType, int durationMonths)
        {
            decimal fee = 0;

            if (membershipType == "Student")
                fee = durationMonths == 1 ? 99 : 1000;

            else if (membershipType == "Regular")
                fee = durationMonths == 1 ? 149 : 1500;

            else if (membershipType == "Premium")
                fee = durationMonths == 1 ? 299 : 3000;

            TempData["MembershipType"] = membershipType;
            TempData["DurationMonths"] = durationMonths.ToString();
            TempData["Fee"] = fee.ToString();

            return RedirectToAction("Checkout");
        }

        // CHECKOUT
        [HttpGet]
        public IActionResult Checkout()
        {
            ViewBag.MembershipType = TempData["MembershipType"];
            ViewBag.DurationMonths = TempData["DurationMonths"];
            ViewBag.Fee = TempData["Fee"];

            TempData.Keep();
            return View();
        }

        // PAYMENT SUCCESS (USER SUBMISSION)
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

            // ================= SAFE TEMP DATA =================
            string membershipType = TempData["MembershipType"]?.ToString() ?? "";
            int durationMonths = TempData["DurationMonths"] != null
                ? Convert.ToInt32(TempData["DurationMonths"])
                : 0;

            decimal fee = TempData["Fee"] != null
                ? Convert.ToDecimal(TempData["Fee"])
                : 0;

            TempData.Keep();

            // ================= MEMBER =================
            var member = await _context.Members
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (member == null)
            {
                member = new Member
                {
                    ApplicationUserId = user.Id,
                    Name = user.FullName,
                    Email = user.Email,
                    Phone = user.PhoneNumber
                };

                _context.Members.Add(member);
                await _context.SaveChangesAsync();
            }

            // ================= BLOCK ACTIVE MEMBERSHIP =================
            var alreadyActive = await _context.Memberships.AnyAsync(m =>
                m.MemberId == member.Id &&
                m.IsActive &&
                m.EndDate >= DateTime.Now);

            if (alreadyActive)
            {
                TempData["Error"] = "Active membership already exists.";
                return RedirectToAction("Index", "Dashboard", new { area = "Member" });
            }

            // ================= CREATE MEMBERSHIP (WAIT FOR ADMIN) =================
            var membership = new Membership
            {
                MemberId = member.Id,
                MembershipType = membershipType,
                DurationMonths = durationMonths,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(durationMonths),

                Fee = fee,

                // 🔴 IMPORTANT: WAIT FOR ADMIN APPROVAL
                IsActive = false
            };

            _context.Memberships.Add(membership);
            await _context.SaveChangesAsync();

            // ================= SAVE SCREENSHOT =================
            string screenshotPath = null;

            if (screenshot != null)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/paymentproof");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(screenshot.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await screenshot.CopyToAsync(stream);
                }

                screenshotPath = "/paymentproof/" + fileName;
            }

            // ================= PAYMENT (PENDING) =================
            var payment = new MembershipPayment
            {
                MembershipId = membership.Id,
                Amount = fee,
                PaymentMethod = paymentMethod,
                PaymentStatus = "Pending", // 🔴 ADMIN WILL APPROVE
                TransactionId = transactionId ?? Guid.NewGuid().ToString(),
                ScreenshotPath = screenshotPath,
                PaymentDate = DateTime.Now
            };

            _context.MembershipPayments.Add(payment);
            await _context.SaveChangesAsync();

            // ================= ROLE UPDATE =================


            TempData["Success"] = "Payment submitted. Awaiting admin approval.";
            return RedirectToAction("Success");
        }

        // RENEW MEMBERSHIP (NEW FEATURE)
       public async Task<IActionResult> Renew(int membershipId)
{
    var membership = await _context.Memberships
        .FirstOrDefaultAsync(x => x.Id == membershipId);

    if (membership == null)
        return NotFound();

    // only expired memberships can renew
    if (membership.EndDate > DateTime.Now)
    {
        TempData["Error"] = "Membership is still active.";
        return RedirectToAction("Index");
    }

    TempData["MembershipType"] = membership.MembershipType;
    TempData["DurationMonths"] = membership.DurationMonths.ToString();
    TempData["Fee"] = membership.Fee.ToString();

    return RedirectToAction("Checkout");
}

        // SUCCESS PAGE
        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        
    }
}
