using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SwarmSight.Filters;
using System;

namespace SwarmSight.Filters
{
    public class FrameBuffer : IDisposable
    {
        private List<Frame> _buffer;
        private int cap = 0;
        private int iTail = 0;
        public int iFirst = 0;

        public int Count
        {
            get
            {
                lock(_buffer)
                    return iTail - iFirst;
            }
        }

        public FrameBuffer(int capacity, int width, int height)
        {
            cap = capacity;

            _buffer = new List<Frame>(capacity);

            //Pre-allocate frames in the buffer -- these will be recycled
            for (int i = 0; i < capacity; i++)
            {
                _buffer.Add(new Frame(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb, false));
            }
        }
                
        public Frame First
        {
            get
            {
                lock(_buffer)
                {
                    if (Count > 0)
                        return _buffer[iFirst % cap];
                    else
                        throw new System.InvalidOperationException("Buffer count is 0");
                }
            }
        }

        public Frame Get(int pos)
        {
            lock(_buffer)
            {
                if (pos < iFirst || pos + 1 > iTail)
                    return null;

                return _buffer[pos % cap];
            }
        }
        
        public Frame GetNthAfterFirst(int n)
        {
            return Get(iFirst + n);
        }

        public Frame GetNthBeforeLast(int n)
        {
            return Get(iTail - 1 - n);
        }

        public int GetNextPosition(int currPos)
        {
            lock(_buffer)
                return (currPos + 1);
        }

        public Frame Last
        {
            get
            {
                lock (_buffer)
                {
                    if (Count > 0)
                        return _buffer[(iTail - 1) % cap];
                    else
                        throw new System.InvalidOperationException("Buffer count is 0");
                }
            }
        }

        public Frame MakeLastAvailable()
        {
            lock(_buffer)
            {
                if (Count >= cap)
                    throw new System.InvalidOperationException("Exceeded buffer capacity");
                    
                iTail++;
                
                Last.Reset();

                if (Count < 0 || Count > cap)
                    throw new InvalidOperationException("Invalid buffer index detected");

                return Last;
            }
        }

        public void RemoveFirst()
        {
            lock(_buffer)
            {
                if (Count <= 0)
                    throw new System.InvalidOperationException("Buffer already 0 count");

                First.Reset();                
                iFirst++;

                if (Count < 0 || Count > cap)
                    throw new InvalidOperationException("Invalid buffer index detected");
            }
        }

        public void Clear()
        {
            lock(_buffer)
            {
                iTail = 0;
                iFirst = 0;

                for (int f = 0; f < _buffer.Count; f++)
                {
                    _buffer[f].Reset();
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < cap; i++)
            {
                _buffer[i].Dispose();
            }

            _buffer.Clear();

            _buffer = null;
        }

        ~FrameBuffer()
        {
            try
            {
                Dispose();
            }
            catch
            { }
        }

        public void Enqueue(Frame rawFrame)
        {
            var Last = MakeLastAvailable();

            Last.DrawFrame(rawFrame);
        }
    }
}
 