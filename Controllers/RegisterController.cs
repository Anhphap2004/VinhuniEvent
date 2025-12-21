using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Models;

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
        [HttpGet]
        public IActionResult RequestOrganizer()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Index", "Login");
            } 

            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserId == userId);

            if (user?.Role?.RoleId == 3)
            {
                ViewBag.IsOrganizer = true;
                ViewBag.HasPendingRequest = false;
                return View();
            }

            var hasPending = _context.RoleRequests
                .Any(r => r.UserId == userId && r.Status == "Pending" && r.RequestedRole == "Organizer");

            ViewBag.IsOrganizer = false;
            ViewBag.HasPendingRequest = hasPending;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestOrganizer(RoleRequest model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId <= 0)
            {
                TempData["Msg"] = "Vui lòng đăng nhập để gửi yêu cầu.";
                return RedirectToAction(nameof(RequestOrganizer));
            }

            // Check duplicate
            bool alreadyRequested = _context.RoleRequests
                .Any(r => r.UserId == userId.Value && r.RequestedRole == "Organizer" && r.Status == "Pending");

            if (alreadyRequested)
            {
                TempData["Msg"] = "Bạn đã gửi yêu cầu làm Organizer, vui lòng chờ Admin duyệt.";
                return RedirectToAction(nameof(RequestOrganizer));
            }

            if (ModelState.IsValid)
            {
                model.UserId = userId.Value;
                model.Status = "Pending";
                model.CreatedAt = DateTime.Now;

                _context.RoleRequests.Add(model);
                _context.SaveChanges();

                return RedirectToAction(nameof(RequestOrganizer));
            }

            return View(model);
        }
    }
}