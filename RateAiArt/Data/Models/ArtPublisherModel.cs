namespace RateAiArt.Data.Models
{
    public class ArtPublisherModel
    {
        public int Id { get; set; }

        public List<ArtModel> Arts { get; set; } = new();

        public List<PublisherLeaderBoardScoreModel> LeaderBoardScores { get; set; } = new();

        public string Nickname { get; set; } = string.Empty;
        
        public int? LeaderBoardRate { get; set; }
    }
}
