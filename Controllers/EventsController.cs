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
            var eventsQuery = _context.Events
                .Where(e => e.IsActive && (!categoryid.HasValue || e.CategoryId == categoryid))
                .Include(e => e.Category)
                .OrderByDescending(e => e.CreatedDate);

            ViewBag.Category = await _context.EventCategories.ToListAsync();
            ViewBag.EventNew = await eventsQuery.Take(4).ToListAsync();
            ViewBag.EventHightLight = await eventsQuery.Where(p => p.Status == "hot").Take(4).ToListAsync();

            var events = await eventsQuery.ToListAsync();
            return View(events);
        }

        [HttpGet("{slug}-{id}.html")]
        public async Task<IActionResult> Details(string slug, int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.CreatedByNavigation)
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

    }
}
