using System;
using System.Collections.Generic;
using System.Drawing;
using Cudafy;
using System.Linq;

namespace SwarmSight.Hardware
{

    public struct LineSegment
    {
        public Point Start;
        public Point End;
        public int Thickness;
        public Color Color;

        public KernelLineSegment ToKernelLineSegment()
        {
            var dy = (float)End.Y - Start.Y;
            var dx = (float)End.X - Start.X;

            return new KernelLineSegment
            {
                Dy = dy,
                Dx = dx,
                Product = End.X * Start.Y - End.Y * Start.X,
                Length = GMath.Sqrt(dx * dx + dy * dy),
                ColorB = Color.B,
                ColorG = Color.G,
                ColorR = Color.R,
                Thickness = Thickness,
                StartX = Start.X,
                StartY = Start.Y
            };
        }

        public static float[,] ToFloatArray(List<LineSegment> segments)
        {
            var kernelSegments = segments.Select(s => s.ToKernelLineSegment()).ToArray();
            var fields = typeof(KernelLineSegment).GetFields();
            var floatArray = new float[kernelSegments.Length, fields.Length];

            for (int s = 0; s < kernelSegments.Length; s++)
                for (int f = 0; f < fields.Length; f++)
                    floatArray[s, f] = (float)fields[f].GetValue(kernelSegments[s]);

            return floatArray;
        }
    }
}
