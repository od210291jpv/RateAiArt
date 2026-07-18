using RateAiArt.DTO.Ai;

namespace RateAiArt.Services
{
    public interface IAiEvaluationService
    {
        Task<EvaluationResponse> EvaluateArtAsync(string Base64Image, string mimeType);
    }
}