using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using SwarmVision.Models;

namespace SwarmVision.VideoPlayer
{
    public class HeadSearchAlgorithm : GeneticAlgoBase<HeadModel>
    {

        public HeadSearchAlgorithm()
        {
            GenerationSize = 30;
            NumberOfGenerations = 10;
        }

        protected override HeadModel CreateChild(HeadModel parent1, HeadModel parent2)
        {
            //Create a new location 0-200% of distance from midpoint between the parents
            var midpoint = new Point
            (
                (int)Midpoint(parent1.Origin.X, parent2.Origin.X),
                (int)Midpoint(parent1.Origin.Y, parent2.Origin.Y)
            );

            var midAngle = Math.Atan2
            (
                (Math.Sin(parent1.AngleRad) + Math.Sin(parent2.AngleRad)) / 2.0,
                (Math.Cos(parent1.AngleRad) + Math.Cos(parent2.AngleRad)) / 2.0
            ) 
            * 180.0 / Math.PI;

            var distanceToMidpoint = Distance(midpoint.X, midpoint.Y, parent1.Origin.X, parent1.Origin.Y);
            var distanceToMidAngle = Math.Abs(AngleDistance(midAngle, parent1.Angle));

            var childPoint = new Point
            (
                (int)NextGaussian(midpoint.X, distanceToMidpoint),
                (int)NextGaussian(midpoint.Y, distanceToMidpoint)
            );

            var childAngle = (int) (NextGaussian(midAngle, distanceToMidAngle)%360.0);

            return new HeadModel { Angle = childAngle, Origin = childPoint };
        }

        private double AngleDistance(double firstAngle, double secondAngle)
        {
            double difference = secondAngle - firstAngle;
            while (difference < -180) difference += 360;
            while (difference > 180) difference -= 360;
            return difference;
        }

        protected override bool ValidChild(HeadModel child)
        {
            return Target.ValidConvoltionLocation(HeadView.Width, HeadView.Height, (int)child.Origin.X, (int)child.Origin.Y, child.Angle);
        }

        protected override HeadModel SelectLocation()
        {
            var aveX = (int)Generation.Average(i => i.Key.Origin.X);
            var aveY = (int)Generation.Average(i => i.Key.Origin.Y);
            var aveWidth = (int)Generation.Average(i => ((Frame)(i.Key.View)).Width);
            var aveHeight = (int)Generation.Average(i => ((Frame)(i.Key.View)).Height);
            var aveAngle = (int) (Generation.Average(i => i.Key.Angle + 100*360.0)%360);

            Target.DrawRectangle(aveX, aveY, aveWidth, aveHeight);

            return new HeadModel { Origin = new Point(aveX, aveY), Angle = aveAngle };
        }


        protected override double ComputeFitness(HeadModel individual)
        {
            if (individual.View == null)
            {
                using (var raw = Frame.FromBitmap(new HeadView(individual).Draw()))
                using (var edged = raw.EdgeFilter())
                using (var rotated = edged.Rotate(individual.Angle))
                {
                    individual.View = rotated.Trim();
                }
            }

            return Target.Compare((Frame)individual.View, (int)individual.Origin.X, (int)individual.Origin.Y);
        }

        protected override HeadModel CreateNewRandomMember()
        {
            return new HeadModel()
            {
                Origin = new Point
                (
                    Random.Next(0, Target.Width - HeadView.Width),
                    Random.Next(0, Target.Height - HeadView.Height)
                ),
                Angle = Random.Next(-15, 15+1)
            };

        }
    }
}