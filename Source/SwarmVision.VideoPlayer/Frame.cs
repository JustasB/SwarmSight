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
            var result = new Frame(Width, Height, Stride, PixelFormat, (byte[]) PixelBytes.Clone());

            result.ShalowCopy(this);

            return result;
        }

        public void ShalowCopy(Frame source)
        {
            FrameIndex = source.FrameIndex;
            FramePercentage = source.FramePercentage;
            FrameTime = source.FrameTime;
            Watch = source.Watch;
            IsDecoded = source.IsDecoded;
        }

        public Frame Clear()
        {
            var length = PixelBytes.GetLongLength(0);

            for (var i = 0; i < length; i++)
            {
                FirstPixelPointer[i] = 0;
            }

            return this;
        }

        public static Frame FromBitmap(string bitmapPath)
        {
            var bitmap = Image.FromFile(bitmapPath) as Bitmap;

            return FromBitmap(bitmap);
        }

        public static Frame FromBitmap(Bitmap bitmap)
        {
            //Get bitmap bytes
            BitmapData bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int numbytes = bmpdata.Stride * bitmap.Height;
            byte[] bytedata = new byte[numbytes];
            IntPtr ptr = bmpdata.Scan0;
            Marshal.Copy(ptr, bytedata, 0, numbytes);
            bitmap.UnlockBits(bmpdata);

            return new Frame(bitmap.Width, bitmap.Height, bmpdata.Stride, bmpdata.PixelFormat, bytedata);
        }

        public void SetMonochromePixel(int x, int y, int whiteness)
        {
            if (x >= Width || x < 0 || y >= Height || y < 0)
                return;

            var offset = Stride*y + x*3;

            FirstPixelPointer[offset] = FirstPixelPointer[offset + 1] = FirstPixelPointer[offset + 2] = 
                (byte)(Math.Min(255, whiteness));
        }
    }
}