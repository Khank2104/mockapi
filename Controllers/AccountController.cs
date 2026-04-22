using Microsoft.AspNetCore.Mvc;

namespace UserManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Logout()
        {
            // Xử lý logout phía Client qua JS sẽ đơn giản hơn cho bài này
            return RedirectToAction("Login");
        }
    }
}
