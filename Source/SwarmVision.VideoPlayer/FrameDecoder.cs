using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Classes;
using SwarmVision.Filters;
using SwarmVision.Hardware;

namespace SwarmVision.VideoPlayer
{
    public class FrameDecoder : Stream
    {
        public VideoProcessorBase Processor;
        public event EventHandler<OnFrameReady> FrameReady;
        public int FrameBufferCapacity = 50; //Max frames to decode ahead
        public int MinimumWorkingFrames = 1; //Don't start processing until this many frames have been decoded
        public LinkedList<Frame> FrameBuffer = new LinkedList<Frame>();

        public bool FramesInBufferMoreThanMinimum
        {
            get { return FrameBuffer.Count > MinimumWorkingFrames; }
        }

        private long _length;
        private byte[] bmpBuffer;
        private int bufferOffset;
        private int roomInBuffer = 0;
        private int bytesLeftToCopy = 0;
        private int width;
        private int height;
        private int stride;
        private PixelFormat pxFormat;
        private Stopwatch watch = new Stopwatch();

        public FrameDecoder(int width, int height, PixelFormat pxFormat)
        {
            this.width = width;
            this.height = height;
            this.pxFormat = pxFormat;
            stride = GetStride(width, pxFormat);


        }

        private static int GetStride(int width, PixelFormat pxFormat)
        {
            //var bitsPerPixel = ((int) pxFormat >> 8) & 0xFF;

            ////Number of bits used to store the image data per line (only the valid data)
            //var validBitsPerLine = width*bitsPerPixel;

            ////4 bytes for every int32 (32 bits)
            //var result = ((validBitsPerLine + 31)/32)*4;

            //Create a bitmap 1px tall with the width, and get it's stride
            using(var frame = new Frame(new Bitmap(width, 1, pxFormat), false))
                return frame.Stride;
        }

        private int frameIndex = 0;
        public override void Write(byte[] buffer, int offset, int count)
        {
            bytesLeftToCopy = count;

            while (bytesLeftToCopy > 0)
            {
                //Each frame gets its own image buffer
                if (bmpBuffer == null)
                {
                    var bmpBufferLength = height*stride;
                    watch = Stopwatch.StartNew();

                    if (false)//GPU.UseGPU)
                        bmpBuffer = GPU.Current.Allocate<byte>(bmpBufferLength);
                    else
                        bmpBuffer = new byte[bmpBufferLength];

                    roomInBuffer = bmpBufferLength;
                    bufferOffset = 0;
                }

                var bytesToCopy = bytesLeftToCopy <= roomInBuffer ? bytesLeftToCopy : roomInBuffer;

                if (false)//GPU.UseGPU)
                    GPU.Current.CopyToDevice(buffer, offset, bmpBuffer, bufferOffset, bytesToCopy);
                else
                    Buffer.BlockCopy(buffer, offset, bmpBuffer, bufferOffset, bytesToCopy);

                roomInBuffer -= bytesToCopy;
                bufferOffset += bytesToCopy;
                bytesLeftToCopy -= bytesToCopy;

                if (roomInBuffer > 0)
                    return;

                //Buffer is full, image is ready. Retain the bitmap data
                var frame = new Frame(width, height, stride, pxFormat, bmpBuffer, false /*GPU.UseGPU*/) {Watch = watch};

                //Release the image buffer (after it has been stored above)
                bmpBuffer = null;

                //If buffer's full, wait till it drops to mostly empty
                if (FrameBuffer.Count >= FrameBufferCapacity)
                    while (FrameBuffer.Count > MinimumWorkingFrames)
                        Thread.Sleep(5);

                //Once there is room, add frames
                try
                {
                    if (Processor != null)
                        frame = Processor.OnAfterDecoding(frame);

                    FrameBuffer.AddLast(frame);

                    frameIndex++;
                }
                catch (ThreadAbortException)
                {
                }

                if (FrameReady != null)
                    FrameReady(this, new OnFrameReady() {Frame = frame});

                if (bytesLeftToCopy > 0)
                {
                    offset = bytesToCopy;
                }
            }
        }

        public void ClearBuffer()
        {
            foreach (var frame in FrameBuffer)
            {
                frame.Dispose();
            }

            FrameBuffer.Clear();
        }

        #region OtherInterfaceMembers

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position
        {
            get { return _length; }
            set { throw new NotImplementedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}