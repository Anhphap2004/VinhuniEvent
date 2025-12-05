using Microsoft.AspNetCore.Mvc;
using VinhuniEvent.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
namespace VinhuniEvent.Areas.Admin.Controllers
{                    
    [Area("Admin")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var students = await _context.Users.Include(u => u.Role).Where(p => p.RoleId == 2).ToListAsync();
            return View(students);
        }
        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Users
                .Include(u => u.EventRegistrations)
                    .ThenInclude(er => er.Event)
                .Include(u => u.Attendances)
                    .ThenInclude(a => a.Event)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (student == null) return NotFound();

            return View(student);
        }

    }
}
