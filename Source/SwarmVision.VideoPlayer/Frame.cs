using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SwarmVision.VideoPlayer
{
    public unsafe class Frame : IDisposable
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Stride { get; private set; }
        public PixelFormat PixelFormat { get; private set; }
        public byte[] PixelBytes { get; private set; }
        public byte* FirstPixelPointer;

        public int FrameIndex { get; set; }
        public double FramePercentage { get; set; }
        public TimeSpan FrameTime { get; set; }
        public bool IsDecoded { get; set; }
        public Stopwatch Watch;

        public Bitmap Bitmap
        {
            get { return new Bitmap(Width, Height, Stride, PixelFormat, addr); }
        }

        //------//

        private GCHandle bmpHandle;
        private IntPtr addr;

        public Frame(int width, int height, int stride, PixelFormat format, byte[] bytes)
        {
            Width = width;
            Height = height;
            Stride = stride;
            PixelFormat = format;
            PixelBytes = bytes;

            bmpHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            addr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
            FirstPixelPointer = (byte*) addr.ToPointer();
        }

        public void Dispose()
        {
            if (bmpHandle.IsAllocated)
                bmpHandle.Free();

            PixelBytes = null;
        }

        public Frame Clone()
        {
            return new Frame(Width, Height, Stride, PixelFormat, (byte[]) PixelBytes.Clone())
                {
                    FrameIndex = FrameIndex,
                    FramePercentage = FramePercentage,
                    FrameTime = FrameTime,
                    Watch = Watch,
                };
        }
    }
}