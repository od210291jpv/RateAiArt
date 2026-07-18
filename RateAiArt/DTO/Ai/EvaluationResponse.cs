namespace RateAiArt.DTO.Ai
{
    public class EvaluationResponse
    {
        public int Creativity { get; set; }

        public int Complexity { get; set; }

        public int RenderQuality { get; set; }

        public int LightingAndColors { get; set; }

        public int Composition { get; set; }

        public int StylisticConsistency { get; set; }

        public List<string> ImprovementTips { get; set; } = new();

        public int OverallScore
        {
            get
            {
                return (Creativity + Complexity + RenderQuality + LightingAndColors + Composition + StylisticConsistency) / 6;
            }
        }
    }
}
