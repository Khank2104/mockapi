using Microsoft.AspNetCore.Mvc;

namespace UserManagementSystem.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return View(new[] { 1, 2, 3, 4 });
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("api/test/claims")]
        public IActionResult GetClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(new { 
                User.Identity.IsAuthenticated, 
                User.Identity.Name, 
                RoleClaimType = ((System.Security.Claims.ClaimsIdentity)User.Identity).RoleClaimType,
                IsAdmin = User.IsInRole("admin"),
                IsSuperuser = User.IsInRole("superuser"),
                Claims = claims 
            });
        }
    }
}