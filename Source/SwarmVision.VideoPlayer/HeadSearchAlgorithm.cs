using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using AForge.Neuro;
using SwarmVision.Filters;
using SwarmVision.Models;
using Point = System.Windows.Point;
using SwarmVision.Hardware;

namespace SwarmVision.VideoPlayer
{
    public class HeadSearchAlgorithm : GeneticAlgoBase<HeadModel>
    {
        //private Network neuralNet;

        public HeadSearchAlgorithm()
        {
            //neuralNet = Network.Load(@"c:\temp\PER.net");
            MinGenerationSize = AntennaAndPERDetector.Config.HeadGenerationSize;
            NumberOfGenerations = AntennaAndPERDetector.Config.HeadGenerations;
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

            return result;
        }

        protected override bool ValidChild(HeadModel child)
        {
            return Target.ShapeData.ValidConvolutionLocation(
                HeadView.Width, 
                HeadView.Height, 
                (int)child.Origin.X, 
                (int)child.Origin.Y, 
                child.Angle
            );
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
            //    Target.DrawFrame(view, (int)top.Origin.X, (int)top.Origin.Y);
            //}


            ////Create frame grabs of head location
            //using (var view = CreateHeadView(top))
            //using (var bmp = Frame.FromBitmap(Target.Bitmap.Clone(new Rectangle((int)top.Origin.X, (int)top.Origin.Y, view.Width, view.Height), PixelFormat.Format24bppRgb)))
            //using (var normalized = bmp.RotateScale(-top.Angle, 1.0 / top.Scale))
            //{
            //    normalized.Bitmap.Save("Y:\\bees\\" + frameIndex + ".bmp", ImageFormat.Bmp);
            //    frameIndex++;
            //}

            ////DETECT PER
            //using (var view = CreateHeadView(top))
            //using (var bmp = Frame.FromBitmap(Target.Bitmap.Clone(new Rectangle((int)top.Origin.X, (int)top.Origin.Y, view.Width, view.Height), PixelFormat.Format24bppRgb)))
            //using (var normalized = bmp.RotateScale(-top.Angle, 1.0 / top.Scale))
            //using (var resized = normalized.Resize(10, 10))
            //{
            //    Target.DrawFrame(view, (int)top.Origin.X, (int)top.Origin.Y);

            //    var nnInput = resized.ToAccordInput();
            //    var output = neuralNet.Compute(nnInput)[0];

            //    Target.DrawRectangle(0, 0, (int)(100 * output + 10), 10);
            //}


            return top;
        }

        public override void ComputeFitness()
        {
            var uncomputed = Generation
                .Where(pair => pair.Value == InitialFitness)
                .Select(pair => pair.Key)
                .ToArray();

            foreach (var individual in uncomputed)
            {
                if (individual.View == null)
                {
                    individual.View = new HeadView(individual).Draw(GPU.UseGPU);
                }
            }

            for (var i = 0; i < uncomputed.Length; i++)
            {
                var individual = uncomputed[i];

                var result = Target.ShapeData.AverageColorDifference((Frame)individual.View, (int)individual.Origin.X, (int)individual.Origin.Y, i+1);

                //Adjust for scale
                result /= individual.Scale;

                Generation[individual] = result;
            } 

        }

        protected override HeadModel CreateNewRandomMember()
        {
            //HEAD
            var result = new HeadModel
            {
                Origin = new Point
                (
                    Random.Next(0, Target.ShapeData.Width - HeadView.Width),
                    Random.Next(0, Target.ShapeData.Height - HeadView.Height)
                ),
                AngleIndex = Random.NextDouble(),
            };

            result.Scale = Random.NextDouble()*(result.ScaleMax - result.ScaleMin) + result.ScaleMin;

            return result;
        }
    }
}