using Microsoft.AspNetCore.Mvc;
using VinhuniEvent.Filters;
namespace VinhuniEvent.Areas.Admin.Controllers
{
    [RoleAuthorize(1,3)]
    [Area("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
