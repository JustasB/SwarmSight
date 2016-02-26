using System;
using System.Collections.Generic;
using System.Threading;

namespace SwarmSight.HeadPartsTracking.Algorithms
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
            var rand = new Random((int)(DateTime.Now.Ticks % 10000) + nextSeed);
            nextSeed++;
            return rand;

            //var currThread = Thread.CurrentThread.ManagedThreadId;

            //while (!_randoms.ContainsKey(currThread))
            //{
            //    try
            //    {
            //        var rand = new Random((int)(DateTime.Now.Ticks % 10000) + nextSeed);
            //        _randoms[currThread] = rand;
            //        nextSeed++;
            //        return rand;
            //    }
            //    catch
            //    {
            //        Thread.Sleep(1);
            //    }
            //}

            //return _randoms[currThread];
        }
    }
}