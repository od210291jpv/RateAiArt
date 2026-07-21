namespace RateAiArt.DTO.Ai
{
    public class EvaluationResponse
    {
        public double Creativity { get; set; }

        public double Complexity { get; set; }

        public double RenderQuality { get; set; }

        public double LightingAndColors { get; set; }

        public double Composition { get; set; }

        public double StylisticConsistency { get; set; }

        public List<string> ImprovementTips { get; set; } = new();

        public double OverallScore
        {
            get
            {
                return (Creativity + Complexity + RenderQuality + LightingAndColors + Composition + StylisticConsistency) / 6d;
            }
        }
    }
}
