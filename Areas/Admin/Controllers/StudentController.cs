using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhuniEvent.Models;

namespace VinhuniEvent.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context) {
        
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var students = await _context.Users.Include(u => u.Role).Where(u => u.RoleId == 2).ToListAsync();
            return View(students);
        }
    }
}
