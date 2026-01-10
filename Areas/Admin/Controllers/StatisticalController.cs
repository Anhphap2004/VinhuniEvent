using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Filters;
using VinhuniEvent.Models;
using VinhuniEvent.ViewModels;
namespace VinhuniEvent.Areas.Admin.Controllers
{
    [RoleAuthorize(1)]
    [Area("Admin")]
    public class StatisticalController : Controller
    {
        private readonly ApplicationDbContext _context;
        public StatisticalController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var now = DateTime.Now;
            var users = _context.Users.ToList();

            var topEvents = _context.Events
    .Select(e => new TopEventDto
    {
        EventName = e.Title,
        RegistrationCount = _context.EventRegistrations.Count(r => r.EventId == e.EventId),
        StartTime = e.StartTime
    })
    .OrderByDescending(x => x.RegistrationCount)
    .ThenByDescending(x => x.StartTime)
    .Take(5)
    .ToList();

            var latestEvents = _context.Events
                .OrderByDescending(e => e.StartTime)
                .Take(5)
                .ToList();

            var totalCheckin = _context.Attendances.Count(a => a.IsPresent == true);
            var totalabsent = _context.Attendances.Count(a => a.IsPresent == false);
            var vm = new ThongKeViewModel
            {
                TotalEvents = _context.Events.Count(),
                TotalRegistrations = _context.EventRegistrations.Count(),
                TotalUsers = users.Count,
                TotalAdmins = users.Count(u => u.RoleId == 1),
                TotalStudents = users.Count(u => u.RoleId == 2),
                TotalOrganizers = users.Count(u => u.RoleId == 3),
                UpcomingEvents = _context.Events.Count(e => e.StartTime > now),
                OngoingEvents = _context.Events.Count(e => e.StartTime <= now && e.EndTime >= now),

                TopEvents = topEvents,
                LatestEvents = latestEvents,
                TotalCheckIn = totalCheckin,
                 TotalAbsent = totalabsent
            };

            return View(vm);
        }

    }
}
