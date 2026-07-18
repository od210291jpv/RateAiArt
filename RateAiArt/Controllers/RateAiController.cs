using Microsoft.AspNetCore.Mvc;

using RateAiArt.Data;
using RateAiArt.DTO.Ai;
using RateAiArt.Services;

using Swashbuckle.AspNetCore.Annotations;

namespace RateAiArt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateAiController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly IAiEvaluationService _evaluationService;

        //private readonly Kernel _kernel;

        public RateAiController(ApplicationContext context, IAiEvaluationService evaluationService)
        {
            _context = context;
            _evaluationService = evaluationService;
        }

        [SwaggerIgnore]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("rateAiArt")]
        public async Task<IActionResult> RateAiArt([FromBody] PromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Base64Image)) 
            {
                return BadRequest("No image provided");
            }

            byte[] imageBytes = Convert.FromBase64String(request.Base64Image);
            EvaluationResponse result = await this._evaluationService.EvaluateArtAsync(imageBytes, request.MimeType);

            return Ok(result);
        }

        [HttpPost("getLeaderBoard")]
        public IActionResult GetLeaderBoard()
        {
            // Dummy endpoint for POST /getLeaderBoard
            return Ok();
        }
    }
}
