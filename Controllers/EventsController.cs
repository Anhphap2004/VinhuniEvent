using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Models;
using VinhuniEvent.Services;

namespace VinhuniEvent.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public EventsController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? categoryid)
        {
            var baseQuery = _context.Events
                .Include(e => e.Category)
                .Where(e => e.IsActive);

            if (categoryid.HasValue)
            {
                baseQuery = baseQuery.Where(e => e.CategoryId == categoryid);
            }

            ViewBag.Categories = await _context.EventCategories.ToListAsync();

            ViewBag.EventNew = await baseQuery
                .OrderByDescending(e => e.CreatedDate)
                .Take(4)
                .ToListAsync();

            ViewBag.EventHighlight = await baseQuery
                .Where(p => p.Status == "hot")
                .OrderByDescending(e => e.CreatedDate)
                .Take(4)
                .ToListAsync();

            var allEvents = await baseQuery
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();

            return View(allEvents);
        }

        [HttpGet("{slug}-{id}.html")]
        public async Task<IActionResult> Details(string slug, int id)
        {

            var userId = HttpContext.Session.GetInt32("UserId");

            ViewBag.CurrentUserId = userId;

            var eventItem = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.CreatedByNavigation)
                .Include(e => e.EventComments)
                    .ThenInclude(c => c.User) 
                                              
                .Include(e => e.EventComments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(e => e.EventId == id && e.IsActive);

            if (eventItem == null)
                return NotFound();

            if (slug != eventItem.Slug)
                return RedirectToActionPermanent("Details", new { slug = eventItem.Slug, id = id });

            bool isRegistered = false;
            if (userId != null)
            {
                isRegistered = await _context.EventRegistrations
                    .AnyAsync(x => x.EventId == id && x.UserId == userId);
            }

            ViewBag.IsRegistered = isRegistered;

            bool isRegistrationExpired = eventItem.RegistrationDeadline.HasValue &&
                                         DateTime.Now.Date > eventItem.RegistrationDeadline.Value.Date;

            ViewBag.IsRegistrationExpired = isRegistrationExpired;

            ViewBag.RelatedEvent = await _context.Events
                .Where(e => e.IsActive && e.EventId != id && e.CategoryId == eventItem.CategoryId)
                .OrderByDescending(e => e.CreatedDate)
                .Take(4)
                .ToListAsync();

            return View(eventItem);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterEvent(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "⚠ Bạn cần đăng nhập để tham gia sự kiện.";
                return RedirectToAction("Index", "Login");
            }

            var existed = await _context.EventRegistrations
                .AnyAsync(x => x.EventId == eventId && x.UserId == userId);

            if (existed)
            {
                TempData["WarningMessage"] = "Bạn đã đăng ký sự kiện này rồi!";
                var ev = await _context.Events.FindAsync(eventId);
                return RedirectToAction("Details", "Events", new { slug = ev?.Slug, id = eventId });
            }

            var join = new EventRegistration
            {
                EventId = eventId,
                UserId = userId.Value,
                RegistrationDate = DateTime.Now,
                Status = "Đã Đăng Ký"
            };

            _context.EventRegistrations.Add(join);
            await _context.SaveChangesAsync();

            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem != null)
            {
                eventItem.CurrentParticipants = (eventItem.CurrentParticipants ?? 0) + 1;
                _context.Events.Update(eventItem);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    try
                    {
                        var body = $@"
<div style='background-color:#f0f2f5; padding:30px 10px; font-family:""Segoe UI"",Arial,sans-serif;'>
    <div style='max-width:600px; margin:0 auto; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 10px 25px rgba(0,0,0,0.05);'>
        
        <div style='background: linear-gradient(135deg, #0056b3 0%, #00a8ff 100%); padding:40px 20px; text-align:center; color:#ffffff;'>
            <div style='font-size:50px; margin-bottom:10px;'>🎟️</div>
            <h1 style='margin:0; font-size:24px; text-transform:uppercase; letter-spacing:2px;'>Xác Nhận Đăng Ký</h1>
            <p style='opacity:0.9; margin-top:5px;'>Cảm ơn bạn đã quan tâm đến sự kiện của chúng tôi!</p>
        </div>

        <div style='padding:30px;'>
            <p style='font-size:16px; color:#4b5563;'>Chào <strong>{user.FullName}</strong>,</p>
            <p style='font-size:16px; color:#4b5563; line-height:1.6;'>Hệ thống đã ghi nhận yêu cầu tham gia sự kiện của bạn. Dưới đây là thông tin chi tiết tấm vé của bạn:</p>

            <div style='margin:25px 0; border:2px dashed #e5e7eb; border-radius:12px; padding:20px; background-color:#fffaf0;'>
                <h2 style='margin:0 0 15px 0; color:#1e40af; font-size:20px;'>✨ {eventItem.Title}</h2>
                
                <table style='width:100%; border-collapse:collapse; font-size:15px; color:#374151;'>
                    <tr>
                        <td style='padding:8px 0; width:100px;'><strong>📅 Bắt đầu:</strong></td>
                        <td style='padding:8px 0;'>{eventItem.StartTime:HH:mm - dd/MM/yyyy}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0;'><strong>📍 Địa điểm:</strong></td>
                        <td style='padding:8px 0;'>{eventItem.Location}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0;'><strong>📌 Trạng thái:</strong></td>
                        <td style='padding:8px 0;'><span style='background-color:#dcfce7; color:#166534; padding:4px 12px; border-radius:20px; font-size:12px; font-weight:bold;'>ĐÃ ĐĂNG KÝ</span></td>
                    </tr>
                </table>
            </div>

            <div style='text-align:center; margin-top:30px;'>
                <a href='https://yourdomain.com/Events/Details/{eventItem.EventId}' 
                   style='background-color:#1e40af; color:#ffffff; padding:14px 30px; text-decoration:none; border-radius:8px; font-weight:bold; display:inline-block; transition: background 0.3s;'>
                   XEM CHI TIẾT SỰ KIỆN
                </a>
            </div>

            <div style='margin-top:30px; padding-top:20px; border-top:1px solid #eee; color:#6b7280; font-size:14px;'>
                <p style='margin:0;'><strong>Lưu ý từ BTC:</strong></p>
                <ul style='padding-left:20px; margin:10px 0;'>
                    <li>Vui lòng có mặt trước 15 phút để check-in.</li>
                    <li>Mang theo thẻ sinh viên hoặc email này để xác nhận.</li>
                </ul>
            </div>
        </div>

        <div style='background-color:#f9fafb; padding:20px; text-align:center; color:#9ca3af; font-size:12px;'>
            <p>© {DateTime.Now.Year} Vinhuni Event Team - Kết nối đam mê</p>
            <p>182 Lê Duẩn, TP. Vinh, Nghệ An</p>
        </div>
    </div>
</div>";
                        await _emailService.SendEmailAsync(user.Email, "[Vinhuni Event] Đăng ký sự kiện thành công", body);
                    }
                    catch
                    {
                        // Bỏ qua lỗi gửi email để không ảnh hưởng luồng đăng ký
                    }
                }
            }
            return RedirectToAction("Details", "Events", new { slug = eventItem?.Slug, id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int eventId, string content, int? parentId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "⚠ Bạn cần đăng nhập để bình luận sự kiện.";
                return RedirectToAction("Index", "Login");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Details", "Events", new { slug = (await _context.Events.FindAsync(eventId))?.Slug, id = eventId });
            }

            var comment = new EventComment
            {
                EventId = eventId,
                UserId = userId.Value,
                Content = content,
                ParentCommentId = parentId,
                CreatedAt = DateTime.Now // Đảm bảo model có trường này hoặc tự sinh
            };

            _context.EventComments.Add(comment);
            await _context.SaveChangesAsync();

            var ev = await _context.Events.FindAsync(eventId);
            return RedirectToAction("Details", "Events", new { slug = ev?.Slug, id = eventId });
        }
    }
}