using BCrypt.Net;
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
    [RoleAuthorize(1)]
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Users
        public IActionResult Index(string? role)
        {
            var users = _context.Users
                .Include(u => u.Role)
                .AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                users = users.Where(u => u.Role.RoleName == role);
            }

            return View(users.ToList());
        }



        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        public IActionResult Create()
        {
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,StudentCode,FullName,Email,PasswordHash,RoleId,Faculty,Major,BirthDate,PhoneNumber,ImageUrl,IsActive,CreatedDate,ImageFile")] User user)
        {
            if (ModelState.IsValid)
            {
                if (user.ImageFile != null)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "main", "img", "users");
                    Directory.CreateDirectory(uploadsFolder); 

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(user.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await user.ImageFile.CopyToAsync(stream);
                    }

                    user.ImageUrl = fileName;
                }
                var existingEmail = _context.Users.FirstOrDefault(u => u.Email == user.Email);
                var existingPhone = _context.Users.FirstOrDefault(u => u.PhoneNumber == user.PhoneNumber);
                var existingStudentCode = _context.Users.FirstOrDefault(u => u.StudentCode == user.StudentCode);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "⚠ Email đã tồn tại.");
                }
                if (existingPhone != null)
                {
                    ModelState.AddModelError("PhoneNumber", "⚠ Số điện thoại đã được sử dụng.");
                }
                if (existingStudentCode != null)
                {
                    ModelState.AddModelError("StudentCode", "⚠ Mã sinh viên đã được sử dụng.");
                }
                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    ModelState.AddModelError("PasswordHash", "⚠ Mật khẩu không hợp lệ!");
                    return View(user);
                }
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                user.IsActive = true;
                user.CreatedDate = DateTime.Now;
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }


        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // POST: Admin/Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,StudentCode,FullName,Email,RoleId,Faculty,Major,BirthDate,PhoneNumber,ImageUrl,IsActive,CreatedDate,ImageFile,PasswordHash")] User user)
        {
            if (id != user.UserId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
                return View(user);
            }

            var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
            if (existingUser == null)
                return NotFound();

            try
            {
                // Xử lý ảnh
                if (user.ImageFile != null)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "main", "img", "users");
                    Directory.CreateDirectory(uploadsFolder);

                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(existingUser.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(uploadsFolder, existingUser.ImageUrl);
                        if (System.IO.File.Exists(oldImagePath))
                            System.IO.File.Delete(oldImagePath);
                    }

                    // Lưu ảnh mới
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(user.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await user.ImageFile.CopyToAsync(stream);
                    }

                    user.ImageUrl = fileName;
                }
                else
                {
                    user.ImageUrl = existingUser.ImageUrl;
                }

                // Kiểm tra trùng thông tin nhưng bỏ qua chính user hiện tại
                if (_context.Users.Any(u => u.Email == user.Email && u.UserId != id))
                {
                    ModelState.AddModelError("Email", "⚠ Email đã tồn tại.");
                    return View(user);
                }
                if (_context.Users.Any(u => u.PhoneNumber == user.PhoneNumber && u.UserId != id))
                {
                    ModelState.AddModelError("PhoneNumber", "⚠ Số điện thoại đã được sử dụng.");
                    return View(user);
                }
                if (_context.Users.Any(u => u.StudentCode == user.StudentCode && u.UserId != id))
                {
                    ModelState.AddModelError("StudentCode", "⚠ Mã sinh viên đã được sử dụng.");
                    return View(user);
                }

                // Xử lý mật khẩu
                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    // Giữ nguyên mật khẩu cũ
                    user.PasswordHash = existingUser.PasswordHash;
                }
                else
                {
                    // Hash lại mật khẩu mới
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                }

                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(user.UserId))
                    return NotFound();
                throw;
            }
        }



        // GET: Admin/Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
