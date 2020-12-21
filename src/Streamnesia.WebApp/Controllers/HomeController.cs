using Microsoft.AspNetCore.Mvc;

namespace Streamnesia.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly StreamnesiaHub _hub;

        public HomeController(StreamnesiaHub hub)
        {
            _hub = hub;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
