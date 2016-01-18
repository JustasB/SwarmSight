using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using AForge.Neuro;
using Accord.MachineLearning.VectorMachines;
using SwarmVision.Filters;
using SwarmVision.HeadPartsTracking.Models;
using Point = System.Windows.Point;
using SwarmVision.Hardware;
using Cudafy;
using System.Threading.Tasks;

namespace SwarmVision.HeadPartsTracking.Algorithms
{
    public class HeadSearchAlgorithm : GeneticAlgoBase<HeadModel>
    {
        //private Network neuralNet;

        public HeadSearchAlgorithm()
        {
            //neuralNet = Network.Load(@"c:\temp\PER.net");
            GenerationSize = AntennaAndPERDetector.Config.HeadGenerationSize;
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
                Angle = Cross(parent1.Angle, parent2.Angle)
            };

            result.ScaleX = Cross(parent1.ScaleX, parent2.ScaleX);
            result.ScaleY = Cross(parent1.ScaleY, parent2.ScaleY);

            return result;
        }

        public override HeadModel Mutated(HeadModel individual)
        {
            var result = new HeadModel();

            result.Origin = new Point
            (
                MutateValue(individual.Origin.X, 0, Target.ShapeData.Width - HeadView.Width),
                MutateValue(individual.Origin.Y, 0, Target.ShapeData.Height - HeadView.Height)
            );

            result.Angle = MutateValue(individual.Angle);
            result.ScaleX = MutateValue(individual.ScaleX);
            result.ScaleY = MutateValue(individual.ScaleY);

            return result;
        }

        public void Reset()
        {
            Generation.ToList().ForEach(pair => pair.Key.Dispose());
            Generation = new Dictionary<HeadModel, double>();
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

            //Debug.WriteLine("Fitness: " + Generation[top]);
            
            BestRecentFitness = Generation[top];

            return top;
        }

        public override void ComputeFitness()
        {
            var uncomputed = Generation
                .Where(pair => pair.Value == InitialFitness)
                .Select(pair => pair.Key)
                .ToList();

            double[] fitnesResults;
            
            if(GPU.UseGPU)
                fitnesResults = ComputeFitnessGPU(Target.ShapeData, uncomputed);

            else
            {
                fitnesResults = uncomputed
                    .AsParallel()
                    .Select(i => ComputeFitness(i))
                    .ToArray();
            }

            for (var i = 0; i < uncomputed.Count; i++)
            {
                Generation[uncomputed[i]] = fitnesResults[i];
            }
        }
        
