

using System;
using System.Windows;

namespace SwarmVision.Models
{
    public class HeadModel : IDisposable
    {
        public double Scale = 1.0;
        public int Angle;
        public Point Origin;

        public AntenaModel LeftAntena;
        public AntenaModel RightAntena;

        //Left is symetric to right
        public Segment Mandible;

        public ProboscisModel Proboscis;

        public IDisposable View; 

        public double AngleRad
        {
            get { return Angle * Math.PI / 180.0; }
        }

        public override string ToString()
        {
            return string.Format(Origin.ToString() + ", A: {0}, S: {1}", Angle, Scale);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public void Dispose()
        {
            if(View != null)
                View.Dispose();
        }

        public HeadModel()
        {
            LeftAntena = new AntenaModel();
            RightAntena = new AntenaModel();
            Mandible = new Segment();
            Proboscis = new ProboscisModel();

            Mandible.Start.X = 3;
            Mandible.Angle = 45;
            Mandible.Length = 5;
            Mandible.Thickness = 1;

            Proboscis.Proboscis.Start.Y = -1;
            Proboscis.Proboscis.Length = 0;
            Proboscis.Tongue.Length = 0;

            RightAntena.Root.Angle = 90;
            RightAntena.Root.Length = 0;
            RightAntena.Tip.Angle = -90;
            RightAntena.Tip.Length = 0;
            RightAntena.Root.Thickness = 2;
            RightAntena.Tip.Thickness = 2;

            LeftAntena.Root.Angle = 90;
            LeftAntena.Root.Length = 0;
            LeftAntena.Tip.Angle = -90;
            LeftAntena.Tip.Length = 0;
            LeftAntena.Root.Thickness = 2;
            LeftAntena.Tip.Thickness = 2;

        }
    }
}
