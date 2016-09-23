using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Classes;
using SwarmSight.Filters;
using SwarmSight.Hardware;

namespace SwarmSight.VideoPlayer
{
    public class FrameDecoder : Stream
    {
        public VideoProcessorBase Processor;
        public event EventHandler<OnFrameReady> FrameReady;
        public int FrameBufferCapacity = 30; //Max frames to decode ahead
        public int MinimumWorkingFrames = 1; //Don't start processing until this many frames have been decoded
        public FrameBuffer FrameBuffer;

        public bool FramesInBufferMoreThanMinimum
        {
            get { return FrameBuffer.Count > MinimumWorkingFrames; }
        }

        private Frame currentFrame;
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
            FrameBuffer = new FrameBuffer(FrameBufferCapacity, width, height);

            this.width = width;
            this.height = height;
            this.pxFormat = pxFormat;
            stride = Frame.ComputeStride(width, pxFormat);
        }

        private int frameIndex = 0;
        public override void Write(byte[] buffer, int offset, int count)
        {
            bytesLeftToCopy = count;

            while (bytesLeftToCopy > 0)
            {
                //Each frame gets its own image buffer
                if (roomInBuffer <= 0)
                {
                    //If buffer's full, wait till it drops to mostly empty
                    if (FrameBuffer.Count >= FrameBufferCapacity)
                        while (FrameBuffer.Count > MinimumWorkingFrames)
                            Thread.Sleep(5);

                    var bmpBufferLength = height*stride;
                    watch = Stopwatch.StartNew();

                    //if (false)//GPU.UseGPU)
                    //    bmpBuffer = GPU.Current.Allocate<byte>(bmpBufferLength);
                    //else

                    FrameBuffer.MakeLastAvailable();
                    currentFrame = FrameBuffer.Last;
                    currentFrame.Reset();
                    currentFrame.Watch = watch;
                    bmpBuffer = currentFrame.PixelBytes;

                    roomInBuffer = bmpBufferLength;
                    bufferOffset = 0;
                }

                var bytesToCopy = bytesLeftToCopy <= roomInBuffer ? bytesLeftToCopy : roomInBuffer;

                //if (false)//GPU.UseGPU)
                //    GPU.Current.CopyToDevice(buffer, offset, bmpBuffer, bufferOffset, bytesToCopy);
                //else
                    Buffer.BlockCopy(buffer, offset, bmpBuffer, bufferOffset, bytesToCopy);

                roomInBuffer -= bytesToCopy;
                bufferOffset += bytesToCopy;
                bytesLeftToCopy -= bytesToCopy;

                if (roomInBuffer > 0)
                    return;
                
                //Frame is filled, continue on to next frame
                try
                {
                    if (Processor != null)
                        currentFrame = Processor.OnAfterDecoding(currentFrame);
                        
                    frameIndex++;
                }
                catch (ThreadAbortException)
                {
                }

                if (FrameReady != null)
                    FrameReady(this, new OnFrameReady() {Frame = currentFrame});

                if (bytesLeftToCopy > 0)
                {
                    offset = bytesToCopy;
                }
            }
        }

        public void ClearBuffer()
        {
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
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
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