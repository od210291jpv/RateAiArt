namespace RateAiArt.Data.Models
{
    public class ArtPublisherModel
    {
        public int Id { get; set; }
        
        public List<ArtModel> Arts { get; set; }
        
        public string Nickname { get; set; }
        
        public int? LeaderBoardRate { get; set; }
    }
}
