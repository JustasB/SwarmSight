namespace SwarmSight.HeadPartsTracking.Models
{
    public class IndexDouble : MinMaxDouble
    {
        public IndexDouble()
        {
            Min = 0;
            Max = 1;
        }

        public static implicit operator IndexDouble(double index)
        {
            return new IndexDouble { Value = index };
        }


    }
}