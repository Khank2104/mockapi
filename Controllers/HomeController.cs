using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;

namespace UserManagementSystem.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var role = User.FindFirst("role")?.Value?.ToLower();
        if (role == "superuser" || role == "admin")
        {
            return RedirectToAction("Index", "Admin");
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Profile()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
