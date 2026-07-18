namespace RateAiArt.Data.Models
{
    public class PublisherLeaderBoardScoreModel
    {
        public int Id { get; set; }
        
        public int PublisherId { get; set; }

        public ArtPublisherModel Publisher { get; set; } = null!;
        
        public double LeaderBoardRate { get; set; }

        public string ArtUrl { get; set; } = string.Empty;
    }
}
