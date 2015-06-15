namespace SwarmVision.Models
{
    public class AntenaModel
    {
        public Segment Root;
        public Segment Tip;

        public AntenaModel()
        {
            Root = new Segment();
            Tip = new Segment(); 
        }
    }
}