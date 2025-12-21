using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VinhuniEvent.Filters;
using VinhuniEvent.Models;
            
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
        // GET: Hiển thị màn hình quét QR
        [HttpGet]
        public async Task<IActionResult> ScanQRCode(int id) // 1️⃣ Sửa 'eventId' thành 'id'
        {
            // 1. Kiểm tra quyền Admin qua Session
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == null)
            {
                return RedirectToAction("Index", "Login", new { area = "" });
            }

            // 2. Tìm sự kiện
            var eventInfo = await _context.Events.FindAsync(id); // 2️⃣ Sửa chỗ này thành 'id'
            if (eventInfo == null) return NotFound();

            ViewBag.EventId = id; // 3️⃣ Sửa chỗ này thành 'id'
            ViewBag.EventName = eventInfo.Title;
            return View();
        }
        // POST: Xử lý điểm danh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessAttendance([FromForm] int eventId, [FromForm] int userId)
        {
            // 1. Kiểm tra quyền Admin (Bảo mật API)
            var adminRoleId = HttpContext.Session.GetInt32("RoleId");
            if (adminRoleId == null)
            {
                return Json(new { success = false, message = "⛔ Bạn không có quyền thực hiện!" });
            }

            // 2. Tìm sinh viên (Dựa trên ID nhận được từ máy quét)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return Json(new { success = false, message = "❌ Không tìm thấy sinh viên!" });
            }

            // 3. Kiểm tra đăng ký
            var isRegistered = await _context.EventRegistrations
                .AnyAsync(r => r.EventId == eventId && r.UserId == userId);

            if (!isRegistered)
            {
                return Json(new { success = false, message = $"⚠️ {user.FullName} chưa đăng ký sự kiện này!" });
            }

            try
            {
                // 4. Cập nhật Attendance
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

                // 5. Trả về kết quả
                return Json(new
                {
                    success = true,
                    message = "✅ Điểm danh thành công!",
                    studentName = user.FullName,
                    studentCode = user.StudentCode,
                    // Nếu ImageUrl null thì dùng ảnh placeholder
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
    }

    // ✅ Tạo ViewModel cho Attendance
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