using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Point = System.Windows.Point;

namespace SwarmSight.Filters
{
    public class Regression
    {
        public class RegressionResult
        {
            public double Intercept;
            public double Slope;
            public double RSquared;

            public RegressionResult Invert()
            {
                var oldSlope = Slope;
                var oldIntercept = Intercept;

                Slope = 1/oldSlope;
                Intercept = -oldIntercept/oldSlope;

                return this;
            }
        }

        // Find the points of intersection.
        public static int FindLineCircleIntersections(
            float cx, float cy, float radius,
            PointF point1, PointF point2,
            out PointF intersection1, out PointF intersection2)
        {
            float dx, dy, A, B, C, det, t;

            dx = point2.X - point1.X;
            dy = point2.Y - point1.Y;

            A = dx * dx + dy * dy;
            B = 2 * (dx * (point1.X - cx) + dy * (point1.Y - cy));
            C = (point1.X - cx) * (point1.X - cx) +
                (point1.Y - cy) * (point1.Y - cy) -
                radius * radius;

            det = B * B - 4 * A * C;
            if ((A <= 0.0000001) || (det < 0))
            {
                // No real solutions.
                intersection1 = new PointF(float.NaN, float.NaN);
                intersection2 = new PointF(float.NaN, float.NaN);
                return 0;
            }
            else if (det == 0)
            {
                // One solution.
                t = -B / (2 * A);
                intersection1 =
                    new PointF(point1.X + t * dx, point1.Y + t * dy);
                intersection2 = new PointF(float.NaN, float.NaN);
                return 1;
            }
            else
            {
                // Two solutions.
                t = (float)((-B + Math.Sqrt(det)) / (2 * A));
                intersection1 =
                    new PointF(point1.X + t * dx, point1.Y + t * dy);
                t = (float)((-B - Math.Sqrt(det)) / (2 * A));
                intersection2 =
                    new PointF(point1.X + t * dx, point1.Y + t * dy);
                return 2;
            }
        }

        public RegressionResult RegressLine(List<Point> points)
        {
            if(points.Count == 0)
                return new RegressionResult();

            var xtyAve = points.Select(p => p.X*p.Y).Average();
            var xAve = points.Average(p => p.X);
            var yAve = points.Average(p => p.Y);
            var xsqAve = points.Select(p => p.X*p.X).Average();
            var ysqAve = points.Select(p => p.Y * p.Y).Average();
            var xAveSq = xAve*xAve;
            var yAveSq = yAve * yAve;

            var result = new RegressionResult();

            result.Slope = (xtyAve - xAve*yAve) / (xsqAve - xAveSq);

            if (double.IsNaN(result.Slope))
                result.Slope = 100000;

            result.Intercept = yAve - result.Slope*xAve;
            result.RSquared = (xtyAve - xAve*yAve)/Math.Sqrt((xsqAve - xAveSq)*(ysqAve - yAveSq));

            return result;
        }
        
        public bool PointIsWithinDistanceFrom(Point p, Point @from, double distance, double minDist = 0)
        {
            var ptDist = Distance(p, @from);
                   
            return ptDist >= minDist && ptDist <= distance;
        }

        public Point PointAlongLine(RegressionResult line, Point point, double distance)
        {
            //d2 = dx2 + dy2
            //m = dy / dx => m2 = dy2 / dx2
            //m2* dx2 = dy2
            //d2 = dx2 + m2 * dx2 => (1 + m2) * dx2

            //sqrt(d2 / (1 + m2)) = dx
            //sqrt(d2 - dx2) = dy

            var dx = Math.Sqrt(distance*distance/(1 + (line.Slope*line.Slope)));
            var dy = Math.Sqrt(distance*distance - dx*dx);

            return new Point
            (
                point.X + (int)dx * Math.Sign(distance), 
                point.Y + (int)dy * Math.Sign(distance)
            );
        }

        public double Distance(Point p, Point origin)
        {
            var distX = p.X - origin.X;
            var distY = p.Y - origin.Y;

            return Math.Sqrt(distX*distX + distY*distY);
        }

        public Point Intersects(RegressionResult line1, RegressionResult line2)
        {
            //x=(a2-a1)/(m1-m2)
            var x = (line2.Intercept - line1.Intercept)/(line1.Slope - line2.Slope);
            var y = line1.Slope*x + line1.Intercept;

            return new Point(x,y);
        }

        public List<Point> PointsOnLineDistanceAway(Point p, RegressionResult line, double dist)
        {
            //{{x1->(-a m+xo-Sqrt[d^2 (1+m^2)-(a+m xo-yo)^2]+m yo)/(1+m^2)}
            //{ x1->(-a m+xo+Sqrt[d^2 (1+m^2)-(a+m xo-yo)^2]+m yo)/(1+m^2)}}

            //{{x1->(-a*m+xo-Math.Sqrt[dsq*(1+msq)-(a+m*xo-yo)^2]+m*yo)/(1+msq)}
            //{ x1->(-a*m+xo+Math.Sqrt[dsq*(1+msq)-(a+m*xo-yo)^2]+m*yo)/(1+msq)}}

            var a = line.Intercept;
            var m = line.Slope;
            var d = dist;
            var dsq = d*d;
            var msq = m*m;
            var xo = p.X;
            var yo = p.Y;

            var sqrtPart = Math.Sqrt(dsq * (1 + msq) - (a + m * xo - yo) * (a + m * xo - yo));
            var xplus =  (int)Math.Round((-a * m + xo - sqrtPart + m * yo) / (1 + msq));
            var xminus = (int)Math.Round((-a * m + xo + sqrtPart + m * yo) / (1 + msq));

            if(xminus == int.MinValue)
                return new List<Point>();

            return new List<Point>
            {
                new Point(xplus,  (int)Math.Round(xplus*m+a)),
                new Point(xminus, (int)Math.Round(xminus*m+a))
            };
        }

        public List<Point> PointsOnVerticalLineDistanceAway(Point p, int lineX, double dist)
        {
            var d = dist;
            var dsq = d*d;
            var xo = p.X;
            var yo = p.Y;
            
            //dy=d2-(xav-xo)2=dy
            //y1 = yo +/ -dy

            var dy = dsq - (lineX - xo)*(lineX - xo);

            return new List<Point>
            {
                new Point(lineX, (int)Math.Round(yo-dy)),
                new Point(lineX, (int)Math.Round(yo+dy))
            };
        }
    }
}