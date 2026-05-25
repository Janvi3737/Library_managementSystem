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

        private readonly UserManager<ApplicationUser>
            _userManager;

        public MembershipController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;

            _userManager = userManager;
        }

        // =====================================================
        // MEMBERSHIP PAGE
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (await _userManager.IsInRoleAsync(
                user,
                "Member"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Member" });
            }

            return View();
        }

        // =====================================================
        // BUY MEMBERSHIP
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Buy(
            string membershipType,
            int durationMonths)
        {
            decimal fee = 0;


            if (membershipType == "Student")
            {
                fee = durationMonths == 1 ? 99 : 1000;
            }


            else if (membershipType == "Regular")
            {
                fee = durationMonths == 1 ? 149 : 1500;
            }


            else if (membershipType == "Premium")
            {
                fee = durationMonths == 1 ? 299 : 3000;
            }

            TempData["MembershipType"] = membershipType;

            TempData["DurationMonths"] = durationMonths.ToString();

            TempData["Fee"] = fee.ToString();

            return RedirectToAction("Checkout");
        }

        // =====================================================
        // CHECKOUT PAGE
        // =====================================================

        [HttpGet]
        public IActionResult Checkout()
        {
            ViewBag.MembershipType =
                TempData["MembershipType"];

            ViewBag.DurationMonths =
                TempData["DurationMonths"];

            ViewBag.Fee =
                TempData["Fee"];

            TempData.Keep();

            return View();
        }

        // =====================================================
        // PAYMENT SUCCESS
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentSuccess(
    IFormFile screenshot,
    string paymentMethod,
    string transactionId)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            // ================= GET DATA =================

            string membershipType =
                TempData["MembershipType"]
                ?.ToString();

            int durationMonths =
                Convert.ToInt32(
                    TempData["DurationMonths"]);

            decimal fee =
                Convert.ToDecimal(
                    TempData["Fee"]);

            TempData.Keep();

            // ================= FIND MEMBER =================

            var member =
                await _context.Members
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId ==
                    user.Id);

            // ================= CREATE MEMBER =================

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

            // ================= CREATE MEMBERSHIP =================

            var membership = new Membership
            {
                MemberId = member.Id,

                MembershipType =
                    membershipType,

                DurationMonths =
                    durationMonths,

                StartDate = DateTime.Now,

                EndDate = DateTime.Now
                    .AddMonths(durationMonths),

                Fee = fee,

                IsActive = true
            };

            _context.Memberships
                .Add(membership);

            await _context.SaveChangesAsync();

            // ================= SAVE SCREENSHOT =================

            string screenshotPath = "";

            if (screenshot != null)
            {
                // Path.Combine with an embedded "/" segment leaves the slash
                // intact, which works on Windows but produces an invalid path
                // on Mac/Linux. Pass each path segment separately so the OS
                // separator is used.
                string folder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "paymentproof");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName =
                    Guid.NewGuid().ToString()
                    + Path.GetExtension(screenshot.FileName);

                string filePath =
                    Path.Combine(folder, fileName);

                using (var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
                {
                    await screenshot.CopyToAsync(stream);
                }

                screenshotPath =
                    "/paymentproof/" + fileName;
            }

            // ================= SAVE PAYMENT =================

            var payment =
                new MembershipPayment
                {
                    MembershipId =
                        membership.Id,

                    Amount = fee,

                    PaymentMethod =
                        paymentMethod,

                    PaymentStatus =
                        "Pending",

                    // Prefer the user-entered transaction ID from the payment
                    // form; only fall back to a generated id if absent.
                    TransactionId =
                        string.IsNullOrWhiteSpace(transactionId)
                            ? Guid.NewGuid().ToString()
                            : transactionId.Trim(),

                    PaymentDate =
                        DateTime.Now
                };

            _context.MembershipPayments
                .Add(payment);

            await _context.SaveChangesAsync();

            // ================= ROLE UPDATE =================

            if (await _userManager
                .IsInRoleAsync(user, "User"))
            {
                await _userManager
                    .RemoveFromRoleAsync(
                        user,
                        "User");
            }

            if (!await _userManager
                .IsInRoleAsync(user, "Member"))
            {
                await _userManager
                    .AddToRoleAsync(
                        user,
                        "Member");
            }

            TempData["Success"] =
                "Payment submitted successfully.";

            return RedirectToAction(
                "Success");
        }

        // =====================================================
        // SUCCESS PAGE
        // =====================================================

        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }
    }
}
