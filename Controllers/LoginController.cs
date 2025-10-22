using Microsoft.AspNetCore.Mvc;
using VinhuniEvent.Models;
using BCrypt.Net;

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
            TempData["SuccessMessage"] = $"Xin chào {user.FullName} 💖";
            return RedirectToAction("Index", "Home");
        }
    }
}
