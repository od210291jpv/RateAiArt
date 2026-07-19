using RateAiArt.DTO.Ai;
using RateAiArt.DTO.Publisher;

namespace RateAiArt.DTO.LeaderBoard
{
    public class LeaderBoardResultDto
    {
        public int Id { get; set; }

        public PublisherDto Publisher { get; set; } = null!;

        public double LeaderBoardRate { get; set; }

        public string ArtUrl { get; set; } = string.Empty;

        public EvaluationResponse? EvaluationResult { get; set; }
    }
}
