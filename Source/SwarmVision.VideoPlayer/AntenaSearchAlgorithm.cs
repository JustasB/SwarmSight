using System.Drawing;

namespace SwarmVision.VideoPlayer
{

    public class Line
    {
        public Point Start = new Point();
        public Point End = new Point();
        public int Thickness;
    }

    public class AntenaSearchAlgorithm : GeneticAlgoBase<Line>
    {
        protected override Line CreateChild(Line parent1, Line parent2)
        {
            //Child starts 0-200% from midpoint of parent's ends
            var childStart = new Point
            (
                (int)NextGaussian((int)Midpoint(parent1.Start.X, parent2.Start.X), Distance(parent1.Start.X, parent2.Start.X) / 2),
                (int)NextGaussian((int)Midpoint(parent1.Start.Y, parent2.Start.Y), Distance(parent1.Start.Y, parent2.Start.Y) / 2)
            );

            var childEnd = new Point
            (
                (int)NextGaussian((int)Midpoint(parent1.End.X, parent2.End.X), Distance(parent1.End.X, parent2.End.X) / 2),
                (int)NextGaussian((int)Midpoint(parent1.End.Y, parent2.End.Y), Distance(parent1.End.Y, parent2.End.Y) / 2)
            );

            var childThickness =
                (int)NextGaussian((int)Midpoint(parent1.Thickness, parent2.Thickness), Distance(parent1.Thickness, parent2.Thickness) / 2);

            var child = new Line()
            {
                Start = childStart,
                End = childEnd,
                Thickness = childThickness
            };

            return child;
        }

        protected override bool ValidChild(Line child)
        {
            throw new System.NotImplementedException();
        }

        protected override Line SelectLocation()
        {
            throw new System.NotImplementedException();
        }

        protected override double ComputeFitness(Line individual)
        {
            throw new System.NotImplementedException();
        }

        protected override Line CreateNewRandomMember()
        {
            throw new System.NotImplementedException();
        }
    }
}