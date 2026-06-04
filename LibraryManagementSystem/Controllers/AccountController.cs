using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LibraryManagementSystem.ClassLibrary.Models;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        private const string ADMIN_KEY = "LIBRARY@2026";

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ================= LOGIN =================

        [HttpGet]
        public IActionResult Login()
        {
            // Fresh install — no admin exists yet. Send them to Register
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
                ModelState.AddModelError(
                    "Email",
                    "Account not found.");

                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    "Password",
                    "Incorrect password.");

                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        // ================= REGISTER =================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Email = model.Email?.Trim();

            // ADMIN KEY VALIDATION 

            if (model.PrivateKey != ADMIN_KEY)
            {
                ModelState.AddModelError(
                    "PrivateKey",
                    "Invalid Admin Security Key.");

                return View(model);
            }

            // CHECK EXISTING EMAIL

            var existingUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }

            // CREATE USER

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result =
                await _userManager.CreateAsync(user, model.Password);

            // SUCCESS

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Admin");

                TempData["Success"] =
                    "Account created successfully.";

                return RedirectToAction("Login");
            }

            // IDENTITY ERRORS

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // ================= LOGOUT =================

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            TempData["Success"] =
                "Logged out successfully.";

            return RedirectToAction(nameof(Login));
        }

        // ================= ACCESS DENIED =================

        public IActionResult AccessDenied(string message = null)
        {
            ViewBag.ErrorMessage = message ??
                "You are not authorized to access this page.";

            return View();
        }
    }
}
