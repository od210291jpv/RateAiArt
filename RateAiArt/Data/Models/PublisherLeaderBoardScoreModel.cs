namespace RateAiArt.Data.Models
{
    public class PublisherLeaderBoardScoreModel
    {
        public int Id { get; set; }
        
        public int PublisherId { get; set; }
        
        public ArtPublisherModel Publisher { get; set; }
        
        public double LeaderBoardRate { get; set; }
        
        public string ArtUrl { get; set; }
    }
}
