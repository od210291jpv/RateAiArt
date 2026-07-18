using RateAiArt.DTO.Ai;

namespace RateAiArt.Services
{
    public interface IAiEvaluationService
    {
        Task<EvaluationResponse> EvaluateArtAsync(byte[] imageBytes, string mimeType);
    }
}