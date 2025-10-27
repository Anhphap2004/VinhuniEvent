using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
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
        public async Task<IActionResult> Index(int? categoryid)
        {
            var eventsQuery = _context.Events
       .Where(e => e.IsActive && (!categoryid.HasValue || e.CategoryId == categoryid))
       .Include(e => e.Category)
       .OrderByDescending(e => e.CreatedDate);
            ViewBag.category = await _context.EventCategories.ToListAsync();
            ViewBag.EventNew = await eventsQuery.Take(4).ToListAsync();
            var events = await eventsQuery.ToListAsync();
            return View(events);
        }

        [HttpGet("{slug}-{id}.html")]
        public async Task<IActionResult> Details(string slug, int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.CreatedByNavigation)
                .FirstOrDefaultAsync(e => e.EventId == id && e.IsActive);

            if (eventItem == null)
                return NotFound();

            if (slug != eventItem.Slug)
                return RedirectToActionPermanent("Details", new { slug = eventItem.Slug, id = id });
            ViewBag.RelatedEvent = await _context.Events
                .Where(e => e.IsActive && e.EventId != id && e.CategoryId == eventItem.CategoryId)
                .OrderByDescending(e => e.CreatedDate)
                .Take(4)
                .ToListAsync();
            return View(eventItem);
        }

    }
}