        public static double[] ComputeFitnessGPU(Frame target, List<HeadModel> list)
        {
            var count = list.Count;
            var plainHead = HeadView.PlainHeadGPU;
            var sizeOfHead = plainHead.PixelBytesLength;
            var gpu = GPU.Current;
            var headGrid = Hardware.Filters.Grid(plainHead.Width, plainHead.Height);
            var block = Hardware.Filters.Block;
            var pOps = new ParallelOptions() { MaxDegreeOfParallelism = 1 };
            var results = new double[list.Count];

            //Allocate all heads 2x (half for upright, half for rotated)
            var sizeOfWorkBuffer = sizeOfHead * count * 2;
            var dev_heads = gpu.Allocate<byte>(sizeOfWorkBuffer);

            //Copy plain head over into the first half

            //try4: parallel copy 5.5ms
            gpu.Launch
            (
                Hardware.Filters.Grid(plainHead.Width * count, plainHead.Height), block, Kernels.RepeatCopyKernelParallel,
                plainHead.PixelBytes, plainHead.Stride, plainHead.Width, plainHead.Height,
                dev_heads, sizeOfWorkBuffer / plainHead.Height, plainHead.Width * count, plainHead.Height
            );

            ////try3: cache the copying 6.2ms
            //gpu.Launch
            //(
            //    grid, block, Kernels.RepeatCopyKernel,
            //    plainHead.PixelBytes, plainHead.Stride, plainHead.Width, plainHead.Height,
            //    dev_heads, sizeOfWorkBuffer / plainHead.Height, plainHead.Width * count, plainHead.Height
            //);


            //Add segments (skip for now)
            //...

            //Rotate scale async
            var transforms = new float[2,count];
            for (int i = 0; i < count; i++)
            {
                var item = list[i];

                transforms[0,i] = (float)item.Angle.InRadians();
                transforms[1,i] = (float)item.ScaleX; //<-------
                throw new NotImplementedException();
            }

            var dev_transforms = gpu.CopyToDevice(transforms);

            gpu.Launch
            (
                Hardware.Filters.Grid(plainHead.Width * count, plainHead.Height), block, Kernels.RepeatedRotateScaleKernel,
                dev_heads, dev_transforms, 
                plainHead.Stride, plainHead.Width, plainHead.Height,
                sizeOfWorkBuffer / plainHead.Height, plainHead.Width * count, plainHead.Height
            );

            //Compute difference
            var searchLocations = new int[count, 2];
            for (int i = 0; i < count; i++)
            {
                var item = list[i];

                searchLocations[i, 0] = (int)item.Origin.X;
                searchLocations[i, 1] = (int)item.Origin.Y;
            }

            var dev_locations = gpu.CopyToDevice(searchLocations);

            var blockPxAvgs = new int[count, headGrid.x, headGrid.y];
            var dev_blockPxAvgs = gpu.Allocate<int>(count, headGrid.x, headGrid.y);

            gpu.Launch
            (
                Hardware.Filters.Grid(plainHead.Width * count, plainHead.Height), block, Kernels.RepeatedAverageColorDifferenceKernel,
                target.PixelBytes, target.Stride, target.Width, target.Height,
                dev_heads, sizeOfWorkBuffer / plainHead.Height, plainHead.Width * count, plainHead.Height, plainHead.Width * count,
                plainHead.Stride, plainHead.Width, plainHead.Height,
                dev_locations, dev_blockPxAvgs
            );

            gpu.CopyFromDevice(dev_blockPxAvgs, blockPxAvgs);

            //Compute model averages from the block averages
            for (int i = 0; i < count; i++)
            {
                var initAvg = blockPxAvgs[i, 0, 0];

                for (int y = 0; y < headGrid.y; y++)
                    for (int x = 0; x < headGrid.x; x++)
                        blockPxAvgs[i, 0, 0] += blockPxAvgs[i, x, y];

                //Subtract twice-added initial value
                blockPxAvgs[i, 0, 0] -= initAvg;

                results[i] = (blockPxAvgs[i, 0, 0] / (plainHead.Width * plainHead.Height)) / list[i].ScaleX; //<----
                throw new NotImplementedException();
            }

            //var t = new Frame(plainHead.Width * count * 2, plainHead.Height, sizeOfWorkBuffer / plainHead.Height, plainHead.PixelFormat, dev_heads, true);

            //Free heads
            gpu.Free(dev_blockPxAvgs);
            gpu.Free(dev_locations);
            gpu.Free(dev_heads);
            gpu.Free(dev_transforms);

            return results;
        }

        KernelSupportVectorMachine svm = KernelSupportVectorMachine.Load(@"c:\temp\head.svm");
        public double ComputeFitness(HeadModel individual)
        {
            //if (individual.View == null)
            //    individual.View = individual.GenerateView(false);

            using (var underHead = Target.ShapeData.SubClipped((int)individual.Origin.X, (int)individual.Origin.Y, HeadView.Width, HeadView.Height))
            using (var deRotSc = underHead.RotateScale(-individual.Angle, 1 / individual.ScaleX, 1 / individual.ScaleY))
            using (var head = deRotSc.SubClipped(
                HeadView.HeadOffsetX,
                HeadView.HeadOffsetY, 
                HeadView.HeadWidth, 
                HeadView.HeadHeight
            ))
            using(var scaledDown = head.ScaleHQ(10,10))
            {
                var svmOut = svm.Compute(scaledDown.ToAccordInput());

                return svmOut+6;
            }


            //var result = Target.ShapeData.AverageColorDifference(
            //    individual.View, 
            //    (int)individual.Origin.X, 
            //    (int)individual.Origin.Y);

            ////Adjust for scale
            //result /= (individual.ScaleX*individual.ScaleY);// +(individual.ScaleY * individual.ScaleY);

            //return result;
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
            };

            result.Angle.Index = Random.NextDouble();
            result.ScaleX.Value = Random.NextDouble()*(result.ScaleX.Max - result.ScaleX.Min) + result.ScaleX.Min;
            result.ScaleY.Value = Random.NextDouble() * (result.ScaleY.Max - result.ScaleY.Min) + result.ScaleY.Min;

            //result.Proboscis.Root.Length = new MinMaxDouble(0,0,0);
            //result.LeftAntena.Tip.Length = new MinMaxDouble(0, 0, 0);
            //result.RightAntena.Tip.Length = new MinMaxDouble(0, 0, 0);

            return result;
        }
    }
}