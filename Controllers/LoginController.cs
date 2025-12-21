using Microsoft.AspNetCore.Mvc;
using VinhuniEvent.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;

namespace VinhuniEvent.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("Email", "⚠ Tài khoản không tồn tại!");
                return View();
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isPasswordValid)
            {
                ModelState.AddModelError("PasswordHash", "⚠ Mật khẩu không chính xác!");
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetInt32("RoleId", user.RoleId);

            var role = _context.Roles.FirstOrDefault(r => r.RoleId == user.RoleId);
            string roleName = role?.RoleName ?? "Unknown";

            switch (user.RoleId)
            {
                case 1: 
                    return RedirectToAction("Index", "Home", new { area = "Admin" });

                case 2: 
                    return RedirectToAction("Index", "Home");

                case 3: 
                    return RedirectToAction("Index", "Home");

                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Bạn đã đăng xuất thành công";
            return RedirectToAction("Index", "Home");
        }
    }
}
