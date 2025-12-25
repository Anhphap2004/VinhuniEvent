using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Models;

namespace VinhuniEvent.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
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
            // 1. Lấy UserId từ Session
            var userId = HttpContext.Session.GetInt32("UserId");

            // 2. Truyền sang View bằng ViewBag (SỬA LỖI ISession trong View)
            ViewBag.CurrentUserId = userId;

            // 3. Truy vấn Event kèm theo Comment và User (SỬA LỖI KHÔNG HIỆN COMMENT)
            var eventItem = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.CreatedByNavigation)
                // --- Load Comment ---
                .Include(e => e.EventComments)
                    .ThenInclude(c => c.User) // Lấy thông tin người bình luận
                                              // --- Load Replies (Trả lời bình luận) ---
                .Include(e => e.EventComments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User) // Lấy thông tin người trả lời
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