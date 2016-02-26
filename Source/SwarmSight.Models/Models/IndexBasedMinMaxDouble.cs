namespace SwarmSight.HeadPartsTracking.Models
{
    public class IndexBasedMinMaxDouble : MinMaxDouble
    {
        public IndexDouble Index;

        public IndexBasedMinMaxDouble(double value, double min, double max) : base(value, min, max)
        {

        }

        public override double Value
        {
            get { return Index*(Max - Min) + Min; }
            set { Index = (value - Min)/(Max - Min); }
        }

        public new IndexBasedMinMaxDouble Clone()
        {
            var baseClone = (IndexBasedMinMaxDouble) base.Clone();

            baseClone.Index = Index;

            return baseClone;
        }
    }
}