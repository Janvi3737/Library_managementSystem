using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    // The real admin dashboard lives at HomeController.Index — no Views/Dashboard/
    public IActionResult Index() => RedirectToAction("Index", "Home");
}
