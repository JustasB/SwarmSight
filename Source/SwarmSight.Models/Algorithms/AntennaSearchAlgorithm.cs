using System;
using System.Collections.Generic;
using System.Linq;
using SwarmSight.Filters;
using SwarmSight.HeadPartsTracking.Models;
using SwarmSight.Hardware;

namespace SwarmSight.HeadPartsTracking.Algorithms
{
    public abstract class AntennaSearchAlgorithm : GeneticAlgoBase<HeadModel>
    {
        protected double HeadScale;
        protected double HeadRotationAngle;

        protected AntennaSearchAlgorithm()
        {
            GenerationSize = AntennaAndPERDetector.Config.AntennaGenerationSize;
            NumberOfGenerations = AntennaAndPERDetector.Config.AntennaGenerations;
        }

        public override void PreProcessTarget()
        {
            //Subtract head from the target
            //var plainHead = new HeadView(new HeadModel { Angle = { Value = HeadRotationAngle }, Scale = { Value = HeadScale } }).Draw(GPU.UseGPU);

            //Target.ColorData = Target.ColorData.Subtract(plainHead, 0, 0);
            //Target.MotionData = Target.MotionData.Subtract(plainHead, 0, 0);
            //Target.ShapeData = Target.ShapeData.Subtract(plainHead, 0, 0);


        }

        public void SetScaleAndRotation(double angle, double scale)
        {
            HeadScale = scale;
            HeadRotationAngle = angle;
        }

        public void Reset()
        {
            Generation.Clear();
        }

        protected override bool ValidChild(HeadModel child)
        {
            //Perform any checks on generated children
            return true;
        }

        protected override HeadModel SelectLocation()
        {
            //var aveX = (int)Generation.Average(i => i.Key.Origin.X);
            //var aveY = (int)Generation.Average(i => i.Key.Origin.Y);
            ////var aveWidth = (int)Generation.Average(i => ((Frame)(i.Key.View)).Width);
            ////var aveHeight = (int)Generation.Average(i => ((Frame)(i.Key.View)).Height);
            //var aveAngleIndex = Generation.Average(i => i.Key.Angle.Index);

            //var result = new HeadModel { Origin = new Point(aveX, aveY), AngleIndex = aveAngleIndex };

            ////Draw all
            //foreach (var i in Generation)
            //{
            //    var model = i.Key;

            //    using (var view = CreateHeadView(model))
            //        Target.DrawFrame(view, (int)model.Origin.X, (int)model.Origin.Y);
            //}

            //Draw top
            //var allAntennae = Generation.Take(5).Select(i => i.Key.RightAntena).ToList();



            ////Create frame grabs of head location
            //using (var view = CreateHeadView(top))
            //using (var bmp = Frame.FromBitmap(Target.Bitmap.Clone(new Rectangle((int)top.Origin.X, (int)top.Origin.Y, view.Width, view.Height), PixelFormat.Format24bppRgb)))
            //using (var normalized = bmp.RotateScale(-top.Angle, 1.0 / top.Scale))
            //{
            //    normalized.Bitmap.Save("Y:\\bees\\" + frameIndex + ".bmp", ImageFormat.Bmp);
            //    frameIndex++;
            //}

            ////Detect PER
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


            //return new HeadModel() { RightAntena = AverageAntena(allAntennae) };
            return Generation.First().Key;
        }
        

        public override void ComputeFitness()
        {
            //if (individual.View == null)
            //{
            //    individual.View = new HeadView(individual).Draw(GPU.UseGPU);
            //}

            //var shapeOverlap = Target.ShapeData.CompareOveralp((Frame)individual.View, 0, 0);
            //var motionOverlap = Target.MotionData.CompareOveralp((Frame)individual.View, 0, 0);
            //var colorOverlap = Target.ColorData.CompareOveralp((Frame)individual.View, 0, 0);

            ////Max possible 3.0, and adjust for motion being counted twice
            //return 1 - (shapeOverlap * AntennaAndPERDetector.Config.ShapeWeight +
            //    motionOverlap * AntennaAndPERDetector.Config.MotionWeight +
            //    colorOverlap * AntennaAndPERDetector.Config.ColorWeight) / 3;

        }
    }
}