using System;
using System.Drawing;

namespace SwarmVision.Models
{
    public class AntenaModel
    {
        public static AntenaModel FromCoords(Point root, Point tip)
        {
            var result = new AntenaModel();

            var rootAngle = Math.Atan2(root.Y, root.X) * (180.0 / Math.PI);

            var rootAngleIndex = (rootAngle - result.Root.AngleMin) / 
                (result.Root.AngleMax - result.Root.AngleMin);

            result.Root.AngleIndex = rootAngleIndex;
            result.Root.Length = Math.Sqrt(root.X* root.X + root.Y* root.Y);

            return result;
        }

        public Segment Root;
        public Segment Tip;

        public int TipX;
        public int TipY;

        public AntenaModel()
        {
            Root = new Segment();
            Tip = new Segment(); 
        }

        public Point RootCoordinates()
        {
            return new Point
            (
                (int)(Root.Start.X + Math.Round(Math.Sin(Root.AngleRad)*Root.Length)),
                (int)(Root.Start.Y + Math.Round(Math.Cos(Root.AngleRad) * Root.Length))
            );
        }

        public Point TipCoordinates()
        {
            var rootEnd = RootCoordinates();

            return new Point
            (
                rootEnd.X + (int)Math.Round(Tip.Length * Math.Sin(Root.AngleRad + Tip.AngleRad)),
                rootEnd.Y + (int)Math.Round(Tip.Length * Math.Cos(Root.AngleRad + Tip.AngleRad))
            );
        }
    }
}