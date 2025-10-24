using Microsoft.AspNetCore.Mvc;

namespace VinhuniEvent.Controllers
{
    public class EventsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
