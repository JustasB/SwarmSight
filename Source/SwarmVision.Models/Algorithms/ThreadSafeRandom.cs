using System;
using System.Collections.Generic;
using System.Threading;

namespace SwarmVision.HeadPartsTracking.Algorithms
{
    public class ThreadSafeRandom
    {
        private Dictionary<int, Random> _randoms = new Dictionary<int, Random>();
        private int nextSeed;
        public ThreadSafeRandom(int seed)
        {
            nextSeed = seed + 1;
        }

        public int Next(int max)
        {
            return CurrentRandom().Next(max);
        }

        public int Next(int min, int max)
        {
            return CurrentRandom().Next(min, max);
        }

        public double NextDouble()
        {
            return CurrentRandom().NextDouble();
        }

        private Random CurrentRandom()
        {
            var currThread = Thread.CurrentThread.ManagedThreadId;

            if (!_randoms.ContainsKey(currThread))
                lock (_randoms)
                {
                    _randoms.Add(currThread, new Random(nextSeed));
                    nextSeed++;
                }

            return _randoms[currThread];
        }
    }
}