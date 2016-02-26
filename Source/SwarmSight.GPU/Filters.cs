using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cudafy;
using Cudafy.Host;
using Cudafy.Translator;

namespace SwarmSight.Hardware
{
    public class Filters
    {
        public const double BlockSideLength = 16;
        public static readonly dim3 Block = new dim3((int)BlockSideLength, (int)BlockSideLength);

        public static dim3 Grid(int width, int height)
        {
            return new dim3((int)Math.Ceiling(width / BlockSideLength), (int)Math.Ceiling(height / BlockSideLength));
        }

        public static byte[] RotateScale(byte[] target, int stride, int width, int height, Color bgColor, double angle, double scale = 1.0)
        {
            var devResult = GPU.Current.Allocate<byte>(target.Length);
            var hostResult = new byte[target.Length];
            var devTarget = GPU.Current.CopyToDevice(target);
            
            //Parallel.For(0, height, y =>
            //{
            //    for (var x = 0; x < width; x++)
            //        rotateScaleKERNEL(new GThread(x, y, new GBlock(new GGrid(new dim3(1, 1, 1)), new dim3(width, height, 1), 0, 0))
            //            , hostResult, target, stride, width, height, (float)(angle / 180.0 * Math.PI), (float)scale);
            //});

            GPU.Current.Launch
            (
                new dim3((int)Math.Ceiling(width / 16.0), (int)Math.Ceiling(height / 16.0)), new dim3(16, 16),
                "rotateScaleKERNEL", devResult, devTarget, stride, width, height, (float)(angle / 180.0 * Math.PI), (float)scale
            );

            GPU.Current.CopyFromDevice(devResult, hostResult);

            GPU.Current.Free(devResult);
            GPU.Current.Free(devTarget);

            return hostResult;
        }

        public static void DrawLineSegments(byte[] target, int stride, int width, int height, LineSegment[] segments)
        {
            if(segments.Length > 100)
                throw new Exception("Only 100 segments are supported per call");

            var SegmentCache = new KernelLineSegment[100];

            var kernelSegments = new KernelLineSegment[segments.Length];

            //Precompute line variables
            for(var s = 0; s < segments.Length; s++)
                kernelSegments[s] = segments[s].ToKernelLineSegment();

            ////CPU
            //SegmentCache = kernelSegments;
            //Parallel.For(0, height, y =>
            //{
            //    for (var x = 0; x < width; x++)
            //        DrawLinesKernel(new GThread(x, y, new GBlock(new GGrid(new dim3(1, 1, 1)), new dim3(width, height, 1), 0, 0)),
            //             target, stride, width, height, segments.Length);
            //});

            //GPU
            SegmentCache = GPU.Current.CopyToDevice(kernelSegments);
            var devTarget = GPU.Current.CopyToDevice(target);
            GPU.Current.Launch
            (
                new dim3((int)Math.Ceiling(width / 16.0), (int)Math.Ceiling(height / 16.0)), new dim3(16, 16),
                "DrawLinesKernel", devTarget, stride, width, height, SegmentCache, segments.Length
            );
            GPU.Current.CopyFromDevice(devTarget, target);
            GPU.Current.Free(devTarget);
        }

        public static void RunEmptyKernel(bool allocateDeviceMem = false, bool copyToDevice = false, bool copyFromDevice = false)
        {
            var memSize = 1024 * 768 * 3;

            byte[] devResult = null;
            var gpu = GPU.Current;

            if (allocateDeviceMem)
                devResult = gpu.Allocate<byte>(memSize);

            byte[] hostResult = null;

            if (allocateDeviceMem && copyToDevice)
            { 
                hostResult = new byte[memSize];
                gpu.CopyToDevice(hostResult, devResult);
            }

            gpu.Launch(Grid(1024, 768), Block, Kernels.EmptyKernel, 0);

            if (copyToDevice && copyFromDevice)
                gpu.CopyFromDevice(devResult, hostResult);

            if(allocateDeviceMem)
                gpu.Free(devResult);
            hostResult = null;
        }
    }
}
