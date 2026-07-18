using Microsoft.AspNetCore.Mvc;

namespace RateAiArt.Data.Models
{
    public class PublisherLeaderBoardScoreModel
    {
        public int Id { get; set; }
        
        public string NickName { get; set; }
        
        public double LeaderBoardRate { get; set; }
        
        public string ArtUrl { get; set; }
    }
}
