using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cudafy;

namespace SwarmSight.Hardware
{
    [Cudafy(eCudafyType.Struct)]
    public struct KernelLineSegment
    {
        public float StartX;
        public float StartY;
        public float Dy;
        public float Dx;
        public float Product;
        public float Length;
        public float Thickness;
        public float ColorB;
        public float ColorG;
        public float ColorR;
    }
}
