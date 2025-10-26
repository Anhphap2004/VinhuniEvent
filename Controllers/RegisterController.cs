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
           
            var existingEmail = _context.Users.FirstOrDefault(u => u.Email == user.Email);
            var existingPhone = _context.Users.FirstOrDefault(u => u.PhoneNumber == user.PhoneNumber);
            var existingStudentCode = _context.Users.FirstOrDefault(u => u.StudentCode == user.StudentCode);

            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "⚠ Email đã tồn tại.");
            }
            if (existingPhone != null)
            {
                ModelState.AddModelError("PhoneNumber", "⚠ Số điện thoại đã được sử dụng.");
            }
            if (existingStudentCode != null)
            {
                ModelState.AddModelError("StudentCode", "⚠ Mã sinh viên đã được sử dụng.");
            }

            var existingEmail = _context.Users.FirstOrDefault(u => u.Email == user.Email);
            var existingPhone = _context.Users.FirstOrDefault(u => u.PhoneNumber == user.PhoneNumber);
            var existingStudentCode = _context.Users.FirstOrDefault(u => u.StudentCode == user.StudentCode);

            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "⚠ Email đã tồn tại.");
            }
            if (existingPhone != null)
            {
                ModelState.AddModelError("PhoneNumber", "⚠ Số điện thoại đã được sử dụng.");
            }
            if (existingStudentCode != null)
            {
                ModelState.AddModelError("StudentCode", "⚠ Mã sinh viên đã được sử dụng.");
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                ModelState.AddModelError("PasswordHash", "⚠ Mật khẩu không hợp lệ!");
                return View(user);
            }
            if (!ModelState.IsValid)
            {
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