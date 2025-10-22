using Microsoft.AspNetCore.Mvc;
using VinhuniEvent.Models;
using BCrypt.Net;

namespace VinhuniEvent.Controllers
{
    public class RegisterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RegisterController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == user.Email || u.PhoneNumber == user.PhoneNumber);
            if (existingUser != null)
            {
                if (existingUser.Email == user.Email)
                {
                    ModelState.AddModelError("Email", "⚠ Email đã tồn tại.");
                }

                if (existingUser.PhoneNumber == user.PhoneNumber)
                {
                    ModelState.AddModelError("PhoneNumber", "⚠ Số điện thoại đã được sử dụng.");
                }
                if(existingUser.StudentCode == user.StudentCode)
                {
                    ModelState.AddModelError("StudentCode", "⚠ Mã sinh viên đã được sử dụng.");
                }
                return View(user);

            }
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                ModelState.AddModelError("PasswordHash", "⚠ Mật khẩu không hợp lệ!");
                return View(user);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.RoleId = 2; 
            user.IsActive = true;
            user.CreatedDate = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "🎉 Đăng ký thành công. Vui lòng đăng nhập!";
            return RedirectToAction("Index", "Login");
        }
    }
}
