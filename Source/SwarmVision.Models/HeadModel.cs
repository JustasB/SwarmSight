

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
        public double AngleMin = -45;
        public double AngleMax = 45;

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
                Length = 5,
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

            
            RightAntena.Root.AngleIndex = 90/180.0;
            RightAntena.Root.AngleMin = 0;
            RightAntena.Root.AngleMax = 180;
            RightAntena.Root.Length = 0;
            RightAntena.Root.LengthMin = 0;
            RightAntena.Root.LengthMax = 0;//30;
            RightAntena.Root.Thickness = 2;

            RightAntena.Tip.AngleIndex = -90/180.0;
            RightAntena.Tip.AngleMin = -135;
            RightAntena.Tip.AngleMax = 0;
            RightAntena.Tip.Length = 0;
            RightAntena.Tip.LengthMin = 0;
            RightAntena.Tip.LengthMax = 0;//30;
            RightAntena.Tip.Thickness = 2;


            //Left angles are reversed
            LeftAntena.Root.AngleIndex = 90 / 180.0;
            LeftAntena.Root.AngleMin = 0;
            LeftAntena.Root.AngleMax = 180;
            LeftAntena.Root.Length = 0;
            LeftAntena.Root.LengthMin = 0;
            LeftAntena.Root.LengthMax = 0;//30;
            LeftAntena.Root.Thickness = 2;

            LeftAntena.Tip.AngleIndex = -90 / 180.0;
            LeftAntena.Tip.AngleMin = -135;
            LeftAntena.Tip.AngleMax = 0;
            LeftAntena.Tip.Length = 0;
            LeftAntena.Tip.LengthMin = 0;
            LeftAntena.Tip.LengthMax = 0;//30;
            LeftAntena.Tip.Thickness = 2;



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
