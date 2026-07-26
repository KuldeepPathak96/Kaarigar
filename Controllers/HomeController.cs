using Microsoft.AspNetCore.Mvc;

namespace Kaarigar.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/terms")]
        public IActionResult Terms()
        {
            return View();
        }
    }
}
