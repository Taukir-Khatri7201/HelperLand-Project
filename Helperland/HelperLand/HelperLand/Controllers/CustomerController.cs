using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelperLand.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult ServiceRequests()
        {
            return View();
        }
    }
}
