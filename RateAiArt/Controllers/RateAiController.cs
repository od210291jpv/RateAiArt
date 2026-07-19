using Microsoft.AspNetCore.Mvc;

using RateAiArt.Data;
using RateAiArt.Data.Models;
using RateAiArt.DTO.Ai;
using RateAiArt.DTO.LeaderBoard;
using RateAiArt.Services;

namespace RateAiArt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateAiController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly IAiEvaluationService _evaluationService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILeaderBoardService _leaderBoardService;

        public RateAiController(ApplicationContext context, IAiEvaluationService evaluationService, ILeaderBoardService leaderBoardService, IWebHostEnvironment environment)
        {
            _context = context;
            _evaluationService = evaluationService;
            _leaderBoardService = leaderBoardService;
            _environment = environment;
        }

        [HttpPost("rateAiArt")]
        public async Task<IActionResult> RateAiArt([FromBody] PromptRequest request)
        {                        
            EvaluationResponse result = await this._evaluationService.EvaluateArtAsync(request.Base64Image, request.MimeType);

            if (request.ShowcaseResultAccepted) 
            {
                string imageHash = request.Base64Image.GetHashCode().ToString();

                await this._context.Publishers.AddAsync(new ArtPublisherModel 
                {
                    Arts = new List<ArtModel>() 
                    {
                        new ArtModel
                        {
                            ImageHash = request.Base64Image.GetHashCode().ToString(),
                            ImagePath = request.Base64Image,
                            ArtRateResultModel = new ArtRateResultModel
                            {
                                Creativity = result.Creativity,
                                Complexity = result.Complexity,
                                RenderQuality = result.RenderQuality,
                                LightingAndColors = result.LightingAndColors,
                                Composition = result.Composition,
                                StylisticConsistency = result.StylisticConsistency,
                                ImprovementTips = result.ImprovementTips,
                            },
                            CreatedAt = DateTime.UtcNow
                        }
                    },
                    LeaderBoardScores = new List<PublisherLeaderBoardScoreModel>(),
                    Nickname = request.Nickname ?? "Anonymous",
                    LeaderBoardRate = result.OverallScore,                    
                });

                await this._context.SaveChangesAsync();

                if (request.Nickname != null) 
                {
                    int publisherId = this._context.Publishers
                        .Where(p => p.Nickname == (request.Nickname ?? "Anonymous"))
                        .Select(p => p.Id)
                        .FirstOrDefault();

                    await this._leaderBoardService.UpdateLeaderBoardRateAsync(
                        publisherId,
                        result.OverallScore,
                        await this.SaveImageToDiskAsync(request.Base64Image, imageHash, request.MimeType));
                }
            }

            return Ok(result);
        }

        private async Task<string> SaveImageToDiskAsync(string base64String, string hash, string mimeType)
        {
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string extension = mimeType.Split('/').LastOrDefault() ?? "jpg";

            string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            string fileName = $"{hash}_{uniqueId}.{extension}";

            string filePath = Path.Combine(uploadsFolder, fileName);

            byte[] imageBytes = Convert.FromBase64String(base64String);
            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            var host = HttpContext.Request.Host.ToUriComponent();

            return $"{HttpContext.Request.Scheme}://{host}/uploads/{fileName}";
        }

        [HttpGet("getLeaderBoard")]
        public async Task<IActionResult> GetLeaderBoard()
        {
            List<LeaderBoardResultDto> result = await this._leaderBoardService.GetLeaderBoard(10);
            return Ok(result);
        }
    }
}
