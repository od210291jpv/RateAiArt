namespace RateAiArt.Data.Models
{
    public class ArtRateResultModel
    {
        public int Id { get; set; }

        public int ArtId { get; set; }

        public ArtModel Art { get; set; }

        public int Creativity { get; set; }

        public int Complexity { get; set; }

        public int RenderQuality { get; set; }

        public int LightingAndColors { get; set; }

        public int Composition { get; set; }

        public int StylisticConsistency { get; set; }

        public List<string> ImprovementTips { get; set; } = new();
    }
}
