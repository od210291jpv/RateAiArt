using Microsoft.AspNetCore.Mvc;

using RateAiArt.Data;
using RateAiArt.Data.Models;
using RateAiArt.DTO.Ai;
using RateAiArt.Services;

namespace RateAiArt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateAiController : Controller
    {
        private readonly ApplicationContext context;
        private readonly IAiEvaluationService _evaluationService;

        public RateAiController(ApplicationContext context, IAiEvaluationService evaluationService)
        {
            this.context = context;
            _evaluationService = evaluationService;
        }

        [HttpPost("rateAiArt")]
        public async Task<IActionResult> RateAiArt([FromBody] PromptRequest request)
        {                        
            EvaluationResponse result = await this._evaluationService.EvaluateArtAsync(request.Base64Image, request.MimeType);

            if (request.ShowcaseResultAccepted) 
            {
                await this.context.Publishers.AddAsync(new ArtPublisherModel 
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

                await this.context.SaveChangesAsync();

                if (request.Nickname != null) 
                {
                    int publisherId = this.context.Publishers
                        .Where(p => p.Nickname == (request.Nickname ?? "Anonymous"))
                        .Select(p => p.Id)
                        .FirstOrDefault();

                    await this.context.PublisherLeaderBoardScores.AddAsync(new PublisherLeaderBoardScoreModel
                    {
                        PublisherId = publisherId,
                        LeaderBoardRate = result.OverallScore,
                        ArtUrl = ""
                    });

                    await this.context.SaveChangesAsync();
                }
            }

            return Ok(result);
        }

        [HttpGet("getLeaderBoard")]
        public IActionResult GetLeaderBoard()
        {
            return Ok();
        }
    }
}
