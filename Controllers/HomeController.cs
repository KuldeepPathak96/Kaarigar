using Kaarigar.Models;
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

        // Target of app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}") in
        // Program.cs. Handles non-exception error responses (400 Bad Request from
        // an expired antiforgery token, 403, plain 404s, etc.) with an actionable
        // message instead of the browser's bare error screen.
        [Route("/Home/StatusCode/{code:int}")]
        public IActionResult StatusCode(int code)
        {
            ViewBag.StatusCode = code;
            ViewBag.Message = code switch
            {
                400 => "Your session on this page expired (this can happen if the page was left open a long time, or the site was recently updated). Please go back, refresh the page, and try again.",
                403 => "You don't have permission to do that.",
                404 => "That page couldn't be found.",
                _ => "Something went wrong loading that page.",
            };
            return View();
        }

        // Target of app.UseExceptionHandler("/Home/Error") in Program.cs. Without this
        // action, any unhandled exception in Production was itself 404'ing (no route
        // matched "/Home/Error"), which is why profile-update crashes appeared to
        // silently "go to a not found page" instead of showing any error at all.
        [Route("/Home/Error")]
        public IActionResult Error()
        {
            var requestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
