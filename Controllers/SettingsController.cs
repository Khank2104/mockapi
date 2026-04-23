using Microsoft.AspNetCore.Mvc;

namespace UserManagementSystem.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
