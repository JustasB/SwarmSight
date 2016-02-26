using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SwarmSight.Hardware;

namespace SwarmSight.Filters
{
    public unsafe class Frame : IDisposable
    {
        public bool IsOnGPU { get; private set; }
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

        public int PixelBytesLength
        {
            get { return Stride * Height; }
        }

        //------//

        private GCHandle pixelBytesHandle;
        private IntPtr firstPixelAddress;
        private IntPtr host_bytes;
        private Bitmap sourceBmp;
        private BitmapData sourceBmpLockedData;

        public Bitmap Bitmap
        {
            get
            {
                if(IsOnGPU)
                {
                    CopyFromGPU();

                    return new Bitmap(Width, Height, Stride, PixelFormat, host_bytes);
                }

                return new Bitmap(Width, Height, Stride, PixelFormat, firstPixelAddress);
            }
        }

        public Frame(int width, int height, PixelFormat format, bool storeOnGPU) 
        {
            if (!storeOnGPU)
            {
                var stride = ComputeStride(width, format);
                var bytes = new byte[stride*height];

                Initialize(width, height, stride, format, bytes, false);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public Frame(int width, int height, int stride, PixelFormat format, byte[] bytes, bool storeOnGPU)
        {
            Initialize(width, height, stride, format, bytes, storeOnGPU);
        }

        public Frame(string bitmapPath, bool storeOnGPU) : this(Image.FromFile(bitmapPath) as Bitmap, storeOnGPU)
        {

        }

        public Frame(Bitmap bitmap, bool storeOnGPU)
        {
            //Copy bitmap bytes to managed array
            BitmapData bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int numbytes = bmpdata.Stride * bitmap.Height;
            byte[] bytedata = new byte[numbytes];
            IntPtr ptr = bmpdata.Scan0;
            Marshal.Copy(ptr, bytedata, 0, numbytes);
            bitmap.UnlockBits(bmpdata);

            Initialize(bitmap.Width, bitmap.Height, bmpdata.Stride, bmpdata.PixelFormat, bytedata, storeOnGPU);

            bitmap.Dispose();
        }

        public void Initialize(int width, int height, int stride, PixelFormat format, byte[] bytes, bool storeOnGPU)
        {
            IsOnGPU = storeOnGPU;
            Width = width;
            Height = height;
            Stride = stride;
            PixelFormat = format;


            if (!IsOnGPU)
            {
                //Pin the bytes
                PixelBytes = bytes;
                pixelBytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                firstPixelAddress = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
                FirstPixelPointer = (byte*)firstPixelAddress.ToPointer();
            }
            else
            {
                //Host array
                if (bytes.Length != 0)
                {
                    PixelBytes = GPU.Current.CopyToDevice(bytes); //Copy the array to GPU
                    bytes = null; //Discard host data
                }

                //Device array
                else
                    PixelBytes = bytes;
            }
        }

        private void CopyFromGPU()
        {
            if (!IsOnGPU)
                return;

            //Release any previously pinned memory
            if (host_bytes != IntPtr.Zero)
                GPU.Current.HostFree(host_bytes);

            //Pin new memory
            host_bytes = GPU.Current.HostAllocate<byte>(Height * Stride);

            //Copy from GPU
            GPU.Current.CopyFromDevice(PixelBytes, 0, host_bytes, 0, Height * Stride);
        }

        public void Dispose()
        {
            if (!IsOnGPU)
            {
                //Free pinned mem
                if (pixelBytesHandle.IsAllocated)
                    pixelBytesHandle.Free();

                firstPixelAddress = IntPtr.Zero;

                PixelBytes = null;
            }
            else
            {
                //Discard GPU bytes
                if(PixelBytes != null)
                    GPU.Current.Free(PixelBytes);

                //Discard any bytes that were copied from GPU
                if (host_bytes != IntPtr.Zero)
                {
                    GPU.Current.HostFree(host_bytes);
                    host_bytes = IntPtr.Zero;
                }
            }

            PixelBytes = null;
        }

        /// <summary>
        /// Dispose of any forgotten handles
        /// </summary>
        ~Frame()
        {
            try
            {
                if (pixelBytesHandle != null && pixelBytesHandle.IsAllocated)
                    pixelBytesHandle.Free();
            }
            catch { }
        }

        public Frame Clone()
        {
            byte[] clonedBytes;

            if(IsOnGPU)
            {
                clonedBytes = GPU.Current.Allocate<byte>(PixelBytesLength);
                GPU.Current.CopyOnDevice(PixelBytes, clonedBytes);
            }
            else
            {
                clonedBytes = (byte[])PixelBytes.Clone();
            }

            var result = new Frame(Width, Height, Stride, PixelFormat, clonedBytes, IsOnGPU);
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

        public Color GetColor(Point point)
        {
            var offset = point.Y*Stride + point.X*3;

            return Color.FromArgb(
                FirstPixelPointer[offset + 2],
                FirstPixelPointer[offset + 1],
                FirstPixelPointer[offset    ]
            );
        }

        public static int ComputeStride(int width, PixelFormat pxFormat)
        {
            var bitsPerPixel = ((int)pxFormat >> 8) & 0xFF;

            //Number of bits used to store the image data per line (only the valid data)
            var validBitsPerLine = width * bitsPerPixel;

            //4 bytes for every int32 (32 bits)
            return ((validBitsPerLine + 31) / 32) * 4;
        }
    }
}