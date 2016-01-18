using System;

namespace SwarmVision.HeadPartsTracking.Models
{
    public class AngleInDegrees : IndexBasedMinMaxDouble
    {
        private const double RadiansPerDegree = Math.PI/180.0;
        private const double DegreesPerRadian = 180.0/Math.PI;

        public AngleInDegrees(double value, double min, double max) : base(value, min, max)
        {
            
        }

        public double InRadians()
        {
            return Value*RadiansPerDegree;
        }

        public void SetValueUsingRadians(double radians)
        {
            Value = radians*DegreesPerRadian;
        }

        public void SetValueUsingXY(double x, double y, double relativeToAngle = 0)
        {
            var rootAngle = -(Math.Atan2(y, x) * DegreesPerRadian - 90.0 - relativeToAngle);
            
            Value = BoundAngleToPlusMinus180(rootAngle);
        }

        public double BoundAngleToPlusMinus180(double value)
        {
            var bwpm360 = value%360;

            if (bwpm360 > 180) return bwpm360 - 360;
            if (bwpm360 < -180) return bwpm360 + 360;

            return bwpm360;
        }
    }
}