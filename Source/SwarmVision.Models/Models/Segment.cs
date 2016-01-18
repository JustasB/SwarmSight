using System;
using System.Windows;

namespace SwarmVision.HeadPartsTracking.Models
{
    public class Segment
    {
        public MinMaxDouble StartX = new MinMaxDouble();
        public MinMaxDouble StartY = new MinMaxDouble();
        
        public AngleInDegrees Angle = new AngleInDegrees(0, -180, 180);
        public MinMaxDouble Length = new MinMaxDouble(0, 0, 100);
        public MinMaxDouble Thickness = new MinMaxDouble(1, 1, 10);

        public override string ToString()
        {
            return string.Format("{0} deg, {1} px", Angle.Value, Length);
        }
    }
}