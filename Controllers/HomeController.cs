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

        [HttpGet("/privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("/about")]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet("/contact")]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpGet("/careers")]
        public IActionResult Careers()
        {
            return View();
        }

        [HttpGet("/faqs")]
        public IActionResult Faqs()
        {
            return View();
        }

        [HttpGet("/cancellation-refund")]
        public IActionResult CancellationRefund()
        {
            return View();
        }
    }
}
