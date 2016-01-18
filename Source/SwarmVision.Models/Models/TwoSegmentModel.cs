using System;
using System.Drawing;

namespace SwarmVision.HeadPartsTracking.Models
{
    public class TwoSegmentModel
    {
        public static TwoSegmentModel FromCoords(Point joint, Point tip)
        {
            var result = new TwoSegmentModel();

            //Compute first segment's angle and length
            result.Root.Angle.SetValueUsingXY(joint.X, joint.Y);
            result.Root.Length.Value = Math.Sqrt(joint.X * joint.X + joint.Y * joint.Y);

            //Compute second segment's angle and length
            var xDist = tip.X - joint.X;
            var yDist = tip.Y - joint.Y;

            result.Tip.Angle.SetValueUsingXY(xDist, yDist, result.Root.Angle);
            result.Tip.Length.Value = Math.Sqrt(xDist * xDist + yDist * yDist);

            return result;
        }

        

        public Segment Root;
        public Segment Tip;

        public int TipX;
        public int TipY;

        public TwoSegmentModel()
        {
            Root = new Segment();
            Tip = new Segment(); 
        }

        public Point RootCoordinates()
        {
            return new Point
            (
                (int) (Root.StartX + Math.Round(Math.Sin(Root.Angle.InRadians())*Root.Length)),
                (int) (Root.StartY + Math.Round(Math.Cos(Root.Angle.InRadians())*Root.Length))
            );
        }

        public Point TipCoordinates()
        {
            var rootEnd = RootCoordinates();

            return new Point
            (
                rootEnd.X + (int)Math.Round(Tip.Length * Math.Sin(Root.Angle.InRadians() + Tip.Angle.InRadians())),
                rootEnd.Y + (int)Math.Round(Tip.Length * Math.Cos(Root.Angle.InRadians() + Tip.Angle.InRadians()))
            );
        }
    }
}