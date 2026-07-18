using Microsoft.AspNetCore.Mvc;

namespace RateAiArt.Controllers
{
    public class RateAiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
