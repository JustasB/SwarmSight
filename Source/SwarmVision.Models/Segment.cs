using System;
using System.Windows;

namespace SwarmVision.Models
{
    public class Segment
    {
        public Point Start;
        public double Angle;
        public double Length;

        public int Thickness = 1;

        public double AngleRad
        {
            get { return Angle*Math.PI/180.0; }
        }
    }
}