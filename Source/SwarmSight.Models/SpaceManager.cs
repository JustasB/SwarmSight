using SwarmSight.Filters;
using SwarmSight.HeadPartsTracking.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;
using DPoint = System.Drawing.Point;
using WPoint = System.Windows.Point;

namespace SwarmSight.HeadPartsTracking
{
    public class SpaceManager
    {
        public int StandardWidth = 100;
        public int StandardHeight = 100;

        public DPoint HeadOffset;
        public DPoint HeadDims
        {
            get
            {
                return new DPoint(
                    (StandardWidth * ScaleX).Rounded(),
                    (StandardHeight * ScaleX).Rounded()
                );
            }
        }

        public double HeadAngle;
        public double OffsetY = 0;
        public double OffsetX = 0;
        public double PriorAngle = 0;
        public double ScaleX;
        public double ScapeDistanceAtScale1;

        public WPoint ToPriorSpaceFromHeadSpace(WPoint p)
        {
            return p.ToPriorSpace(new WPoint(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleX, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }

        public System.Drawing.Point ToHeadSpaceFromPriorSpace(WPoint p)
        {
            return p.ToHeadSpace(new WPoint(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleX, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }
        public WPoint ToPriorSpaceFromHeadSpace(System.Drawing.Point p)
        {
            return p.ToWindowsPoint().ToPriorSpace(new WPoint(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleX, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }

        public System.Drawing.Point ToHeadSpaceFromPriorSpace(System.Drawing.Point p)
        {
            return p.ToWindowsPoint().ToHeadSpace(new WPoint(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleX, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }

        public DPoint ToStandardSpaceFromHeadSpace(DPoint p)
        {
            var x = (int)Round(1.0 * p.X / HeadDims.X * StandardWidth, 0);
            var y = (int)Round(1.0 * p.Y / HeadDims.Y * StandardHeight, 0);

            return new DPoint(x, y);
        }

        public DPoint ToHeadSpaceFromStandardSpace(DPoint p)
        {
            var x = (int)Round(1.0 * p.X / StandardWidth * HeadDims.X, 0);
            var y = (int)Round(1.0 * p.Y / StandardHeight * HeadDims.Y, 0);

            return new DPoint(x, y);
        }

        public DPoint ToFrameSpaceFromHeadSpace(DPoint p)
        {
            return new DPoint(p.X + HeadOffset.X, p.Y + HeadOffset.Y);
        }

        public DPoint ToHeadSpaceFromFrameSpace(DPoint p)
        {
            return new DPoint(p.X - HeadOffset.X, p.Y - HeadOffset.Y);
        }
    }

}
