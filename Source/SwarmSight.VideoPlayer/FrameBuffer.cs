using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SwarmSight.Filters;

namespace SwarmSight.VideoPlayer
{
    public class FrameBuffer<T> : IEnumerable<T>
    {
        private readonly LinkedList<T> _buffer;

        public FrameBuffer()
        {
            _buffer = new LinkedList<T>();
        }

        public LinkedListNode<T> First
        {
            get
            {
                lock (_buffer)
                {
                    return _buffer.First;
                }
            }
        }

        public void Remove(LinkedListNode<T> target)
        {
            lock (_buffer)
            {
                _buffer.Remove(target);
            }
        }

        public void Remove(T target)
        {
            lock (_buffer)
            {
                _buffer.Remove(target);
            }
        }

        public int Count
        {
            get
            {
                lock (_buffer)
                {
                    return _buffer.Count;
                }
            }
        }

        public void AddLast(T target)
        {
            lock (_buffer)
            {
                _buffer.AddLast(target);
            }
        }

        public void Clear()
        {
            lock (_buffer)
            {
                _buffer.Clear();
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            lock (_buffer)
            {
                return _buffer.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
 