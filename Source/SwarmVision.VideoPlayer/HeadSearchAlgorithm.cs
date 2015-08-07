using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using AForge.Neuro;
using SwarmVision.Models;
using Point = System.Windows.Point;

namespace SwarmVision.VideoPlayer
{
    public class HeadSearchAlgorithm : GeneticAlgoBase<HeadModel>
    {
        private Network neuralNet;

        public HeadSearchAlgorithm()
        {
            neuralNet = Network.Load(@"c:\temp\PER.net");
        }

        protected override HeadModel CreateChild(HeadModel parent1, HeadModel parent2)
        {
            //HEAD
            var result = new HeadModel
            {
                Origin = new Point
                (
                    x: (int)Cross(parent1.Origin.X, parent2.Origin.X, 0),
                    y: (int)Cross(parent1.Origin.Y, parent2.Origin.Y, 0)
                ),
                AngleIndex = Cross(parent1.AngleIndex, parent2.AngleIndex, 0, 1)
            };

            result.Scale = Cross(parent1.Scale, parent2.Scale, parent1.ScaleMin, parent1.ScaleMax);

            //PROBOSCIS - MAIN SEGMENT
            result.Proboscis.Proboscis.AngleIndex = Cross
            (
                parent1.Proboscis.Proboscis.AngleIndex, 
                parent2.Proboscis.Proboscis.AngleIndex, 
                0,
                1
            );

            result.Proboscis.Proboscis.Length = (int)Cross
            (
                parent1.Proboscis.Proboscis.Length,
                parent2.Proboscis.Proboscis.Length,
                parent1.Proboscis.Proboscis.LengthMin,
                parent1.Proboscis.Proboscis.LengthMax
            );

            //PROBOSCIS - TONGUE
            result.Proboscis.Tongue.AngleIndex = Cross
            (
                parent1.Proboscis.Tongue.AngleIndex,
                parent2.Proboscis.Tongue.AngleIndex,
                0,
                1
            );

            result.Proboscis.Tongue.Length = (int)Cross
            (
                parent1.Proboscis.Tongue.Length,
                parent2.Proboscis.Tongue.Length,
                parent1.Proboscis.Tongue.LengthMin,
                parent1.Proboscis.Tongue.LengthMax
            );

            //MANDIBLE
            result.Mandible.AngleIndex = Cross
            (
                parent1.Mandible.AngleIndex,
                parent2.Mandible.AngleIndex,
                0,
                1
            );

            result.Mandible.Length = (int)Cross
            (
                parent1.Mandible.Length,
                parent2.Mandible.Length,
                parent1.Mandible.LengthMin,
                parent1.Mandible.LengthMax
            );

            //ANTENA - LEFT - ROOT
            result.LeftAntena.Root.AngleIndex = Cross
            (
                parent1.LeftAntena.Root.AngleIndex,
                parent2.LeftAntena.Root.AngleIndex,
                0,
                1
            );

            result.LeftAntena.Root.Length = (int)Cross
            (
                parent1.LeftAntena.Root.Length,
                parent2.LeftAntena.Root.Length,
                parent1.LeftAntena.Root.LengthMin,
                parent1.LeftAntena.Root.LengthMax
            );

            //ANTENA - LEFT - TIP
            result.LeftAntena.Tip.AngleIndex = Cross
            (
                parent1.LeftAntena.Tip.AngleIndex,
                parent2.LeftAntena.Tip.AngleIndex,
                0,
                1
            );

            result.LeftAntena.Tip.Length = (int)Cross
            (
                parent1.LeftAntena.Tip.Length,
                parent2.LeftAntena.Tip.Length,
                parent1.LeftAntena.Tip.LengthMin,
                parent1.LeftAntena.Tip.LengthMax
            );

            //ANTENA - RIGHT - ROOT
            result.RightAntena.Root.AngleIndex = Cross
            (
                parent1.RightAntena.Root.AngleIndex,
                parent2.RightAntena.Root.AngleIndex,
                0,
                1
            );

            result.RightAntena.Root.Length = (int)Cross
            (
                parent1.RightAntena.Root.Length,
                parent2.RightAntena.Root.Length,
                parent1.RightAntena.Root.LengthMin,
                parent1.RightAntena.Root.LengthMax
            );

            //ANTENA - RIGHT - TIP
            result.RightAntena.Tip.AngleIndex = Cross
            (
                parent1.RightAntena.Tip.AngleIndex,
                parent2.RightAntena.Tip.AngleIndex,
                0,
                1
            );

            result.RightAntena.Tip.Length = (int)Cross
            (
                parent1.RightAntena.Tip.Length,
                parent2.RightAntena.Tip.Length,
                parent1.RightAntena.Tip.LengthMin,
                parent1.RightAntena.Tip.LengthMax
            );


            return result;
        }



        private double AngleDistance(double firstAngle, double secondAngle)
        {
            double difference = secondAngle - firstAngle;
            while (difference < -180) difference += 360;
            while (difference > 180) difference -= 360;
            return difference;
        }

        protected override bool ValidChild(HeadModel child)
        {
            return Target.ValidConvolutionLocation(HeadView.Width, HeadView.Height, (int)child.Origin.X, (int)child.Origin.Y, child.Angle);
        }

