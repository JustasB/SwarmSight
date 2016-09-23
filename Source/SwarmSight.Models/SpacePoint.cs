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
    public class SpacePoint
    {
        public SpaceManager Space;

        public DPoint FramePoint;
        public DPoint SubclippedPoint;
        public DPoint StandardPoint;
        public WPoint PriorPoint;

        public SpacePoint(SpaceManager spaceManager)
        {
            Space = spaceManager;
        }

        public SpacePoint FromStandardSpace(DPoint source)
        {
            StandardPoint = source;
            SubclippedPoint = Space.ToHeadSpaceFromStandardSpace(StandardPoint);
            PriorPoint = Space.ToPriorSpaceFromHeadSpace(SubclippedPoint);
            FramePoint = Space.ToFrameSpaceFromHeadSpace(SubclippedPoint);

            return this;
        }

        public SpacePoint FromPriorSpace(WPoint source)
        {
            PriorPoint = source;
            SubclippedPoint = Space.ToHeadSpaceFromPriorSpace(source);
            StandardPoint = Space.ToStandardSpaceFromHeadSpace(SubclippedPoint);
            FramePoint = Space.ToFrameSpaceFromHeadSpace(SubclippedPoint);

            return this;
        }

        public SpacePoint FromFrameSpace(DPoint source)
        {
            FramePoint = source;
            SubclippedPoint = Space.ToHeadSpaceFromFrameSpace(source);
            StandardPoint = Space.ToStandardSpaceFromHeadSpace(SubclippedPoint);
            PriorPoint = Space.ToPriorSpaceFromHeadSpace(SubclippedPoint);

            return this;
        }
    }
}
