

using SwarmVision.Filters;
using System;
using System.Windows;
using System.Drawing;

namespace SwarmVision.HeadPartsTracking.Models
{
    public class HeadModel : IDisposable
    {
        public MinMaxDouble ScaleX = new MinMaxDouble(1, 0.1, 7.9);
        public MinMaxDouble ScaleY = new MinMaxDouble(1, 0.1, 7.9);
        public AngleInDegrees Angle = new AngleInDegrees(0, -270, +270);

        public System.Windows.Point Origin = new System.Windows.Point(0, 0);
        public System.Windows.Point Dimensions = new System.Windows.Point(4*83, 4*83);

        public TwoSegmentModel LeftAntena;
        public TwoSegmentModel RightAntena;
        public TwoSegmentModel Proboscis;

        public Frame View; 

        public override string ToString()
        {
            return string.Format(Origin.ToString() + ", A: {0}, S: {1}{2}", Angle, ScaleX, ScaleY);
        }

        public void Dispose()
        {
            if (View != null)
            {
                View.Dispose();
                View = null;
            }
        }

        public HeadModel(bool mouthparts = false)
        {
            LeftAntena = new TwoSegmentModel();
            RightAntena = new TwoSegmentModel();
            Proboscis = new TwoSegmentModel();

            if (mouthparts)
            {
                //Proboscis can be extended or retracted and sways side to side somewhat
                Proboscis.Root.StartY.Value = 22;

                Proboscis.Root.Thickness = new MinMaxDouble(3, 1, 5);
                Proboscis.Root.Angle = new AngleInDegrees(0, -15, 15);
                Proboscis.Root.Length = new MinMaxDouble(0, 0, 50);

                //Proboscis tongue can be extended, and sways very little
                Proboscis.Tip.Length.Min = 0;
                Proboscis.Tip.Length.Max = 0; //30;
                Proboscis.Tip.Length.Value = 0;

                Proboscis.Tip.Angle.Min = -5;
                Proboscis.Tip.Angle.Max = 5;
                Proboscis.Tip.Angle.Value = 0;

                RightAntena.Root.StartX.Min = LeftAntena.Root.StartX.Min = 2;
                RightAntena.Root.StartX.Max = LeftAntena.Root.StartX.Max = 6;
                RightAntena.Root.StartX.Value = LeftAntena.Root.StartX.Value = 4;

                RightAntena.Root.StartY.Min = LeftAntena.Root.StartY.Min = -6;
                RightAntena.Root.StartY.Max = LeftAntena.Root.StartY.Max = -2;
                RightAntena.Root.StartY.Value = LeftAntena.Root.StartY.Value = -4;

                RightAntena.Root.Angle.Index = LeftAntena.Root.Angle.Index = 0;
                RightAntena.Root.Angle.Min = LeftAntena.Root.Angle.Min = 2;
                RightAntena.Root.Angle.Max = LeftAntena.Root.Angle.Max = 136;

                RightAntena.Root.Length.Min = LeftAntena.Root.Length.Min = 5;
                RightAntena.Root.Length.Max = LeftAntena.Root.Length.Max = 17;
                RightAntena.Root.Length.Value = LeftAntena.Root.Length.Value = 0;

                RightAntena.Root.Thickness.Min = LeftAntena.Root.Thickness.Min = 1;
                RightAntena.Root.Thickness.Max = LeftAntena.Root.Thickness.Max = 5;
                RightAntena.Root.Thickness.Value = LeftAntena.Root.Thickness.Value = 3;

                RightAntena.Tip.Angle.Index = LeftAntena.Tip.Angle.Index = -90/180.0;
                RightAntena.Tip.Angle.Min = LeftAntena.Tip.Angle.Min = -70;
                RightAntena.Tip.Angle.Max = LeftAntena.Tip.Angle.Max = 3;
                RightAntena.Tip.Length.Min = LeftAntena.Tip.Length.Min = 11;
                RightAntena.Tip.Length.Max = LeftAntena.Tip.Length.Max = 35;
                RightAntena.Tip.Length.Value = LeftAntena.Tip.Length.Value = 0;
                RightAntena.Tip.Thickness.Min = LeftAntena.Tip.Thickness.Min = 1;
                RightAntena.Tip.Thickness.Max = LeftAntena.Tip.Thickness.Max = 5;
                RightAntena.Tip.Thickness.Value = LeftAntena.Tip.Thickness.Value = 3;

            }

        }

        public HeadModel()
        {
            
        }

        public static HeadModel FromNormalizedCoordinates(
            int LeftAntenaTipX,
            int LeftAntenaTipY,
            int RightAntenaTipX,
            int RightAntenaTipY,
            int RightAntenaJointX,
            int RightAntenaJointY,
            int LeftAntenaJointX,
            int LeftAntenaJointY,
            int ProboscisTipX,
            int ProboscisTipY
            )
        {
            var head = new HeadModel(true);

            //Convert normalized coordinates to view coordinates

            head.LeftAntena = TwoSegmentModel.FromCoords(
                new System.Drawing.Point(HeadView.Denormalize(LeftAntenaJointX), HeadView.Denormalize(LeftAntenaJointY)),
                new System.Drawing.Point(HeadView.Denormalize(LeftAntenaTipX), HeadView.Denormalize(LeftAntenaTipY))
            );

            head.RightAntena = TwoSegmentModel.FromCoords(
                new System.Drawing.Point(HeadView.Denormalize(RightAntenaJointX), HeadView.Denormalize(RightAntenaJointY)),
                new System.Drawing.Point(HeadView.Denormalize(RightAntenaTipX), HeadView.Denormalize(RightAntenaTipY))
            );

            head.Proboscis = TwoSegmentModel.FromCoords(
                new System.Drawing.Point(HeadView.Denormalize(ProboscisTipX), HeadView.Denormalize(ProboscisTipY - HeadView.HeadLength)),
                new System.Drawing.Point(HeadView.Denormalize(ProboscisTipX), HeadView.Denormalize(ProboscisTipY - HeadView.HeadLength))
            );

            head.Proboscis.Root.StartY.Value = 22;

            return head;
        }

        public Frame GenerateView(bool useGPU)
        {
            if (View != null)
                View.Dispose();

            View = new HeadView(this).Draw(useGPU);

            return View;
        }
    }
}
