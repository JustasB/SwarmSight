using SwarmSight.Filters;

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

        public DPoint AntennaSensorOffset;
        public DPoint AntennaSensorDims
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

        public WPoint ToPriorSpaceFromSubclippedSpace(WPoint p)
        {
            return p.ToPriorSpace(new WPoint(AntennaSensorDims.X, AntennaSensorDims.Y), HeadAngle, ScaleX, ScaleX, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }

        public System.Drawing.Point ToSubclippedSpaceFromPriorSpace(WPoint p)
        {
            return p.ToSubclippedSpace(new WPoint(AntennaSensorDims.X, AntennaSensorDims.Y), HeadAngle, ScaleX, ScaleX, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }
        public WPoint ToPriorSpaceFromSubclippedSpace(System.Drawing.Point p)
        {
            return p.ToWindowsPoint().ToPriorSpace(new WPoint(AntennaSensorDims.X, AntennaSensorDims.Y), HeadAngle, ScaleX, ScaleX, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }

        public System.Drawing.Point ToSubclippedSpaceFromPriorSpace(System.Drawing.Point p)
        {
            return p.ToWindowsPoint().ToSubclippedSpace(new WPoint(AntennaSensorDims.X, AntennaSensorDims.Y), HeadAngle, ScaleX, ScaleX, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }

        public DPoint ToStandardSpaceFromSubclippedSpace(DPoint p)
        {
            var x = (int)Round(1.0 * p.X / AntennaSensorDims.X * StandardWidth, 0);
            var y = (int)Round(1.0 * p.Y / AntennaSensorDims.Y * StandardHeight, 0);

            return new DPoint(x, y);
        }

        public DPoint ToSubclippedSpaceFromStandardSpace(DPoint p)
        {
            var x = (int)Round(1.0 * p.X / StandardWidth * AntennaSensorDims.X, 0);
            var y = (int)Round(1.0 * p.Y / StandardHeight * AntennaSensorDims.Y, 0);

            return new DPoint(x, y);
        }

        public DPoint ToFrameSpaceFromSubclippedSpace(DPoint p)
        {
            return new DPoint(p.X + AntennaSensorOffset.X, p.Y + AntennaSensorOffset.Y);
        }

        public DPoint ToSubclippedSpaceFromFrameSpace(DPoint p)
        {
            return new DPoint(p.X - AntennaSensorOffset.X, p.Y - AntennaSensorOffset.Y);
        }
    }

}
