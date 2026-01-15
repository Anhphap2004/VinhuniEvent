using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> CheckInImageOnly(int eventId, IFormFile photo)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || photo == null) return Json(new { success = false });

            try
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/checkin");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var fileName = $"img_{eventId}_{userId}_{DateTime.Now.Ticks}.jpg";
                using (var stream = new FileStream(Path.Combine(folderPath, fileName), FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId);
                if (attendance != null)
                {
                    attendance.CheckInImage = "/uploads/checkin/" + fileName;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy đăng ký" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
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

            // Nội dung QR là UserId để cán bộ có thể quét kiểm tra nhanh
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