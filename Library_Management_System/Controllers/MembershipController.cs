// using LibraryManagementSystem.ClassLibrary.Data;
// using LibraryManagementSystem.ClassLibrary.Models;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using System.IO;

// namespace Library_Management_System.Controllers
// {
//     [Authorize(Roles = "User,Member")]
//     public class MembershipController : Controller
//     {

//         private readonly AppDbContext _context;

//         private readonly UserManager<ApplicationUser>
//             _userManager;

//         private readonly SignInManager<ApplicationUser>
//             _signInManager;

//         public MembershipController(
//             AppDbContext context,
//             UserManager<ApplicationUser> userManager,
//             SignInManager<ApplicationUser> signInManager)
//         {
//             _context = context;

//             _userManager = userManager;

//             _signInManager = signInManager;
//         }

//         // =====================================================
//         // MEMBERSHIP PAGE
//         // =====================================================

//         [HttpGet]
//         public async Task<IActionResult> Index()
//         {
//             var user =
//                 await _userManager.GetUserAsync(User);

//             if (await _userManager.IsInRoleAsync(
//                 user,
//                 "Member"))
//             {
//                 return RedirectToAction(
//                     "Index",
//                     "Dashboard",
//                     new { area = "Member" });
//             }

//             return View();
//         }

//         // =====================================================
//         // BUY MEMBERSHIP
//         // =====================================================

//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public IActionResult Buy(
//             string membershipType,
//             int durationMonths)
//         {
//             decimal fee = 0;


//             if (membershipType == "Student")
//             {
//                 fee = durationMonths == 1 ? 99 : 1000;
//             }


//             else if (membershipType == "Regular")
//             {
//                 fee = durationMonths == 1 ? 149 : 1500;
//             }


//             else if (membershipType == "Premium")
//             {
//                 fee = durationMonths == 1 ? 299 : 3000;
//             }

//             TempData["MembershipType"] = membershipType;

//             TempData["DurationMonths"] = durationMonths.ToString();

//             TempData["Fee"] = fee.ToString();

//             return RedirectToAction("Checkout");
//         }

//         // =====================================================
//         // CHECKOUT PAGE
//         // =====================================================

//         [HttpGet]
//         public IActionResult Checkout()
//         {
//             ViewBag.MembershipType =
//                 TempData["MembershipType"];

//             ViewBag.DurationMonths =
//                 TempData["DurationMonths"];

//             ViewBag.Fee =
//                 TempData["Fee"];

//             TempData.Keep();

//             return View();
//         }

//         // =====================================================
//         // PAYMENT SUCCESS
//         // =====================================================

//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> PaymentSuccess(
//     IFormFile screenshot,
//     string paymentMethod)
//         {
//             var user =
//                 await _userManager.GetUserAsync(User);

//             if (user == null)
//             {
//                 return RedirectToAction(
//                     "Login",
//                     "Account");
//             }

//             // ================= GET DATA =================

//             string membershipType =
//                 TempData["MembershipType"]
//                 ?.ToString();

//             int durationMonths =
//                 Convert.ToInt32(
//                     TempData["DurationMonths"]);

//             decimal fee =
//                 Convert.ToDecimal(
//                     TempData["Fee"]);

//             TempData.Keep();

//             // ================= FIND MEMBER =================

//             var member =
//                 await _context.Members
//                 .FirstOrDefaultAsync(x =>
//                     x.ApplicationUserId ==
//                     user.Id);

//             // ================= CREATE MEMBER =================

//             if (member == null)
//             {
//                 member = new Member
//                 {
//                     ApplicationUserId = user.Id,

//                     Name = user.FullName,

//                     Email = user.Email,

//                     Phone = user.PhoneNumber
//                 };

//                 _context.Members.Add(member);

//                 await _context.SaveChangesAsync();
//             }

//             // ================= GUARD: ALREADY ACTIVE MEMBERSHIP =================
//             // Without this check, double-submitting the checkout form (or
//             // simply going through Buy a second time before the first
//             // membership expires) creates duplicate Membership rows + a
//             // duplicate MembershipPayment "Pending" row.
//             var alreadyActive =
//                 await _context.Memberships.AnyAsync(m =>
//                     m.MemberId == member.Id &&
//                     m.IsActive &&
//                     m.EndDate >= DateTime.Now);

//             if (alreadyActive)
//             {
//                 TempData["Error"] =
//                     "You already have an active membership. " +
//                     "Wait until it expires before buying another.";
//                 return RedirectToAction(
//                     "Index",
//                     "Dashboard",
//                     new { area = "Member" });
//             }

//             // ================= CREATE MEMBERSHIP =================