        protected override HeadModel SelectLocation()
        {
            //var aveX = (int)Generation.Average(i => i.Key.Origin.X);
            //var aveY = (int)Generation.Average(i => i.Key.Origin.Y);
            ////var aveWidth = (int)Generation.Average(i => ((Frame)(i.Key.View)).Width);
            ////var aveHeight = (int)Generation.Average(i => ((Frame)(i.Key.View)).Height);
            //var aveAngleIndex = Generation.Average(i => i.Key.AngleIndex);

            //var result = new HeadModel { Origin = new Point(aveX, aveY), AngleIndex = aveAngleIndex };

            ////Draw all
            //foreach (var i in Generation)
            //{
            //    var model = i.Key;

            //    using (var view = CreateHeadView(model))
            //        Target.DrawFrame(view, (int)model.Origin.X, (int)model.Origin.Y);
            //}

            //Draw top
            var top = Generation.First().Key;

            //using (var view = CreateHeadView(top))
            //{
            //    Target.DrawFrame(view, (int) top.Origin.X, (int) top.Origin.Y);
            //}


            ////Create frame grabs of head location
            //using (var view = CreateHeadView(top))
            //using (var bmp = Frame.FromBitmap(Target.Bitmap.Clone(new Rectangle((int)top.Origin.X, (int)top.Origin.Y, view.Width, view.Height), PixelFormat.Format24bppRgb)))
            //using (var normalized = bmp.RotateScale(-top.Angle, 1.0 / top.Scale))
            //{
            //    normalized.Bitmap.Save("Y:\\bees\\" + frameIndex + ".bmp", ImageFormat.Bmp);
            //    frameIndex++;
            //}

            ////Create frame grabs of head location
            using (var view = CreateHeadView(top))
            using (var bmp = Frame.FromBitmap(Target.Bitmap.Clone(new Rectangle((int)top.Origin.X, (int)top.Origin.Y, view.Width, view.Height), PixelFormat.Format24bppRgb)))
            using (var normalized = bmp.RotateScale(-top.Angle, 1.0 / top.Scale))
            using (var resized = normalized.Resize(10, 10))
            {
                Target.DrawFrame(view, (int)top.Origin.X, (int)top.Origin.Y);

                var nnInput = resized.ToAccordInput();
                var output = neuralNet.Compute(nnInput)[0];

                Target.DrawRectangle(0, 0, (int)(100 * output + 10), 10);
            }


            return top;
        }

        protected override double ComputeFitness(HeadModel individual)
        {
            if (individual.View == null)
            {
                individual.View = CreateHeadView(individual);
            }

            var result = Target.Compare((Frame)individual.View, (int)individual.Origin.X, (int)individual.Origin.Y);

            //Adjust for scale
            result /= individual.Scale;

            return result;
        }

        protected Frame CreateHeadView(HeadModel head)
        {
            using (var raw = Frame.FromBitmap(new HeadView(head).Draw()))
            using (var edged = raw.EdgeFilter())
            {
                return edged.RotateScale(head.Angle, head.Scale);
            }
        }

        protected override HeadModel CreateNewRandomMember()
        {
            //HEAD
            var result = new HeadModel
            {
                Origin = new Point
                (
                    Random.Next(0, Target.Width - HeadView.Width),
                    Random.Next(0, Target.Height - HeadView.Height)
                ),
                AngleIndex = Random.NextDouble(),
            };

            result.Scale = Random.NextDouble()*(result.ScaleMax - result.ScaleMin) + result.ScaleMin;

            //PROBOSCIS - MAIN SEGMENT
            result.Proboscis.Proboscis.AngleIndex = Random.NextDouble();
            result.Proboscis.Proboscis.Length = Random.Next
            (
                (int)result.Proboscis.Proboscis.LengthMin, 
                (int)result.Proboscis.Proboscis.LengthMax
            );

            //PROBOSCIS - TONGUE
            result.Proboscis.Tongue.AngleIndex = Random.NextDouble();
            result.Proboscis.Tongue.Length = Random.Next
            (
                (int)result.Proboscis.Tongue.LengthMin,
                (int)result.Proboscis.Tongue.LengthMax
            );

            //MANDIBLE
            result.Mandible.AngleIndex = Random.NextDouble();
            result.Mandible.Length = Random.Next
            (
                (int)result.Mandible.LengthMin,
                (int)result.Mandible.LengthMax
            );

            //ANTENA - LEFT - ROOT
            result.LeftAntena.Root.AngleIndex = Random.NextDouble();
            result.LeftAntena.Root.Length = Random.Next
            (
                (int)result.LeftAntena.Root.LengthMin,
                (int)result.LeftAntena.Root.LengthMax
            );

            //ANTENA - LEFT - TIP
            result.LeftAntena.Tip.AngleIndex = Random.NextDouble();
            result.LeftAntena.Tip.Length = Random.Next
            (
                (int)result.LeftAntena.Tip.LengthMin,
                (int)result.LeftAntena.Tip.LengthMax
            );

            //ANTENA - RIGHT - ROOT
            result.RightAntena.Root.AngleIndex = Random.NextDouble();
            result.RightAntena.Root.Length = Random.Next
            (
                (int)result.RightAntena.Root.LengthMin,
                (int)result.RightAntena.Root.LengthMax
            );

            //ANTENA - RIGHT - TIP
            result.RightAntena.Tip.AngleIndex = Random.NextDouble();
            result.RightAntena.Tip.Length = Random.Next
            (
                (int)result.RightAntena.Tip.LengthMin,
                (int)result.RightAntena.Tip.LengthMax
            );

            return result;
        }
    }
}