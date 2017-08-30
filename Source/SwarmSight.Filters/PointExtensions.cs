using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using PointF = System.Drawing.PointF;
using Rectangle = System.Drawing.Rectangle;
using DPoint = System.Drawing.Point;
using Color = System.Drawing.Color;
using System.Diagnostics;
using static System.Math;

namespace SwarmSight.Filters
{
    public static class PointExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToPriorAngle(this double tanhAngle)
        {
            var zeroTothree60 = (tanhAngle - 90 + 360) % 360;

            if (zeroTothree60 > 180)
                return zeroTothree60 - 360;
            else
                return zeroTothree60;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(this Point a, Point b)
        {
            var x = b.X - a.X;
            var y = b.Y - a.Y;

            return Math.Sqrt(x * x + y * y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(this System.Drawing.Point a, System.Drawing.Point b)
        {
            var x = b.X - a.X;
            var y = b.Y - a.Y;

            return Math.Sqrt(x * x + y * y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point ToWindowsPoint(this System.Drawing.Point target)
        {
            return new Point(target.X, target.Y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointF ToPointF(this Point target)
        {
            return new PointF((float)target.X, (float)target.Y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointF ToPointF(this System.Drawing.Point target)
        {
            return new PointF(target.X, target.Y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point Moved(this Point target, double byX, double byY)
        {
            return new Point(target.X + byX, target.Y + byY);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Drawing.Point Moved(this System.Drawing.Point target, int byX, int byY)
        {
            return new System.Drawing.Point(target.X + byX, target.Y + byY);
        }
        public static Rectangle EnclosingRectangle(this Point target, int radius)
        {
            return new Rectangle(
                (target.X - radius).Rounded(),
                (target.Y - radius).Rounded(),
                radius * 2 + 1,
                radius * 2 + 1
            );
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point ToPriorSpace(this Point target, Point windowDims, double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double scapeDistance)
        {
            target.Offset(-windowDims.X / 2.0 + offsetX, -windowDims.Y / 2.0 + offsetY);
            target = target.Multiply(1.0 / (scaleX * scapeDistance), 1.0 / (scaleY * scapeDistance));
            target = target.Rotate(-(headAngle - priorAngle));

            return target;

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Drawing.Point ToSubclippedSpace(this Point target, Point windowDims, double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double scapeDistance)
        {
            target = target.Multiply(scaleX * scapeDistance, scaleY * scapeDistance);
            target = target.Rotate(headAngle - priorAngle);
            target.Offset(windowDims.X / 2.0 - offsetX, windowDims.Y / 2.0 - offsetY);

            return target.ToDrawingPoint();
        }

        public static Point Multiply(this Point target, double scalar, double? scalarY = null)
        {
            target.X *= scalar;
            target.Y *= scalarY ?? scalar;

            return target;
        }
        public static List<Point> AndNeighbors(this Point p)
        {
            var result = new List<Point>(8);

            for (var x = -1; x <= 1; x++)
                for (var y = -1; y <= 1; y++)
                {
                    result.Add(new Point(p.X + x, p.Y + y));
                }

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Drawing.Point ToDrawingPoint(this Point target)
        {
            return new System.Drawing.Point(target.X.Rounded(), target.Y.Rounded());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point InvertY(this Point target)
        {
            target.Y *= -1;

            return target;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point Clone(this Point target)
        {
            return new Point(target.X, target.Y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point Rotate(this Point target, double angle)
        {
            var result = target;
            var degRadian = angle * Math.PI / 180.0;

            var cos = Math.Cos(degRadian);
            var sin = Math.Sin(degRadian);

            result.X = target.X * cos - target.Y * sin;
            result.Y = target.X * sin + target.Y * cos;

            target = result;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInConvexPolygon(int testX, int testY, List<DPoint> polygon)
        {
            //Cannot be part of empty polygon
            if (polygon.Count == 0)
            {
                return false;
            }

            var x = testX;
            var y = testY;

            //With 1-pt polygon, only if it's the point
            if (polygon.Count == 1)
            {
                return (polygon[0].X == x && polygon[0].Y == y);
            }

            //n>2 Keep track of cross product sign changes
            var pos = 0;
            var neg = 0;

            for (var i = 0; i < polygon.Count; i++)
            {
                //Form a segment between the i'th point
                var x1 = polygon[i].X;
                var y1 = polygon[i].Y;

                //If point is in the polygon
                if (x1 == x && y1 == y)
                    return true;

                //And the i+1'th, or if i is the last, with the first point
                var i2 = i < polygon.Count - 1 ? i + 1 : 0;

                var x2 = polygon[i2].X;
                var y2 = polygon[i2].Y;

                //Compute the cross product
                var d = (x - x1) * (y2 - y1) - (y - y1) * (x2 - x1);

                if (d > 0) pos++;
                if (d < 0) neg++;

                //If the sign changes, then point is outside
                if (pos > 0 && neg > 0)
                    return false;
            }

            //If no change in direction, then on same side of all segments, and thus inside
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPointInPolygon(int x, int y, Point[] polygon)
        {
            var p = new Point(x, y);

            double minX = polygon[0].X;
            double maxX = polygon[0].X;
            double minY = polygon[0].Y;
            double maxY = polygon[0].Y;
            for (int i = 1; i < polygon.Length; i++)
            {
                Point q = polygon[i];
                minX = Math.Min(q.X, minX);
                maxX = Math.Max(q.X, maxX);
                minY = Math.Min(q.Y, minY);
                maxY = Math.Max(q.Y, maxY);
            }

            if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
            {
                return false;
            }

            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y) &&
                     p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInConvexPolygon(DPoint testPoint, List<DPoint> polygon)
        {
            //Cannot be part of empty polygon
            if (polygon.Count == 0)
            {
                return false;
            }

            //With 1-pt polygon, only if it's the point
            if (polygon.Count == 1)
            {
                return polygon[0] == testPoint;
            }

            //n>2 Keep track of cross product sign changes
            var pos = 0;
            var neg = 0;

            for (var i = 0; i < polygon.Count; i++)
            {
                //If point is in the polygon
                if (polygon[i] == testPoint)
                    return true;

                //Form a segment between the i'th point
                var x1 = polygon[i].X;
                var y1 = polygon[i].Y;

                //And the i+1'th, or if i is the last, with the first point
                var i2 = i < polygon.Count - 1 ? i + 1 : 0;

                var x2 = polygon[i2].X;
                var y2 = polygon[i2].Y;

                var x = testPoint.X;
                var y = testPoint.Y;

                //Compute the cross product
                var d = (x - x1) * (y2 - y1) - (y - y1) * (x2 - x1);

                if (d > 0) pos++;
                if (d < 0) neg++;

                //If the sign changes, then point is outside
                if (pos > 0 && neg > 0)
                    return false;
            }

            //If no change in direction, then on same side of all segments, and thus inside
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInPolygon(this Point testPoint, List<Point> polygon)
        {
            //Cannot be part of empty polygon
            if (polygon.Count == 0)
            {
                return false;
            }

            //With 1-pt polygon, only if it's the point
            if (polygon.Count == 1)
            {
                return polygon[0] == testPoint;
            }

            //n>2 Keep track of cross product sign changes
            var pos = 0;
            var neg = 0;

            for (var i = 0; i < polygon.Count; i++)
            {
                //If point is in the polygon
                if (polygon[i] == testPoint)
                    return true;

                //Form a segment between the i'th point
                var x1 = polygon[i].X;
                var y1 = polygon[i].Y;

                //And the i+1'th, or if i is the last, with the first point
                var i2 = i < polygon.Count - 1 ? i + 1 : 0;

                var x2 = polygon[i2].X;
                var y2 = polygon[i2].Y;

                var x = testPoint.X;
                var y = testPoint.Y;

                //Compute the cross product
                var d = (x - x1) * (y2 - y1) - (y - y1) * (x2 - x1);

                if (d > 0) pos++;
                if (d < 0) neg++;

                //If the sign changes, then point is outside
                if (pos > 0 && neg > 0)
                    return false;
            }

            //If no change in direction, then on same side of all segments, and thus inside
            return true;
        }

        public static Stopwatch DistanceTime = new Stopwatch();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double[] DistToSegmentSquared(this List<Point> points, Point x1y1, Point x2y2)
        {
            return DistToSegmentSquared(points, x1y1.X, x1y1.Y, x2y2.X, x2y2.Y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double[] DistToSegmentSquared(this List<Point> points, Double x1, Double y1, Double x2, Double y2)
        {
            var C = x2 - x1;
            var D = y2 - y1;
            var lenSq = C * C + D * D;
            var result = new double[points.Count];
            var _x1 = x1;
            var _y1 = y1;
            var _x2 = x2;
            var _y2 = y2;

            //Parallel.For(0, points.Count, p =>
            for (int p = 0; p < points.Count; p++)
            {
                var pt = points[p];

                var x = pt.X;
                var y = pt.Y;

                var A = x - _x1;
                var B = y - _y1;

                var param = lenSq > 0 ? (A * C + B * D) / lenSq : -1;

                var xx = param >= 0 && param <= 1 ? _x1 + param * C : (param < 0 ? _x1 : _x2);
                var yy = param >= 0 && param <= 1 ? _y1 + param * D : (param < 0 ? _y1 : _y2);

                var dx = x - xx;
                var dy = y - yy;

                result[p] = (dx * dx + dy * dy); //Math.Sqrt
            }
            //);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Positive(this double target)
        {
            return Abs(target);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Positive(this int target)
        {
            return Abs(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Rounded(this double target)
        {
            return (int)Math.Round(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color ToColor(this int bwValue)
        {
            return Color.FromArgb(bwValue, bwValue, bwValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color ToColor(this double index)
        {
            var d = (255 * index).Rounded();

            return Color.FromArgb(d, d, d);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Scale(this double target, double targetRangeL, double targetRangeH, double resultRangeL, double resultRangeH)
        {
            return (target - targetRangeL) / (targetRangeH - targetRangeL) * (resultRangeH - resultRangeL) + resultRangeL;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double? Median<TColl, TValue>(
            this IEnumerable<TColl> source,
            Func<TColl, TValue> selector)
        {
            return source.Select<TColl, TValue>(selector).Median();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double? Median<T>(
            this IEnumerable<T> source)
        {
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
                source = source.Where(x => x != null);

            int count = source.Count();
            if (count == 0)
                return null;

            source = source.OrderBy(n => n);

            int midpoint = count / 2;
            if (count % 2 == 0)
                return (Convert.ToDouble(source.ElementAt(midpoint - 1)) + Convert.ToDouble(source.ElementAt(midpoint))) / 2.0;
            else
                return Convert.ToDouble(source.ElementAt(midpoint));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point ToPoint(this PointF pt)
        {
            return new Point(pt.X, pt.Y);
        }


        public static Point GetPointDistanceAwayOnLine(this Point startPt, double length, Point linePoint, Point closerToPoint)
        {
            PointF int1, int2;

            Regression.FindLineCircleIntersections((float)startPt.X, (float)startPt.Y, (float)length,
                                startPt.ToPointF(), linePoint.ToPointF(), out int1, out int2);

            var result =
                int1.ToPoint().Distance(closerToPoint) < int2.ToPoint().Distance(closerToPoint) ?
                int1 : int2;

            return result.ToPoint();
        }

        public static Point3D CombineMirrorPoints(Point top, Point side, double d, double angle)
        {
            var b = d + top.X;
            var alpha = 90 - angle;
            var zprime = -side.X;
            var c = Math.Cos(alpha * Math.PI / 180.0) * zprime;
            var e = b + c;
            var g = e / Math.Cos(angle * Math.PI / 180.0);
            var f = Math.Sqrt(g * g + zprime * zprime);
            var h = Math.Sqrt(f * f - b * b);

            return new Point3D(top.X, top.Y, h);
        }

    }
}
