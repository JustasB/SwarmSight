using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwarmVision.Filters;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace UnitTests.Hardware.GPU
{
    [TestClass]
    public class KernelsTests
    {
        public KernelsTests()
        {

        }

        [TestMethod]
        public void TestMethod1()
        {
            var bmpA = new Bitmap(3, 3, PixelFormat.Format24bppRgb);
            var bmpB = new Bitmap(3, 3, PixelFormat.Format24bppRgb);

            bmpA.SetPixel(0, 0, Color.FromArgb(200, 200, 200));
            bmpA.SetPixel(1, 1, Color.FromArgb(200, 200, 200));
            bmpA.SetPixel(2, 2, Color.FromArgb(200, 200, 200));

            var frameA = new Frame(bmpA, true);
            var frameB = new Frame(bmpB, true);

            var gpu = SwarmVision.Hardware.GPU.Current;
            var dev_locations = gpu.CopyToDevice(new int[1, 2]);

            var blockPxAvgs = new int[1, 1, 1];
            var dev_blockPxAvgs = gpu.Allocate<int>(1, 1, 1);

            gpu.Launch
            (
                SwarmVision.Hardware.Filters.Grid(frameA.Width, frameA.Height), SwarmVision.Hardware.Filters.Block, SwarmVision.Hardware.Kernels.RepeatedAverageColorDifferenceKernel,
                frameB.PixelBytes, frameB.Stride, frameB.Width, frameB.Height,
                frameA.PixelBytes, frameA.Stride, frameA.Width, frameA.Height, 0,
                frameA.Stride, frameA.Width, frameA.Height,
                dev_locations, dev_blockPxAvgs
            );

            gpu.CopyFromDevice(dev_blockPxAvgs, blockPxAvgs);

            Assert.IsTrue(blockPxAvgs.Cast<int>().Sum() > 0);
        }

    }
}
