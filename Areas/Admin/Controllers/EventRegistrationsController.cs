using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VinhuniEvent.Filters;
using VinhuniEvent.Models;
using VinhuniEvent.ViewModels;
using VinhuniEvent.Services;

namespace VinhuniEvent.Areas.Admin.Controllers
{
    [RoleAuthorize(1, 3)]
    [Area("Admin")]
    public class EventRegistrationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public EventRegistrationsController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Hiển thị màn hình quét QR
        [HttpGet]
        public async Task<IActionResult> ScanQRCode(int id) 
        {
            // 1. Kiểm tra quyền Admin qua Session
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == null)
            {
                return RedirectToAction("Index", "Login", new { area = "" });
            }

            // 2. Tìm sự kiện
            var eventInfo = await _context.Events.FindAsync(id); 
            if (eventInfo == null) return NotFound();

            ViewBag.EventId = id; 
            ViewBag.EventName = eventInfo.Title;
            return View();
        }
        // POST: Xử lý điểm danh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessAttendance([FromForm] int eventId, [FromForm] int userId)
        {
            var adminRoleId = HttpContext.Session.GetInt32("RoleId");
            if (adminRoleId == null)
            {
                return Json(new { success = false, message = "⛔ Bạn không có quyền thực hiện!" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return Json(new { success = false, message = "❌ Không tìm thấy sinh viên!" });
            }


            var isRegistered = await _context.EventRegistrations
                .AnyAsync(r => r.EventId == eventId && r.UserId == userId);

            if (!isRegistered)
            {
                return Json(new { success = false, message = $"⚠️ {user.FullName} chưa đăng ký sự kiện này!" });
            }

            try
            {
                
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        EventId = eventId,
                        UserId = userId,
                        AttendanceTime = DateTime.Now,
                        IsPresent = true
                    };
                    _context.Attendances.Add(attendance);
                }
                else
                {
                    if (attendance.IsPresent == true)
                    {
                        return Json(new { success = false, message = $"ℹ️ {user.FullName} đã điểm danh rồi." });
                    }
                    attendance.IsPresent = true;
                    attendance.AttendanceTime = DateTime.Now;
                    _context.Attendances.Update(attendance);
                }

                await _context.SaveChangesAsync();

         
                return Json(new
                {
                    success = true,
                    message = "✅ Điểm danh thành công!",
                    studentName = user.FullName,
                    studentCode = user.StudentCode,
        
                    image = !string.IsNullOrEmpty(user.ImageUrl) ? $"/images/users/{user.ImageUrl}" : "https://via.placeholder.com/150"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }


        // GET: Admin/EventRegistrations
        public async Task<IActionResult> Index()
        {
            var eventRegistrations = await _context.EventRegistrations
                .Include(e => e.Event)
                .Include(e => e.User)
                .ToListAsync();

            return View(eventRegistrations);
        }

        // GET: Admin/EventRegistrations/ByEvent/5
        public async Task<IActionResult> ByEvent(int? id, string attendanceFilter)
        {
            if (id == null)
                return NotFound();

            var eventInfo = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventInfo == null)
                return NotFound();

            var registrations = await _context.EventRegistrations
                .Include(er => er.User)
                .Include(er => er.Event)
                    .ThenInclude(e => e.Attendances)
                .Where(er => er.EventId == id)
                .ToListAsync();

            // 🌸✨ LỌC TRẠNG THÁI ĐIỂM DANH ✨🌸
            if (!string.IsNullOrEmpty(attendanceFilter))
            {
                registrations = attendanceFilter switch
                {
                    // Đã điểm danh
                    "present" => registrations
                        .Where(r => r.Event.Attendances
                            .Any(a => a.UserId == r.UserId && a.IsPresent == true))
                        .ToList(),

                    // Vắng mặt
                    "absent" => registrations
                        .Where(r => r.Event.Attendances
                            .Any(a => a.UserId == r.UserId && a.IsPresent == false))
                        .ToList(),

                    // Chưa điểm danh
                    "none" => registrations
                        .Where(r => !r.Event.Attendances
                            .Any(a => a.UserId == r.UserId))
                        .ToList(),

                    _ => registrations
                };
            }

            ViewBag.Count = registrations.Count;
            ViewBag.EventTitle = eventInfo.Title;
            ViewBag.EventDate = eventInfo.CreatedDate?.ToString("dd/MM/yyyy");
            ViewBag.EventId = id;
            ViewBag.AttendanceFilter = attendanceFilter; 

            return View(registrations);
        }


        // GET: Admin/EventRegistrations/Attendance?eventId=5
        public async Task<IActionResult> Attendance(int eventId)
        {
            var eventInfo = await _context.Events.FindAsync(eventId);
            if (eventInfo == null) return NotFound();

            var registrations = await _context.EventRegistrations
                .Include(r => r.User)
                .Where(r => r.EventId == eventId)
                .ToListAsync();

            var attendances = await _context.Attendances
                .Where(a => a.EventId == eventId)
                .ToListAsync();

            var viewModel = registrations.Select(r => {
                var attendanceRecord = attendances.FirstOrDefault(a => a.UserId == r.UserId);
                return new AttendanceViewModel
                {
                    UserId = r.UserId,
                    FullName = r.User?.FullName,
                    StudentCode = r.User?.StudentCode,
                    IsPresent = attendanceRecord?.IsPresent,
                    CheckInImage = attendanceRecord?.CheckInImage
                };
            }).ToList();

            ViewBag.EventId = eventId;
            ViewBag.EventTitle = eventInfo.Title;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAttendance([FromForm] int eventId, [FromForm] int userId, [FromForm] bool isPresent)
        {
            try
            {
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        EventId = eventId,
                        UserId = userId,
                        AttendanceTime = DateTime.Now,
                        IsPresent = isPresent
                    };
                    _context.Attendances.Add(attendance);
                }
                else
                {
                    attendance.IsPresent = isPresent;
                    attendance.AttendanceTime = DateTime.Now;
                    _context.Attendances.Update(attendance);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật điểm danh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/EventRegistrations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventRegistration = await _context.EventRegistrations
                .Include(e => e.Event)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.RegistrationId == id);

            if (eventRegistration == null)
            {
                return NotFound();
            }

            return View(eventRegistration);
        }

        // POST: Admin/EventRegistrations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventRegistration = await _context.EventRegistrations.FindAsync(id);

            if (eventRegistration != null)
            {
                var eventItem = await _context.Events.FindAsync(eventRegistration.EventId);

                // ✅ Xóa cả bản ghi điểm danh nếu có
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EventId == eventRegistration.EventId
                                           && a.UserId == eventRegistration.UserId);
                if (attendance != null)
                {
                    _context.Attendances.Remove(attendance);
                }

                _context.EventRegistrations.Remove(eventRegistration);

                if (eventItem != null && (eventItem.CurrentParticipants ?? 0) > 0)
                {
                    eventItem.CurrentParticipants--;
                    _context.Events.Update(eventItem);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa đăng ký thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy dữ liệu đăng ký cần xóa!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/EventRegistrations/IssueCertificates/5
        public async Task<IActionResult> IssueCertificates(int id)
        {
            var eventInfo = await _context.Events.FindAsync(id);
            if (eventInfo == null)
                return NotFound();

            var registrations = await _context.EventRegistrations
                .Include(r => r.User)
                .Where(r => r.EventId == id)
                .ToListAsync();

            var attendances = await _context.Attendances
                .Where(a => a.EventId == id && a.IsPresent == true)
                .ToListAsync();

            var existingCertificates = await _context.Certificates
                .Where(c => c.EventId == id)
                .Select(c => c.UserId)
                .ToListAsync();

            var viewModel = new CertificateIssueViewModel
            {
                EventId = id,
                EventTitle = eventInfo.Title,
                EventDate = eventInfo.StartTime,
                EventLocation = eventInfo.Location,
                Items = registrations.Select(r => new CertificateIssueItem
                {
                    UserId = r.UserId,
                    FullName = r.User?.FullName ?? "",
                    StudentCode = r.User?.StudentCode,
                    Faculty = r.User?.Faculty,
                    IsPresent = attendances.Any(a => a.UserId == r.UserId),
                    AlreadyIssued = existingCertificates.Contains(r.UserId)
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Admin/EventRegistrations/IssueCertificatesConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueCertificatesConfirmed(int eventId)
        {
            var eventInfo = await _context.Events.FindAsync(eventId);
            if (eventInfo == null)
                return NotFound();

            // Lấy danh sách người đã điểm danh và chưa được cấp chứng nhận
            var presentAttendances = await _context.Attendances
                .Where(a => a.EventId == eventId && a.IsPresent == true)
                .Select(a => a.UserId)
                .ToListAsync();

            var existingCertificates = await _context.Certificates
                .Where(c => c.EventId == eventId)
                .Select(c => c.UserId)
                .ToListAsync();

            var usersToIssue = presentAttendances
                .Where(userId => !existingCertificates.Contains(userId))
                .ToList();

            if (usersToIssue.Count == 0)
            {
                TempData["InfoMessage"] = "Không có sinh viên nào cần cấp giấy chứng nhận mới.";
                return RedirectToAction("IssueCertificates", new { id = eventId });
            }

            var createdCertificates = new List<Certificate>();

            // Tạo giấy chứng nhận
            foreach (var userId in usersToIssue)
            {
                var certificate = new Certificate
                {
                    EventId = eventId,
                    UserId = userId,
                    CertificateCode = $"VNU-{eventId:D4}-{userId:D5}-{DateTime.Now:yyyyMMdd}",
                    IssuedAt = DateTime.Now,
                    Status = "Issued"
                };
                createdCertificates.Add(certificate);
                _context.Certificates.Add(certificate);
            }

            await _context.SaveChangesAsync();

            var issuedUsers = await _context.Users
                .Where(u => usersToIssue.Contains(u.UserId))
                .ToListAsync();

            foreach (var user in issuedUsers)
            {
                var certificate = createdCertificates.FirstOrDefault(c => c.UserId == user.UserId);
                if (certificate == null)
                {
                    continue;
                }

                try
                {
                    var body = $@"
<div style='background-color:#f8fafc; padding:50px 10px; font-family:""Segoe UI"",Tahoma,Geneva,Verdana,sans-serif;'>
    <div style='max-width:650px; margin:0 auto; background-color:#ffffff; border-radius:20px; overflow:hidden; box-shadow:0 15px 35px rgba(0,0,0,0.1); border: 1px solid #e2e8f0;'>
        
        <div style='background: linear-gradient(135deg, #1e3a8a 0%, #3b82f6 100%); padding:40px; text-align:center; color:#ffffff;'>
            <div style='font-size:60px; margin-bottom:10px;'>🏆</div>
            <h1 style='margin:0; font-size:26px; text-transform:uppercase; letter-spacing:3px; font-weight:800;'>Giấy Chứng Nhận</h1>
            <p style='opacity:0.9; font-style:italic; margin-top:5px;'>Vinhuni Event vinh danh nỗ lực của bạn</p>
        </div>

        <div style='padding:40px; text-align:center;'>
            <p style='font-size:18px; color:#64748b; margin-bottom:5px;'>Chứng nhận này được trao cho</p>
            <h2 style='font-size:28px; color:#1e293b; margin:0 0 20px 0; font-family:""Georgia"", serif;'>{user.FullName}</h2>
            
            <p style='font-size:16px; color:#475569; line-height:1.7; max-width:500px; margin:0 auto;'>
                Đã hoàn thành xuất sắc các nội dung và yêu cầu trong sự kiện: <br/>
                <strong style='color:#1e3a8a; font-size:20px;'>{eventInfo.Title}</strong>
            </p>

            <div style='margin:35px auto; width: fit-content; background-color:#fffbeb; border:1px solid #fde68a; border-radius:12px; padding:20px 40px; text-align:left;'>
                <table style='border-collapse:collapse; font-size:14px; color:#92400e;'>
                    <tr>
                        <td style='padding:5px 10px;'><strong>🆔 Mã số:</strong></td>
                        <td style='padding:5px 10px;'>{certificate.CertificateCode}</td>
                    </tr>
                    <tr>
                        <td style='padding:5px 10px;'><strong>📅 Ngày cấp:</strong></td>
                        <td style='padding:5px 10px;'>{DateTime.Now:dd/MM/yyyy}</td>
                    </tr>
                    <tr>
                        <td style='padding:5px 10px;'><strong>📍 Địa điểm:</strong></td>
                        <td style='padding:5px 10px;'>{eventInfo.Location}</td>
                    </tr>
                </table>
            </div>

            <div style='margin-top:40px;'>
                <a href='https://yourdomain.com/Certificates/Download/{certificate.CertificateCode}' 
                   style='background-color:#1e3a8a; color:#ffffff; padding:16px 40px; text-decoration:none; border-radius:50px; font-weight:bold; font-size:16px; display:inline-block; box-shadow:0 4px 12px rgba(30,58,138,0.3);'>
                   📥 TẢI XUỐNG CHỨNG NHẬN (PDF)
                </a>
            </div>
        </div>

        <div style='background-color:#f1f5f9; padding:25px; text-align:center; color:#64748b; font-size:13px;'>
            <p style='margin:0;'>Chứng nhận này được cấp bởi hệ thống <strong>Vinhuni Event</strong>.</p>
            <p style='margin:5px 0;'>Mã chứng nhận có giá trị đối chiếu trên hệ thống quản lý sinh viên của Trường Đại học Vinh.</p>
        </div>
    </div>
</div>";
                    await _emailService.SendEmailAsync(user.Email, "[Vinhuni Event] Giấy chứng nhận đã được cấp", body);
                }
                catch
                {
                    // Bỏ qua lỗi gửi email để không ảnh hưởng kết quả cấp chứng nhận
                }
            }

            TempData["SuccessMessage"] = $"Đã cấp {usersToIssue.Count} giấy chứng nhận thành công!";
            return RedirectToAction("IssueCertificates", new { id = eventId });
        }
    }

    public class AttendanceViewModel
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? StudentCode { get; set; }
        public bool? IsPresent { get; set; }

        public string? CheckInImage { get; set; }
    }
    public class AttendanceRequest
    {
        public int EventId { get; set; }
        public int UserId { get; set; }
        public bool IsPresent { get; set; }
    }

}