using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Models;

namespace VinhuniEvent.ViewComponents
{
    public class EventTabsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public EventTabsViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.EventCategories
                .Where(c => c.IsActive == true)
                .Include(c => c.Events.Where(e => e.IsActive == true))
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View(categories);
        }
    }
}
