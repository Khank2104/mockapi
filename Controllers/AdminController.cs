using Microsoft.AspNetCore.Mvc;

namespace UserManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
