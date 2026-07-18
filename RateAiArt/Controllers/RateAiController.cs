using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using RateAiArt.Data;

namespace RateAiArt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateAiController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly Kernel _kernel;

        public RateAiController(ApplicationContext context, Kernel kernel)
        {
            _context = context;
            _kernel = kernel;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("rateAiArt")]
        public IActionResult RateAiArt()
        {
            // Dummy endpoint for POST /rateAiArt
            return Ok();
        }

        [HttpPost("getLeaderBoard")]
        public IActionResult GetLeaderBoard()
        {
            // Dummy endpoint for POST /getLeaderBoard
            return Ok();
        }
    }
}
