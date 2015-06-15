namespace SwarmVision.Models
{
    public class ProboscisModel
    {
        public Segment Proboscis;
        public Segment Tongue;

        public ProboscisModel()
        {
            Proboscis = new Segment();
            Tongue = new Segment();
            
            Proboscis.Thickness = 2;
        }
    }
}