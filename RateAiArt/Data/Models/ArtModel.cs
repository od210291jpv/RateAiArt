namespace RateAiArt.Data.Models
{
    public class ArtModel
    {
        public int Id { get; set; }
        
        public string ImageHash { get; set; }
        
        public string ImagePath { get; set; }
        
        public ArtRateResultModel ArtRateResultModel { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}
