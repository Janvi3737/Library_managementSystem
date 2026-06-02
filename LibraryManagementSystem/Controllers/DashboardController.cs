using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    // The real admin dashboard lives at HomeController.Index — no Views/Dashboard/
    // folder exists. Direct hits to /Dashboard used to crash with "view not found";
    // redirect so the URL stays usable.
    public IActionResult Index() => RedirectToAction("Index", "Home");
}