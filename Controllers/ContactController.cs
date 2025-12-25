using Microsoft.AspNetCore.Mvc;
using VinhuniEvent.Models;

namespace VinhuniEvent.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 📌 GET: Contact
        public IActionResult Index()
        {
            return View(new Contact());
        }

        // 📌 POST: Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contact contact)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (ModelState.IsValid)
            {
                if (userId == null)
                {
                    TempData["error"] = "Vui lòng đăng nhập trước khi gửi liên hệ 🌿";
                    return RedirectToAction("Index", "Login");
                }
                          contact.UserId = userId.Value;
                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                TempData["success"] = "Gửi liên hệ thành công 💌";
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi → quay lại form
            return View("Index", contact);
        }
    }
}
