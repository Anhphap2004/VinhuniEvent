using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Filters;
using VinhuniEvent.Models;
using VinhuniEvent.Services;
namespace VinhuniEvent.Areas.Admin.Controllers
{
    [RoleAuthorize(1)]
    [Area("Admin")]
    public class RoleRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public RoleRequestsController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ================================
        // LIST REQUEST
        // ================================
        public IActionResult PendingRoleRequests()
        {
            var pending = _context.RoleRequests
                .Include(r => r.User)
                .Where(r => r.Status == "Pending" || r.Status == null)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var approved = _context.RoleRequests
                .Include(r => r.User)
                .Where(r => r.Status == "Approved")
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var rejected = _context.RoleRequests
                .Include(r => r.User)
                .Where(r => r.Status == "Rejected")
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            ViewBag.Pending = pending;
            ViewBag.Approved = approved;
            ViewBag.Rejected = rejected;

            return View();
        }


        // ================================
        // HANDLE REQUEST
        // ================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Handle(int requestId, bool approve) // Thêm async Task
        {
            var request = _context.RoleRequests
                .Include(r => r.User)
                .FirstOrDefault(r => r.RequestId == requestId);

            if (request == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu!";
                return RedirectToAction(nameof(PendingRoleRequests));
            }

            request.Status = approve ? "Approved" : "Rejected";
            if (approve && request.User != null)
            {
                request.User.RoleId = 3; // Cấp quyền Organizer
            }

            await _context.SaveChangesAsync();

            // ================================
            // GỬI EMAIL THÔNG BÁO "PRO"
            // ================================
            if (request.User != null)
            {
                try
                {
                    string statusColor = approve ? "#059669" : "#dc2626";
                    string statusText = approve ? "ĐÃ ĐƯỢC CHẤP THUẬN" : "CHƯA ĐƯỢC CHẤP THUẬN";
                    string icon = approve ? "🌟" : "✉️";

                    var body = $@"
            <div style='background-color:#f3f4f6; padding:40px 10px; font-family:""Segoe UI"",Arial,sans-serif;'>
                <div style='max-width:600px; margin:0 auto; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
                    <div style='background-color:{(approve ? "#1e40af" : "#374151")}; padding:30px; text-align:center; color:#ffffff;'>
                        <div style='font-size:48px; margin-bottom:10px;'>{icon}</div>
                        <h2 style='margin:0; font-size:22px; letter-spacing:1px;'>THÔNG BÁO QUYỀN HẠN</h2>
                    </div>
                    
                    <div style='padding:30px; line-height:1.6; color:#374151;'>
                        <p>Chào <strong>{request.User.FullName}</strong>,</p>
                        <p>Cảm ơn bạn đã gửi yêu cầu nâng cấp quyền hạn tại <strong>Vinhuni Event</strong>. Ban quản trị đã xem xét hồ sơ của bạn và sau đây là kết quả:</p>
                        
                        <div style='background-color:#f9fafb; border-left:4px solid {statusColor}; padding:20px; margin:25px 0;'>
                            <p style='margin:0; font-size:14px; color:#6b7280;'>Trạng thái yêu cầu:</p>
                            <p style='margin:5px 0 0; font-size:18px; color:{statusColor}; font-weight:bold;'>{statusText}</p>
                            <p style='margin:10px 0 0; font-size:14px;'>Vai trò hiện tại: <strong>{(approve ? "Organizer (Ban tổ chức)" : "Member")}</strong></p>
                        </div>

                        {(approve
                                    ? "<p>Chúc mừng! Giờ đây bạn đã có quyền <strong>tạo và quản lý các sự kiện</strong> của riêng mình. Hãy bắt đầu tạo nên những hoạt động ý nghĩa nhé!</p>"
                                    : "<p>Rất tiếc, yêu cầu của bạn chưa phù hợp ở thời điểm hiện tại. Đừng nản lòng, bạn vẫn có thể tiếp tục tham gia các sự kiện với tư cách thành viên.</p>")}
                        
                        <div style='text-align:center; margin-top:30px;'>
                            <a href='https://vinhuni.edu.vn/Admin' style='background-color:#1e40af; color:#ffffff; padding:12px 30px; text-decoration:none; border-radius:8px; font-weight:bold; display:inline-block;'>VÀO TRANG QUẢN TRỊ</a>
                        </div>
                    </div>

                    <div style='background-color:#f9fafb; padding:20px; text-align:center; color:#9ca3af; font-size:12px; border-top:1px solid #eee;'>
                        © {DateTime.Now.Year} Vinhuni Event Team - Hệ thống quản lý sự kiện Đại học Vinh
                    </div>
                </div>
            </div>";

                    await _emailService.SendEmailAsync(request.User.Email, $"[Vinhuni Event] Kết quả yêu cầu quyền Organizer", body);
                }
                catch { /* Log error if necessary */ }
            }

            TempData["Msg"] = approve
                ? $"Đã duyệt yêu cầu của {request.User?.FullName} ❤️"
                : "Yêu cầu đã bị từ chối 😢";

            return RedirectToAction(nameof(PendingRoleRequests));
        }

        // ================================
        // XEM CHI TIẾT
        // ================================
        public IActionResult Details(int id)
        {
            var req = _context.RoleRequests
                .Include(r => r.User)
                .FirstOrDefault(r => r.RequestId == id);

            if (req == null)
                return NotFound();

            return View(req);
        }
    }
}
