
using LibraryManagementSystem.ClassLibrary.Models;
using Library_Management_System.ViewModels;
using Library_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        // ================= REGISTER =================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // CREATE ROLES

                if (!await _roleManager.RoleExistsAsync("Member"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Member"));
                }

                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("User"));
                }

                var existingUser =
                    await _userManager.FindByEmailAsync(model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Email already registered.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    FullName = model.FullName,
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = false
                };

                var result =
                    await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // ✅ DEFAULT ROLE = USER

                    await _userManager.AddToRoleAsync(user, "User");

                    // EMAIL CONFIRM TOKEN

                    var token =
                        await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    var confirmationLink = Url.Action(
                        "ConfirmEmail",
                        "Account",
                        new
                        {
                            userId = user.Id,
                            token = WebUtility.UrlEncode(token)
                        },
                        Request.Scheme);

                    // EMAIL BODY

                    string body = $@"
                <h2>Welcome to BookVerse</h2>

                <p>Hello {user.FullName},</p>

                <p>Please confirm your email before login.</p>

                <a href='{confirmationLink}'>
                    Confirm Email
                </a>";

                    try
                    {
                        await _emailService.SendEmailAsync(
                            user.Email,
                            "Confirm Your Email",
                            body);
                    }
                    catch
                    {
                    }

                    TempData["Success"] =
                        "Registration successful. Please confirm your email.";

                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        // ================= EMAIL CONFIRM =================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId,string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Login");
            }

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            token = WebUtility.UrlDecode(token);

            var result =
                await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Email confirmed successfully.";

                return RedirectToAction("Login");
            }

            TempData["Error"] =
                "Email confirmation failed.";

            return RedirectToAction("Login");
        }

        // ================= LOGIN =================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user =
                    await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Account not found.");
                    return View(model);
                }

                // ✅ EMAIL CONFIRM CHECK

                if (!user.EmailConfirmed)
                {
                    ModelState.AddModelError("",
                        "Please confirm your email first.");

                    return View(model);
                }

                var result =
                    await _signInManager.PasswordSignInAsync(
                        user.UserName,
                        model.Password,
                        model.RememberMe,
                        false);

                if (result.Succeeded)
                {

                    if (await _userManager.IsInRoleAsync(user, "Member"))
                    {
                        return RedirectToAction(
                            "Index",
                            "Dashboard",
                            new { area = "Member" });
                    }


                    return RedirectToAction(
                        "Index",
                        "Home");
                }

                ModelState.AddModelError("",
                    "Invalid login attempt.");
            }

            return View(model);
        }

        // ================= LOGOUT =================

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["success"] = "Logged out successfully.";
            return RedirectToAction("Login");
        }

        // ================= FORGOT PASSWORD =================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var resetLink = Url.Action(
                    "ResetPassword",
                    "Account",
                    new
                    {
                        token = WebUtility.UrlEncode(token),
                        email = user.Email
                    },
                    Request.Scheme
                );

                string subject = "Password Reset Link";
                string body = $@"
                    <h3>Password Reset Request</h3>
                    <p>Click below to reset your password:</p>
                    <a href='{resetLink}'>Reset Password</a>
                ";

                try
                {
                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    TempData["success"] = "Reset link sent to email.";
                }
                catch
                {
                    TempData["error"] = "Could not send reset email. Try again later.";
                }

                return View();
            }

            return View(model);
        }

        // ================= RESET PASSWORD =================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            return View(new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction("Login");
                }

                model.Token = WebUtility.UrlDecode(model.Token);

                var result = await _userManager.ResetPasswordAsync(
                    user,
                    model.Token,
                    model.Password);

                if (result.Succeeded)
                {
                    TempData["success"] = "Password reset successful.";
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        public IActionResult AccessDenied()
        {
            // Render the styled AccessDenied.cshtml. Redirecting to Membership
            // hides the "Get Member access to use this feature" CTA the page
            // was built to show.
            return View();
        }
    }
}
