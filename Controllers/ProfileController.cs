using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Models;
namespace VinhuniEvent.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }
            var user = _context.Users
             .Include(u => u.EventRegistrations)
                 .ThenInclude(er => er.Event)
             .Include(u => u.Attendances)
                 .ThenInclude(a => a.Event)
             .FirstOrDefault(u => u.UserId == userId);

            return View(user);
        }
    }
}
