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

            //  Lưu thông tin vào Session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetInt32("RoleId", user.RoleId);

            //  Lấy RoleName để phân quyền
            var role = _context.Roles.FirstOrDefault(r => r.RoleId == user.RoleId);
            string roleName = role?.RoleName ?? "Unknown";

            TempData["SuccessMessage"] = $"Xin chào {user.FullName} 💖 ({roleName})";

            //  Điều hướng theo quyền
            if (roleName == "Admin")
                return RedirectToAction("Index", "Admin");
            else if (roleName == "Student")
                return RedirectToAction("Index", "Student");
            else if (roleName == "Giảng viên")
                return RedirectToAction("Index", "Teacher");
            else
                return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}
