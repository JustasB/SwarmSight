using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using Classes;
using SwarmVision.Filters;
using SwarmVision.Hardware;
using SwarmVision.Models;

namespace SwarmVision.VideoPlayer
{
    public class FrameDecoder : Stream
    {
        public VideoProcessorBase Processor;
        public event EventHandler<OnFrameReady> FrameReady;
        public int FrameBufferCapacity = 30; //Max frames to decode ahead
        public int MinimumWorkingFrames = 5; //Don't start processing until this many frames have been decoded
        public LinkedList<Frame> FrameBuffer = new LinkedList<Frame>();

        public bool FramesInBufferMoreThanMinimum
        {
            get { return FrameBuffer.Count > MinimumWorkingFrames; }
        }

        private long _length;
        private byte[] bmpBuffer;
        private int bmpBufferIndex;
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
            var bitsPerPixel = ((int) pxFormat >> 8) & 0xFF;

            //Number of bits used to store the image data per line (only the valid data)
            var validBitsPerLine = width*bitsPerPixel;

            //4 bytes for every int32 (32 bits)
            var result = ((validBitsPerLine + 31)/32)*4;

            return result;
        }

        private int frameIndex = 0;
        public override void Write(byte[] buffer, int offset, int count)
        {
            var bmpBufferLength = height * stride;

            //Each frame gets its own image buffer
            if (bmpBuffer == null)
            {
                watch = Stopwatch.StartNew();

                if (GPU.UseGPU)
                    bmpBuffer = GPU.Current.Allocate<byte>(bmpBufferLength);
                else
                    bmpBuffer = new byte[bmpBufferLength];
            }
            while (count > 0)
            {
                var roomInBuffer = bmpBufferLength - bmpBufferIndex;

                if (count < roomInBuffer)
                    roomInBuffer = count;

                if (GPU.UseGPU)
                    GPU.Current.CopyToDevice(buffer, offset, bmpBuffer, bmpBufferIndex, roomInBuffer);
                else
                    Buffer.BlockCopy(buffer, offset, bmpBuffer, bmpBufferIndex, roomInBuffer);

                bmpBufferIndex += roomInBuffer;
                _length += roomInBuffer;
                offset += roomInBuffer;
                count -= roomInBuffer;


                //Buffer is full, image is ready
                if (bmpBufferIndex != bmpBufferLength)
                    continue;

                //Retain the bitmap data
                var frame = new Frame(width, height, stride, pxFormat, bmpBuffer, GPU.UseGPU) {Watch = watch};
                
                //Release the image buffer (after it has been stored above)
                bmpBuffer = null;

                count = 0;


                //If buffer's full, wait till it drops to mostly empty
                if (FrameBuffer.Count >= FrameBufferCapacity)
                    while (FrameBuffer.Count > MinimumWorkingFrames)
                        Thread.Sleep(5);

                //Once there is room, add frames
                try
                {
                    if(Processor != null)
                        frame = Processor.OnAfterDecoding(frame);

                    FrameBuffer.AddLast(frame);

                    frameIndex++;

                }
                catch(ThreadAbortException)
                {
                }

                Debug.Print(new string('D', FrameBuffer.Count));

                bmpBufferIndex = 0;

                if (FrameReady != null)
                    FrameReady(this, new OnFrameReady() {Frame = frame});
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