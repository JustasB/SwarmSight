using System;
using System.Windows;

namespace SwarmVision.Models
{
    public class Segment
    {
        public Point Start;
        public int StartXMin;
        public int StartXMax;
        public int StartYMin;
        public int StartYMax;

        /// <summary>
        /// 0..1 value corresponding to the min, max angles
        /// </summary>
        public double AngleIndex;

        //360 by default
        public double AngleMin = -180;
        public double AngleMax = 180;

        public double Length;

        //Positive and up to 10 long
        public double LengthMin = 0;
        public double LengthMax = 10;

        public int Thickness = 1;
        public int ThicknessMin = 1;
        public int ThicknessMax = 10;

        public double Angle
        {
            get { return (AngleMax - AngleMin)*AngleIndex+AngleMin; }   
        }
        public double AngleRad
        {
            get { return Angle*Math.PI/180.0; }
        }

        public override string ToString()
        {
            return string.Format("Ang:{0}, Len:{1}", Angle, Length);
        }
    }
}