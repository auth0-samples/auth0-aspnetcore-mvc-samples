using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SampleMvcApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "admin")]
        public IActionResult Admin()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
