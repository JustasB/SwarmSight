using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Classes
{
    public class FrameComparerResults
    {
        public int Threshold { get; set; }
        public int ChangedPixelsCount { get; set; }
        public TimeSpan FrameTime { get; set; }
        public int FrameIndex { get; set; }
        public List<Point> ChangedPixels { get; set; }

        public FrameComparerResults()
        {
            ChangedPixels = new List<Point>();
        }
    }
}