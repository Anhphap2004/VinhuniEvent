using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
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
             .Include(u => u.Certificates)
                 .ThenInclude(c => c.Event)
             .FirstOrDefault(u => u.UserId == userId);

            return View(user);
        }
        public async Task<IActionResult> MyQRCode()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Index", "Login", new { area = "" });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            string qrContent = user.UserId.ToString();

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                string base64Image = Convert.ToBase64String(qrCodeImage);
                ViewBag.QrCodeImage = "data:image/png;base64," + base64Image;
            }

            return View(user);
        }
    }
}
