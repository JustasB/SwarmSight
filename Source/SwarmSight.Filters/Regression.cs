using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

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
            result.Intercept = yAve - result.Slope*xAve;
            result.RSquared = (xtyAve - xAve*yAve)/Math.Sqrt((xsqAve - xAveSq)*(ysqAve - yAveSq));

            return result;
        }

        public double FindDistanceToBendPoint(List<Point> points, Point start, double minDistance, double maxDistance)
        {
            var divisions = 10;
            var increment = (maxDistance - minDistance) / divisions;
            var prevSlope = (double?)null;

            var slopeTracker = (new double[] { }).Select(r => new
            {
                RangeDistance = (double)0,
                SumRSquared = 0.0
            })
            .ToList();

            //Progressivelly include points to regress
            for (var d = 0; d < divisions; d++)
            {
                var rangeDistance = (d + 1) * increment + minDistance;

                var seg1pts = points.Where(p => PointIsWithinDistanceFrom(p, start, rangeDistance)).ToList();
                var seg2pts = points.Where(p => PointIsWithinDistanceFrom(p, start, maxDistance, rangeDistance)).ToList();

                slopeTracker.Add(new
                {
                    RangeDistance = rangeDistance,
                    SumRSquared = RegressLine(seg1pts).RSquared + 2*RegressLine(seg2pts).RSquared
                });
            }

            //Assume the bend occurs whre both segments are most line-like
            var maxRsquared = slopeTracker.Max(r => r.SumRSquared);

            return slopeTracker.First(r => r.SumRSquared == maxRsquared).RangeDistance;
        }

        public double FindDistanceToBendPointOld(List<Point> points, Point start, double minDistance, double maxDistance)
        {
            var divisions = 10;
            var slopeDeltaThreshold = 0.2;

            var increment = (maxDistance-minDistance)/divisions;
            var prevSlope = (double?) null;

            var slopeTracker = (new double[] { }).Select(r => new
            {
                RangeDistance = (double)0,
                LineParams = new RegressionResult(),
                SlopeAbsDelta = 0.0
            })
            .ToList();

            //Progressivelly include points to regress
            for (var d = 0; d < divisions; d++)
            {
                var rangeDistance = (d)*increment+minDistance;
                var rangePoints = points.Where(p => PointIsWithinDistanceFrom(p,start, rangeDistance)).ToList();
                var lineParams = RegressLine(rangePoints);
                var slopeAbsDelta = prevSlope == null ? 0 : Math.Abs(prevSlope.Value - lineParams.Slope);

                slopeTracker.Add(new
                {
                    RangeDistance = rangeDistance, LineParams = lineParams, SlopeAbsDelta = slopeAbsDelta
                });

                prevSlope = lineParams.Slope;
            }

            //Assume the bend occurs when slope starts to change
            var threshold = slopeTracker.Max(r => r.SlopeAbsDelta)*slopeDeltaThreshold;

            for (var r = 1; r < slopeTracker.Count; r++)
            {
                if (slopeTracker[r].SlopeAbsDelta > threshold)
                {
                    return slopeTracker[r].RangeDistance;
                }
            }

            //If there is no bend (this will never happen)
            return maxDistance;
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
                point.X + (int)dx, 
                point.Y + (int)dy
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

            return new Point((int)x,(int)y);
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