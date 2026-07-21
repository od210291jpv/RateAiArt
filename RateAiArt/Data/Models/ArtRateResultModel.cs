namespace RateAiArt.Data.Models
{
    public class ArtRateResultModel
    {
        public int Id { get; set; }

        public int ArtId { get; set; }

        public ArtModel Art { get; set; } = null!;

        public double Creativity { get; set; }

        public double Complexity { get; set; }

        public double RenderQuality { get; set; }

        public double LightingAndColors { get; set; }

        public double Composition { get; set; }

        public double StylisticConsistency { get; set; }

        public List<string> ImprovementTips { get; set; } = new();
    }
}
