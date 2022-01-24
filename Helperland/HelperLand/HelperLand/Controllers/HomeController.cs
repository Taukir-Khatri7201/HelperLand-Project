using Microsoft.AspNetCore.Mvc;

namespace HelperLand.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult faq()
        {
            return View();
        }

        public IActionResult prices()
        {
            return View();
        }
    
        public IActionResult contact()
        {
            return View();
        }

        public IActionResult about()
        {
            return View();
        }
    }
}
