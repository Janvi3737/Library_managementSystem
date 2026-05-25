
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
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            EmailService emailService,
            IWebHostEnvironment env,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _env = env;
            _logger = logger;
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

                    // In Development we skip email confirmation entirely so the
                    // dev loop works without depending on Gmail SMTP delivery
                    // (which silently fails on networks blocking port 587 and
                    // leaves the user permanently unable to log in).
                    if (_env.IsDevelopment())
                    {
                        user.EmailConfirmed = true;
                        await _userManager.UpdateAsync(user);

                        TempData["Success"] =
                            "Registration successful. You can log in now (dev mode skips email confirmation).";

                        return RedirectToAction("Login");
                    }

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

                        TempData["Success"] =
                            "Registration successful. Check your inbox for the confirmation link.";
                    }
                    catch (Exception ex)
                    {
                        // Don't swallow silently — the user would think registration
                        // worked, then be unable to log in with no clue why.
                        _logger.LogError(ex,
                            "Failed to send confirmation email to {Email}", user.Email);

                        TempData["Error"] =
                            "Account created but the confirmation email could not be sent. " +
                            "Use 'Resend confirmation email' on the login page.";
                    }

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

        // ================= RESEND CONFIRMATION =================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email is required.";
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByEmailAsync(email);

            // Don't disclose whether the email exists — same response either way
            // so attackers can't enumerate registered accounts.
            if (user == null || user.EmailConfirmed)
            {
                TempData["Success"] =
                    "If an unconfirmed account exists for that email, a new confirmation link has been sent.";
                return RedirectToAction("Login");
            }

            // Dev convenience: don't try to send mail, just confirm immediately.
            if (_env.IsDevelopment())
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                TempData["Success"] =
                    "Email confirmed (dev mode). You can log in now.";

                return RedirectToAction("Login");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Account",
                new
                {
                    userId = user.Id,
                    token = WebUtility.UrlEncode(token)
                },
                Request.Scheme);

            string body = $@"
                <h2>Confirm your email</h2>
                <p>Hello {user.FullName},</p>
                <p>Click the link below to confirm your email.</p>
                <a href='{confirmationLink}'>Confirm Email</a>";

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Confirm Your Email",
                    body);

                TempData["Success"] =
                    "If an unconfirmed account exists for that email, a new confirmation link has been sent.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to resend confirmation email to {Email}", user.Email);

                TempData["Error"] =
                    "Could not send the confirmation email. Please contact support.";
            }

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
                    ModelState.AddModelError("",
                        "No account found for this email. Please register first.");
                    return View(model);
                }

                if (!user.EmailConfirmed)
                {
                    // Surface the resend option on the login page so the user can
                    // recover without contacting an admin.
                    ViewBag.ShowResendConfirmation = true;
                    ViewBag.ResendEmail = model.Email;

                    ModelState.AddModelError("",
                        "Email not confirmed. Use the 'Resend confirmation email' link below.");

                    return View(model);
                }

                var result =
                    await _signInManager.PasswordSignInAsync(
                        user.UserName,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: true);

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

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("",
                        "Account temporarily locked due to too many failed login attempts. Try again later.");
                }
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError("",
                        "Login not allowed for this account.");
                }
                else
                {
                    ModelState.AddModelError("",
                        "Incorrect password.");
                }
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
            return RedirectToAction("Index", "Membership");
        }
    }
}
