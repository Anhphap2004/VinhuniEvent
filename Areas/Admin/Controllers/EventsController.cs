using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VinhuniEvent.Models;

namespace VinhuniEvent.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Events
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Events.Include(u => u.Category).Include(u => u.CreatedByNavigation);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Admin/Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(u => u.Category)
                .Include(u => u.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.EventId == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // GET: Admin/Events/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.EventCategories, "CategoryId", "CategoryName");
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName");
            return View();
        }

        // POST: Admin/Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,CategoryId,Title,Description,Location,StartTime,EndTime,CreatedBy,MaxParticipants,Image,IsActive,CreatedDate,ImageFile")] Event @event)
        {
            if (ModelState.IsValid)
            {
                @event.Slug = GenerateSlug(@event.Title);
                if (@event.ImageFile != null && @event.ImageFile.Length > 0)
                {
                    // Tạo đường dẫn chuẩn
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "main", "img", "events");

                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    // Tạo tên file duy nhất
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(@event.ImageFile.FileName);

                    // Đường dẫn đầy đủ để lưu file
                    var filePath = Path.Combine(uploadDir, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await @event.ImageFile.CopyToAsync(fileStream);
                    }

                    // Gán tên file vào cột Image
                    @event.Image = uniqueFileName;
                }

                @event.CreatedDate = DateTime.Now;
                @event.IsActive = true;
                _context.Add(@event);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.EventCategories, "CategoryId", "CategoryName", @event.CategoryId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName", @event.CreatedBy);
            return View(@event);
        }
        private string GenerateSlug(string title)
        {
            string str = title.ToLowerInvariant();

            // Bỏ dấu tiếng Việt
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\p{IsCombiningDiacriticalMarks}+", string.Empty)
                .Normalize(NormalizationForm.FormD);

            // Chuyển ký tự đặc biệt thành dấu -
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\s-]", "");

            // Thay khoảng trắng bằng dấu -
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", "-").Trim('-');

            // Cắt bớt nếu quá dài
            return str.Length <= 100 ? str : str.Substring(0, 100);
        } 

        // GET: Admin/Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.EventCategories, "CategoryId", "CategoryName", @event.CategoryId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName", @event.CreatedBy);
            return View(@event);
        }

        // POST: Admin/Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
       [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, Event @event)
{
    if (id != @event.EventId)
        return NotFound();

    if (ModelState.IsValid)
    {
        try
        {
                    // Nếu slug rỗng => tạo lại từ Title
                    if (string.IsNullOrEmpty(@event.Slug))
                    {
                        @event.Slug = GenerateSlug(@event.Title);
                    }
                    // 🔹 Lấy dữ liệu cũ từ DB để giữ ảnh cũ
                    var existingEvent = await _context.Events.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (existingEvent == null)
                return NotFound();

            // 🔹 Nếu không upload ảnh mới → giữ ảnh cũ
            if (@event.ImageFile == null)
            {
                @event.Image = existingEvent.Image;
            }
            else
            {
                // Có ảnh mới thì upload
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/main/img/events");
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(@event.ImageFile.FileName);
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await @event.ImageFile.CopyToAsync(fileStream);
                }

                // (Tuỳ chọn) xoá ảnh cũ khỏi thư mục nếu tồn tại
                if (!string.IsNullOrEmpty(existingEvent.Image))
                {
                    var oldFilePath = Path.Combine(uploadDir, existingEvent.Image);
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                @event.Image = uniqueFileName;
            }

            _context.Update(@event);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EventExists(@event.EventId))
                return NotFound();
            else
                throw;
        }
    }

    ViewData["CategoryId"] = new SelectList(_context.EventCategories, "CategoryId", "CategoryName", @event.CategoryId);
    ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName", @event.CreatedBy);
    return View(@event);
}



        // GET: Admin/Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(u => u.Category)
                .Include(u => u.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.EventId == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: Admin/Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
    }
}
