using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Library_Management_System.Areas.Member.Controllers
{
    //[Area("Member")]
    [Authorize(Roles = "Member,User")]
    public class ProfileController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _env = env;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // 👤 PROFILE PAGE

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" });
            }

            var model = new ProfileViewModel
            {
                Name = user.FullName,
                Phone = user.PhoneNumber,
                NotificationPrefs = user.NotificationPrefs,
                ProfileImagePath = user.ProfileImagePath
            };

            return View(model);
        }

        // ✏️ EDIT PROFILE GET

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" });
            }

            var model = new ProfileViewModel
            {
                Name = user.FullName,
                Phone = user.PhoneNumber,
                NotificationPrefs = user.NotificationPrefs,
                ProfileImagePath = user.ProfileImagePath
            };

            return View(model);
        }

        // ✏️ EDIT PROFILE POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" });
            }

            user.FullName = model.Name;
            user.PhoneNumber = model.Phone;
            user.NotificationPrefs = model.NotificationPrefs;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["success"] =
                "Profile updated successfully";

            return RedirectToAction(nameof(Index));
        }

        // 🖼️ UPLOAD AVATAR GET

        [HttpGet]
        public async Task<IActionResult> Avatar()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" });
            }

            var model = new ProfileViewModel
            {
                ProfileImagePath = user.ProfileImagePath
            };

            return View(model);
        }

        // 🖼️ UPLOAD AVATAR POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Avatar(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" });
            }

            if (model.AvatarFile == null)
            {
                TempData["error"] =
                    "Please select an image";

                return RedirectToAction(nameof(Avatar));
            }

            // VALIDATE FILE TYPE

            var allowedExtensions =
                new[] { ".jpg", ".jpeg", ".png", ".webp" };

            var extension =
                Path.GetExtension(model.AvatarFile.FileName)
                .ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["error"] =
                    "Only JPG, PNG, and WEBP images are allowed";

                return RedirectToAction(nameof(Avatar));
            }

            // DELETE OLD IMAGE

            if (!string.IsNullOrEmpty(user.ProfileImagePath))
            {
                var oldImagePath = Path.Combine(
                    _env.WebRootPath,
                    user.ProfileImagePath.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            // CREATE FOLDER

            var folder = Path.Combine(
                _env.WebRootPath,
                "images",
                "profiles");

            Directory.CreateDirectory(folder);

            // GENERATE FILE NAME

            var fileName =
                Guid.NewGuid().ToString() + extension;

            var filePath = Path.Combine(folder, fileName);

            // SAVE IMAGE

            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await model.AvatarFile.CopyToAsync(stream);
            }

            // SAVE DB PATH

            user.ProfileImagePath =
                "/images/profiles/" + fileName;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["error"] =
                    "Failed to upload profile photo";

                return RedirectToAction(nameof(Avatar));
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["success"] =
                "Profile photo updated successfully";

            return RedirectToAction(nameof(Index));
        }

        // 🔒 CHANGE PASSWORD GET

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // 🔒 CHANGE PASSWORD POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" });
            }

            var result =
                await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword,
                    model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["success"] =
                "Password updated successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
