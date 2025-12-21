using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Filters;
using VinhuniEvent.Models;

namespace VinhuniEvent.Areas.Admin.Controllers
{
    [RoleAuthorize(1, 3)]
    [Area("Admin")]
    public class RoleRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoleRequestsController(ApplicationDbContext context)
        {
            _context = context;
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
        public IActionResult Handle(int requestId, bool approve)
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
            if (approve)
            {
                if (request.User != null)
                {
                    request.User.RoleId = 3;
                }
            }

            _context.SaveChanges();

            TempData["Msg"] = approve
                ? $"Đã duyệt yêu cầu trở thành Organizer của {request.User?.FullName} ❤️"
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
