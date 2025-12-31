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

namespace VinhuniEvent.Areas.Admin.Controllers
{
    [RoleAuthorize(1, 3)]
    [Area("Admin")]
    public class EventRegistrationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventRegistrationsController(ApplicationDbContext context)
        {
            _context = context;
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
            ViewBag.AttendanceFilter = attendanceFilter; // 👈 Lưu lại để View hiển thị đúng

            return View(registrations);
        }


        // GET: Admin/EventRegistrations/Attendance?eventId=5
        public async Task<IActionResult> Attendance(int eventId)
        {
            var eventInfo = await _context.Events.FindAsync(eventId);

            if (eventInfo == null)
            {
                return NotFound();
            }

            var registrations = await _context.EventRegistrations
                .Include(r => r.User)
                .Where(r => r.EventId == eventId)
                .ToListAsync();

            var attendances = await _context.Attendances
                .Where(a => a.EventId == eventId)
                .ToListAsync();

            var viewModel = registrations.Select(r => new AttendanceViewModel
            {
                UserId = r.UserId,
                FullName = r.User?.FullName,
                StudentCode = r.User?.StudentCode,
                IsPresent = attendances.FirstOrDefault(a => a.UserId == r.UserId)?.IsPresent
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
                _context.Certificates.Add(certificate);
            }

            await _context.SaveChangesAsync();

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
    }
    public class AttendanceRequest
    {
        public int EventId { get; set; }
        public int UserId { get; set; }
        public bool IsPresent { get; set; }
    }

}