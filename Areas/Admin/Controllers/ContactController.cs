using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Models;

namespace Vinhuni.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 📌 READ: Danh sách
        public IActionResult Index()
        {
            var contacts = _context.Contacts
                                   .Include(c => c.User)
                                   .OrderByDescending(c => c.CreatedAt)
                                   .ToList();
            return View(contacts);
        }

        // 📌 READ: Chi tiết
        public IActionResult Details(int id)
        {
            var contact = _context.Contacts
                                  .Include(c => c.User)
                                  .FirstOrDefault(c => c.ContactId == id);

            if (contact == null) return NotFound();

            // đánh dấu đã đọc
            contact.IsRead = true;
            _context.SaveChanges();

            return View(contact);
        }

    

        // 📌 DELETE: GET
        public IActionResult Delete(int id)
        {
            var contact = _context.Contacts
                                  .Include(c => c.User)
                                  .FirstOrDefault(c => c.ContactId == id);

            if (contact == null) return NotFound();
            return View(contact);
        }

        // 📌 DELETE: POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var contact = _context.Contacts.Find(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
