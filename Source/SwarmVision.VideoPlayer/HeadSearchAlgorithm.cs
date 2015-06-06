using System;
using System.Drawing;
using System.Linq;

namespace SwarmVision.VideoPlayer
{
    public class HeadSearchAlgorithm : GeneticAlgoBase<Point>
    {

        protected override Point CreateChild(Point parent1, Point parent2)
        {
            //Create a new location 0-200% of distance from midpoint between the parents
            var midpoint = new Point
            (
                (int)Midpoint(parent1.X, parent2.X),
                (int)Midpoint(parent1.Y, parent2.Y)
            );

            var distanceToMidpoint = Distance(midpoint.X, midpoint.Y, parent1.X, parent1.Y);

            var child = new Point
            (
                (int)NextGaussian(midpoint.X, distanceToMidpoint),
                (int)NextGaussian(midpoint.Y, distanceToMidpoint)
            );

            return child;
        }

        protected override bool ValidChild(Point child)
        {
            return Target.ValidConvoltionLocation(Pattern, child.X, child.Y);
        }

        protected override Point SelectLocation()
        {
            var aveX = (int)Generation.Average(i => i.Key.X);
            var aveY = (int)Generation.Average(i => i.Key.Y);

            return new Point(aveX, aveY);
        }

        protected override double ComputeFitness(Point individual)
        {
            return Target.Compare(Pattern, individual.X, individual.Y);
        }

        protected override Point CreateNewRandomMember()
        {
            return new Point
                (
                Random.Next(0, Target.Width - Pattern.Width),
                Random.Next(0, Target.Height - Pattern.Height)
                );
        }
    }
}