using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwarmSight.Filters;
using SwarmSight.HeadPartsTracking.Algorithms;
using SwarmSight.HeadPartsTracking.Models;

namespace Testing
{
    [TestClass]
    public class AntenaAndPERTests
    {
        [TestMethod]
        public void TestPatternAverageColorDifference()
        {
            var target = new Frame("../../testTarget.bmp", false);
            var pattern = new Frame("../../testPattern.bmp", false);

            var selfResult = target.AverageColorDifference(target, 0, 0);
            var perfectMatch = target.AverageColorDifference(pattern, 0, 4);
            var twoDIff = target.AverageColorDifference(pattern, 2, 0);
            var threeFiff = target.AverageColorDifference(pattern, 2, 4);

            Assert.AreEqual(selfResult, 0);
            Assert.AreEqual(perfectMatch, 0);
            Assert.IsTrue(twoDIff == 7225*2);
            Assert.IsTrue(threeFiff == 7225*3);
        }

        [TestMethod]
        public void TestFitnessMap()
        {
            var headFinder = new HeadSearchAlgorithm();
            var frame = new Frame("../../testFrameWithHeadEdged.bmp", false);
            headFinder.SetTarget(new FrameCollection { ShapeData = frame });
            var rotatedHead = new HeadModel() {Angle = {Value = 15}, ScaleX = {Value = 1.03}};
            var headView = rotatedHead.GenerateView(false);
            var fitnessMap = new Frame(new Bitmap(headView.Width,headView.Height,PixelFormat.Format24bppRgb), false);

            for (var y = 0; y < fitnessMap.Height; y++)
            for (var x = 0; x < fitnessMap.Width;  x++)
            {
                rotatedHead.Origin = new System.Drawing.Point(x,y);
                var fitness = Math.Log(headFinder.ComputeFitness(rotatedHead));
                var address = 3*x + y*fitnessMap.Stride;

                fitnessMap.PixelBytes[address] = 
                        fitnessMap.PixelBytes[address + 1] = 
                        fitnessMap.PixelBytes[address + 2] = (byte)(Math.Pow(fitness, 2));
            }
            

        }

        [TestMethod]
        public void FindHeadInTestFrame()
        {
            var headFinder = new HeadSearchAlgorithm();
            var frame = new Frame("../../testFrameWithHeadEdged.bmp", false);
            var headLocation = headFinder.Search(new FrameCollection {ShapeData = frame});


            Assert.AreEqual(headLocation.Angle.Value, 13, 5);
            Assert.AreEqual(headLocation.ScaleX.Value, 1, 0.1);
            Assert.AreEqual(headLocation.Origin.X, 21);
            Assert.AreEqual(headLocation.Origin.X, 24);
        }
    }
}