//             var membership = new Membership
//             {
//                 MemberId = member.Id,

//                 MembershipType =
//                     membershipType,

//                 DurationMonths =
//                     durationMonths,

//                 StartDate = DateTime.Now,

//                 EndDate = DateTime.Now
//                     .AddMonths(durationMonths),

//                 Fee = fee,

//                 IsActive = true
//             };

//             _context.Memberships
//                 .Add(membership);

//             await _context.SaveChangesAsync();

//             // ================= SAVE SCREENSHOT =================

//             string screenshotPath = "";

//             if (screenshot != null)
//             {
//                 string folder =
//                     Path.Combine(
//                         Directory.GetCurrentDirectory(),
//                         "wwwroot/paymentproof");

//                 if (!Directory.Exists(folder))
//                 {
//                     Directory.CreateDirectory(folder);
//                 }

//                 string fileName =
//                     Guid.NewGuid().ToString()
//                     + Path.GetExtension(screenshot.FileName);

//                 string filePath =
//                     Path.Combine(folder, fileName);

//                 using (var stream =
//                     new FileStream(
//                         filePath,
//                         FileMode.Create))
//                 {
//                     await screenshot.CopyToAsync(stream);
//                 }

//                 screenshotPath =
//                     "/paymentproof/" + fileName;
//             }

//             // ================= SAVE PAYMENT =================

//             var payment =
//                 new MembershipPayment
//                 {
//                     MembershipId =
//                         membership.Id,

//                     Amount = fee,

//                     PaymentMethod =
//                         paymentMethod,

//                     PaymentStatus =
//                         "Pending",

//                     TransactionId =
//                         Guid.NewGuid().ToString(),

//                     // Was being computed above but never stored — the admin
//                     // approval page needs this to render the proof image.
//                     ScreenshotPath =
//                         string.IsNullOrEmpty(screenshotPath)
//                             ? null
//                             : screenshotPath,

//                     PaymentDate =
//                         DateTime.Now
//                 };

//             _context.MembershipPayments
//                 .Add(payment);

//             await _context.SaveChangesAsync();

//             // ================= ROLE UPDATE =================

//             if (await _userManager
//                 .IsInRoleAsync(user, "User"))
//             {
//                 await _userManager
//                     .RemoveFromRoleAsync(
//                         user,
//                         "User");
//             }

//             if (!await _userManager
//                 .IsInRoleAsync(user, "Member"))
//             {
//                 await _userManager
//                     .AddToRoleAsync(
//                         user,
//                         "Member");
//             }

//             // Without this, the new "Member" role is in the DB but the user's
//             // auth cookie still carries the old claims — they'd be redirected
//             // away from any [Authorize(Roles="Member")] page until next login.
//             await _signInManager.RefreshSignInAsync(user);

//             TempData["Success"] =
//                 "Payment submitted successfully.";

//             return RedirectToAction(
//                 "Success");
//         }
//         // =====================================================
//         // SUCCESS PAGE
//         // =====================================================

//         [HttpGet]
//         public IActionResult Success()
//         {
//             return View();
//         }
//     }
// }

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

        // =====================================================
        // MEMBERSHIP PAGE
        // =====================================================
        // [HttpGet]
        // public async Task<IActionResult> Index()
        // {
        //     var user = await _userManager.GetUserAsync(User);

        //     if (user != null && await _userManager.IsInRoleAsync(user, "Member"))
        //     {
        //         return RedirectToAction("Index", "Dashboard", new { area = "Member" });
        //     }

        //     return View();
        // }
        // =====================================================
// MEMBERSHIP PAGE
// =====================================================

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
        // =====================================================
        // BUY MEMBERSHIP
        // =====================================================
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

        // =====================================================
        // CHECKOUT
        // =====================================================
        [HttpGet]
        public IActionResult Checkout()
        {
            ViewBag.MembershipType = TempData["MembershipType"];
            ViewBag.DurationMonths = TempData["DurationMonths"];
            ViewBag.Fee = TempData["Fee"];

            TempData.Keep();
            return View();
        }

        // =====================================================
        // PAYMENT SUCCESS (USER SUBMISSION)
        // =====================================================
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
            // if (await _userManager.IsInRoleAsync(user, "User"))
            //     await _userManager.RemoveFromRoleAsync(user, "User");

            // ⚠️ Do NOT add Member role until admin approves (recommended)
            // You can move this to Admin Approval later if needed

            // await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Payment submitted. Awaiting admin approval.";
            return RedirectToAction("Success");
        }

        // =====================================================
        // RENEW MEMBERSHIP (NEW FEATURE)
        // =====================================================
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