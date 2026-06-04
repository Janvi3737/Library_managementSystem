using System.Net;
using LibraryManagementSystem.ClassLibrary.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        private const string ADMIN_KEY = "LIBRARY@2026";

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailService emailService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
        }

        // ===== LOGIN =====

        [HttpGet]
        public IActionResult Login()
        {
            // Fresh install: send first visitor to Register so they can seed an admin
            if (!_userManager.Users.Any())
            {
                TempData["Info"] = "No admin account exists yet. Register the first admin to get started.";
                return RedirectToAction(nameof(Register));
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "Account not found.");
                return View(model);
            }

            // Block login until email is confirmed — same policy as the user app
            if (!user.EmailConfirmed)
            {
                ViewBag.ShowResendConfirmation = true;
                ViewBag.ResendEmail = model.Email;
                ModelState.AddModelError("",
                    "Please confirm your email first. Check your inbox for the link " +
                    "or use 'Resend confirmation email' below.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            if (result.IsLockedOut)
                ModelState.AddModelError("", "Account locked due to too many failed logins. Try again later.");
            else if (result.IsNotAllowed)
                ModelState.AddModelError("", "Login not allowed.");
            else
                ModelState.AddModelError("Password", "Incorrect password.");

            return View(model);
        }

        // ===== REGISTER =====

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Email = model.Email?.Trim();

            if (model.PrivateKey != ADMIN_KEY)
            {
                ModelState.AddModelError("PrivateKey", "Invalid Admin Security Key.");
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Admin");

            // Send confirmation email
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = Url.Action("ConfirmEmail", "Account",
                new { userId = user.Id, token = WebUtility.UrlEncode(token) },
                Request.Scheme);

            var body =
                $"<h2>Welcome to BookVerse Admin</h2>" +
                $"<p>Hello {WebUtility.HtmlEncode(user.FullName)},</p>" +
                $"<p>Please confirm your admin email before signing in.</p>" +
                $"<p><a href='{confirmLink}' " +
                $"style='display:inline-block;padding:10px 18px;background:#7c3aed;color:white;" +
                $"border-radius:8px;text-decoration:none;'>Confirm Email</a></p>" +
                $"<p style='color:#666;font-size:12px'>If the button doesn't work paste this URL: {confirmLink}</p>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, "Confirm your admin email", body);
                TempData["Success"] = "Account created. Check your inbox to confirm before logging in.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send admin confirmation email to {Email}", user.Email);
                TempData["Error"] =
                    "Account created but the confirmation email could not be sent. " +
                    "Use 'Resend confirmation email' on the login page once SMTP is fixed.";
            }

            return RedirectToAction(nameof(Login));
        }

        // ===== CONFIRM EMAIL =====

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var decoded = WebUtility.UrlDecode(token);
            var result = await _userManager.ConfirmEmailAsync(user, decoded);

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? "Email confirmed. You can log in now." : "Email confirmation failed.";

            return RedirectToAction(nameof(Login));
        }

        // ===== RESEND CONFIRMATION =====

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email is required.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByEmailAsync(email);

            // Same response either way so attackers can't enumerate emails
            var genericSuccess =
                "If an unconfirmed account exists for that email, a new confirmation link has been sent.";

            if (user == null || user.EmailConfirmed)
            {
                TempData["Success"] = genericSuccess;
                return RedirectToAction(nameof(Login));
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = Url.Action("ConfirmEmail", "Account",
                new { userId = user.Id, token = WebUtility.UrlEncode(token) },
                Request.Scheme);

            var body =
                $"<h2>Confirm your admin email</h2>" +
                $"<p>Hello {WebUtility.HtmlEncode(user.FullName)},</p>" +
                $"<p><a href='{confirmLink}'>Confirm Email</a></p>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, "Confirm your admin email", body);
                TempData["Success"] = genericSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend admin confirmation to {Email}", user.Email);
                TempData["Error"] = "Could not send the confirmation email. Check SMTP settings.";
            }

            return RedirectToAction(nameof(Login));
        }

        // ===== LOGOUT =====

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Success"] = "Logged out successfully.";
            return RedirectToAction(nameof(Login));
        }

        // ===== ACCESS DENIED =====

        public IActionResult AccessDenied(string? message = null)
        {
            ViewBag.ErrorMessage = message ?? "You are not authorized to access this page.";
            return View();
        }
    }
}
