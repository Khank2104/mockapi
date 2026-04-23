using Microsoft.AspNetCore.Mvc;

namespace UserManagementSystem.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return View(new[] { 1, 2, 3, 4 });
        }
    }
}