using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Models;

namespace VinhuniEvent.Controllers
{
    public class CertificatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CertificatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Certificates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var certificate = await _context.Certificates
                .Include(c => c.Event)
                    .ThenInclude(e => e.Category)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CertificateId == id);

            if (certificate == null)
                return NotFound();

            // Chỉ cho phép xem chứng nhận của chính mình hoặc admin
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (certificate.UserId != userId && roleId != 1)
                return Forbid();

            return View(certificate);
        }
    }
}
