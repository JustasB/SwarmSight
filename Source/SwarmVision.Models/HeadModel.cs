

using System;
using System.Windows;

namespace SwarmVision.Models
{
    public class HeadModel : IDisposable
    {
        public double Scale = 1;
        public double ScaleMin = 1.0;
        public double ScaleMax = 1.5;

        public double AngleIndex;
        public double AngleMin = -15;
        public double AngleMax = 20;

        public Point Origin;

        public double Angle
        {
            get { return (AngleMax - AngleMin)*AngleIndex+AngleMin; }
        }

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
            if (View != null)
            {
                View.Dispose();
                View = null;
            }
        }

        public HeadModel()
        {
            LeftAntena = new AntenaModel();
            RightAntena = new AntenaModel();
            
            

            //Mandibles are short and have limited opening
            Mandible = new Segment
            {
                Start = {X = 3},
                Length = 0,
                Thickness = 1,
                AngleMax = 90, //Fully open
                AngleMin = 0, //Fully closed
                LengthMax = 0,//10,
                LengthMin = 0,//3,
            };

            Proboscis = new ProboscisModel();

            //Proboscis can be extended or retracted and sways side to side somewhat
            Proboscis.Proboscis.Start.Y = -1;
            Proboscis.Proboscis.AngleMin = -15;
            Proboscis.Proboscis.AngleMax = 15;
            Proboscis.Proboscis.Length = 0;
            Proboscis.Proboscis.LengthMin = 0;
            Proboscis.Proboscis.LengthMax = 0;//30;

            //Proboscis tongue can be extended, and sways very little
            Proboscis.Tongue.Length = 0;
            Proboscis.Tongue.LengthMin = 0;
            Proboscis.Tongue.LengthMax = 0;//30;
            Proboscis.Tongue.AngleMin = -5;
            Proboscis.Tongue.AngleMax = 5;


            RightAntena.Root.Start.X = LeftAntena.Root.Start.X = 4;
            RightAntena.Root.StartXMin = LeftAntena.Root.StartXMin = 2;
            RightAntena.Root.StartXMax = LeftAntena.Root.StartXMax = 6;

            RightAntena.Root.Start.Y = LeftAntena.Root.Start.Y = -4;
            RightAntena.Root.StartYMin = LeftAntena.Root.StartYMin = -6;
            RightAntena.Root.StartYMax = LeftAntena.Root.StartYMax = -2;

            RightAntena.Root.AngleIndex = LeftAntena.Root.AngleIndex = 90 / 180.0;
            RightAntena.Root.AngleMin = LeftAntena.Root.AngleMin = 2;
            RightAntena.Root.AngleMax = LeftAntena.Root.AngleMax = 136;
            RightAntena.Root.Length = LeftAntena.Root.Length = 0;
            RightAntena.Root.LengthMin = LeftAntena.Root.LengthMin = 5;
            RightAntena.Root.LengthMax = LeftAntena.Root.LengthMax = 17;
            RightAntena.Root.Thickness = LeftAntena.Root.Thickness = 3;
            RightAntena.Root.ThicknessMin = LeftAntena.Root.ThicknessMin = 1;
            RightAntena.Root.ThicknessMax = LeftAntena.Root.ThicknessMax = 5;

            RightAntena.Tip.AngleIndex = LeftAntena.Tip.AngleIndex = -90 / 180.0;
            RightAntena.Tip.AngleMin = LeftAntena.Tip.AngleMin = -70;
            RightAntena.Tip.AngleMax = LeftAntena.Tip.AngleMax = 3;
            RightAntena.Tip.Length = LeftAntena.Tip.Length = 0;
            RightAntena.Tip.LengthMin = LeftAntena.Tip.LengthMin = 11;
            RightAntena.Tip.LengthMax = LeftAntena.Tip.LengthMax = 35;
            RightAntena.Tip.Thickness = LeftAntena.Tip.Thickness = 3;
            RightAntena.Tip.ThicknessMin = LeftAntena.Tip.ThicknessMin = 1;
            RightAntena.Tip.ThicknessMax = LeftAntena.Tip.ThicknessMax = 5;

        }

        public HeadModel Clone()
        {
            var clone = new HeadModel
            {
                Scale = Scale,
                ScaleMin = ScaleMin,
                ScaleMax = ScaleMax,
                AngleIndex = AngleIndex,
                AngleMin = AngleMin,
                AngleMax = AngleMax,
                Origin = new Point(Origin.X, Origin.Y)
            };

            return clone;
        }
    }
}
