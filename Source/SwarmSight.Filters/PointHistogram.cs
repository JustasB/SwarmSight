using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Media3D;

namespace SwarmSight.Filters
{
    /// <summary>
    /// A 256-bin color value histogram that keeps track of points that belong to
    /// each bin
    /// </summary>
    public class PointHistogram
    {
        public int Count;
        public List<Point>[] BinPoints;
        public int StartingCap = 100;

        public PointHistogram()
        {
            BinPoints = new List<Point>[256];

            for (int i = 0; i < 256; i++)
            {
                //Pre-allocate assuming that lowest values will have the most points
                var cap = StartingCap / (i + 1);

                //Emplies are not tracked by default
                if (i == 0)
                    cap = 0;

                BinPoints[i] = new List<Point>(cap);
            }
        }

        /// <summary>
        /// Build a histogram of color values from the source frame by placing each
        /// point from the specified list of points into that point's color bin
        /// </summary>
        /// <param name="source"></param>
        /// <param name="points"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public unsafe PointHistogram FromPointList(Frame source, List<Point> points, ColorChannel channel = ColorChannel.G)
        {
            var firstPx = source.FirstPixelPointer;
            var stride = source.Stride;
            var chan = (int)channel;

            Count = 0;
            BinPoints.AsParallel().ForAll(bin => bin.Clear());

            points
                .AsParallel()
                .ForAll(p =>
                {
                    var x = p.X;
                    var y = p.Y;

                    var offset = y * stride + 3 * x + chan;
                    var bin = firstPx[offset];

                    //Don't track empties
                    if (bin != 0)
                    {
                        var binList = BinPoints[bin];

                        lock (binList)
                        {
                            binList.Add(p);
                        }
                    }

                    Count++;
                });

            return this;
        }

        /// <summary>
        /// Gets the points and their bin value that have the highest bin values
        /// (i.e. the top n% active points)
        /// </summary>
        /// <param name="topPercent"></param>
        /// <returns></returns>
        public List<Tuple<Point, double>> GetTail(double topPercent = 0.04, int lowLimit = 10)
        {
            var stopCount = (int)Math.Round(Count * topPercent, 0);
            var result = new List<Tuple<Point, double>>(stopCount);
            var limit = lowLimit;

            //Slow motion component
            if (Count > 0)
            {
                var tailSum = 0;
                var bin = 255;

                //Collect the points from the histogram tail until reached X% of possible hull points
                do
                {
                    var pointsInBin = BinPoints[bin];
                    var binCount = pointsInBin.Count;

                    if (binCount > 0)
                    {
                        tailSum += binCount;
                        result.AddRange(pointsInBin.Select(p => new Tuple<Point, double>(p, bin)));
                    }
                    
                    bin--;
                }
                while (bin >= limit && tailSum < stopCount);
            }

            return result;
        }

    }
}
