using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Models;
using VinhuniEvent.Services;

namespace VinhuniEvent.Controllers
{
    public class RegisterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public RegisterController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
            string plainPassword = user.PasswordHash;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.RoleId = 2;
            user.IsActive = true;
            user.CreatedDate = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            try
            {
                var body = $@"
<div style='background-color:#f4f7fa; padding:40px 10px; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
    <div style='max-width:600px; margin:0 auto; background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 12px rgba(0,0,0,0.1);'>
        
        <div style='background-color:#0056b3; padding:30px; text-align:center;'>
            <div style='font-size:50px; margin-bottom:10px;'>🎓</div>
            <h1 style='color:#ffffff; margin:0; font-size:24px; letter-spacing:1px; text-transform:uppercase;'>Chào mừng bạn gia nhập!</h1>
        </div>

        <div style='padding:40px 30px;'>
            <p style='font-size:18px; color:#333; margin-top:0;'>Xin chào <strong>{user.FullName}</strong>,</p>
            <p style='color:#555; font-size:16px; line-height:1.6;'>Tài khoản của bạn tại <strong>Vinhuni Event</strong> đã được tạo thành công. Chào mừng bạn đến với nền tảng quản lý và tham gia sự kiện dành riêng cho sinh viên.</p>
            
            <div style='background-color:#f8f9fa; border:1px solid #e9ecef; border-radius:10px; padding:25px; margin:30px 0;'>
                <h3 style='margin-top:0; color:#0056b3; font-size:16px; text-transform:uppercase;'>Thông tin tài khoản của bạn:</h3>
                <table style='width:100%; font-size:15px; border-collapse:collapse;'>
                    <tr>
                        <td style='padding:8px 0; color:#777; width:40%;'>Email đăng nhập:</td>
                        <td style='padding:8px 0; color:#333;'><strong>{user.Email}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0; color:#777;'>Mật khẩu:</td>
                        <td style='padding:8px 0; color:#d9534f;'><strong>{plainPassword}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0; color:#777;'>Ngày đăng ký:</td>
                        <td style='padding:8px 0; color:#333;'>{DateTime.Now:dd/MM/yyyy HH:mm}</td>
                    </tr>
                </table>
            </div>

            <div style='text-align:center; margin-top:35px;'>
                <a href='https://vinhuni.edu.vn/' style='background-color:#28a745; color:#ffffff; padding:15px 35px; text-decoration:none; border-radius:8px; font-weight:bold; font-size:16px; display:inline-block; box-shadow:0 4px 6px rgba(40,167,69,0.3);'>KHÁM PHÁ SỰ KIỆN NGAY</a>
            </div>

            <p style='color:#888; font-size:14px; margin-top:35px; border-top:1px solid #eee; padding-top:20px; font-style:italic;'>
                <strong>Mẹo bảo mật:</strong> Hãy đổi mật khẩu ngay sau khi đăng nhập và tuyệt đối không chia sẻ email này cho bất kỳ ai khác.
            </p>
        </div>

        <div style='background-color:#f1f3f5; padding:20px; text-align:center; color:#999; font-size:12px;'>
            <p style='margin:0;'>© {DateTime.Now.Year} Vinhuni Event Team - Trường Đại học Vinh</p>
            <p style='margin:5px 0 0;'>Địa chỉ: 182 Lê Duẩn, Thành phố Vinh, Nghệ An</p>
        </div>
    </div>
</div>";
                await _emailService.SendEmailAsync(user.Email, "[Vinhuni Event] Đăng ký tài khoản thành công", body);
            }
            catch
            {
                // Bỏ qua lỗi gửi email để không ảnh hưởng tới luồng đăng ký
            }

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