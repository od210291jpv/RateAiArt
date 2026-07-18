namespace RateAiArt.Data.Models
{
    public class ArtModel
    {
        public int Id { get; set; } = 0;

        public int PublisherId { get; set; } = 0;

        public ArtPublisherModel Publisher { get; set; } = null!;

        public string ImageHash { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty;

        public ArtRateResultModel ArtRateResultModel { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
