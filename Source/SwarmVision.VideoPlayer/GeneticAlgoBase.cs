using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SwarmVision.VideoPlayer
{
    public abstract class GeneticAlgoBase<T> where T: IDisposable
    {
        protected Random Random = new Random(1);
        protected Dictionary<T, double> Generation = new Dictionary<T, double>();
        protected int ParentCount = 0;
        protected int GenerationSize = 10;
        protected int MinGenerationSize = 10;
        protected int NumberOfGenerations = 2;
        protected double PercentRandom = 50;
        protected Frame Target;
        protected int PercentPruneLow = 70;
        protected int PercentPruneHigh = 70;
        protected double InitialFitness = -1;

        protected abstract T CreateChild(T parent1, T parent2);
        protected abstract bool ValidChild(T child);
        protected abstract T SelectLocation();
        protected abstract double ComputeFitness(T individual);
        protected abstract T CreateNewRandomMember();

        private int frame = 0;

        public T Search(Frame target)
        {
            GenerationSize = MinGenerationSize + Math.Max(0, 5 - frame)*20;

            Target = target;

            //Reset the fitness of each member on new frame
            for (var i = 0; i < Generation.Count; i++)
                Generation[Generation.ElementAt(i).Key] = InitialFitness;

            //Create new generation
            for (var g = 0; g < (NumberOfGenerations + (Math.Max(5 - frame,0))); g++)
            {
                var timer = new Stopwatch(); timer.Start();

                //Add random members as potential parents or initially
                var randomCount = Generation.Count == 0 ? 
                    GenerationSize : 
                    (PercentRandom / 100.0) * GenerationSize;

                for (var i = 0; i < randomCount && Generation.Count < GenerationSize; i++)
                {
                    var newItem = CreateNewRandomMember();

                    if (ValidChild(newItem) && !Generation.ContainsKey(newItem))
                        Generation.Add(newItem, InitialFitness);
                    else
                        i--; //Try again
                }

                ParentCount = Generation.Count;

                //Fill the rest with children
                while (Generation.Count < GenerationSize)
                {
                    //Pick two random survivors (parents)
                    T randomSurvivor1;
                    T randomSurvivor2;

                    SelectParents(out randomSurvivor1, out randomSurvivor2);

                    //Don't cross with self
                    if (randomSurvivor1.Equals(randomSurvivor2))
                        continue;

                    var child = CreateChild(randomSurvivor1, randomSurvivor2);

                    //Don't add invalid locations or already exists
                    if (!Generation.ContainsKey(child) && 
                        ValidChild(child))
                        Generation.Add(child, InitialFitness);
                }

                var timer2 = new Stopwatch(); timer2.Start();
                //Evaluate fitness (starting at bottom of list, where unevaluated units are)
                for (var i = GenerationSize - 1; i >= 0; i--)
                {
                    var individual = Generation.ElementAt(i);

                    //If reached an evaluated unit, stop looping
                    if (individual.Value > InitialFitness)
                        break;

                    var fitness = ComputeFitness(individual.Key);

                    Generation[individual.Key] = fitness;
                }
                Debug.WriteLine("ComputeFitness (ms): " + timer2.ElapsedMilliseconds);

                //Prune least fit X-Y% of population
                var keep = (int) ((1-Random.Next(PercentPruneLow, PercentPruneHigh + 1)/100.0)*GenerationSize);

                //Sort by fitness
                Generation = Generation
                    .OrderBy(i => i.Value)
                    .ToDictionary(i => i.Key, i => i.Value);

                //Dispose about to be pruned members
                for (var i = keep; i < Generation.Count; i++)
                    Generation.ElementAt(i).Key.Dispose();

                Generation = Generation
                    .Take(keep)
                    .ToDictionary(i => i.Key, i => i.Value);

                Debug.WriteLine("Gen: " + g + " took (ms): " + timer.ElapsedMilliseconds + ", best fitness: " + Generation.First().Value);
            }

            frame++;

            return SelectLocation();
        }

        protected virtual void SelectParents(out T parent1, out T parent2)
        {
            parent1 = Generation.ElementAt(Random.Next(ParentCount)).Key;
            parent2 = Generation.ElementAt(Random.Next(ParentCount)).Key;
        }

        protected double Midpoint(double a, double b)
        {
            return (a + b) / 2.0;
        }

        protected double Distance(double x1, double x2)
        {
            return Math.Abs(x1 - x2);
        }

        protected double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt
            (
                SquareOf(x1 - x2) +
                SquareOf(y1 - y2)
            );
        }

        protected double SquareOf(double x)
        {
            return x * x;
        }

        protected double NextGaussian(double mu = 0, double sigma = 1)
        {
            var u1 = Random.NextDouble();
            var u2 = Random.NextDouble();

            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                Math.Sin(2.0 * Math.PI * u2);

            var randNormal = mu + sigma * randStdNormal;

            return randNormal;
        }

        /// <summary>
        /// Produces a value that is picked from a Gausian distribution centered around the midpoint
        /// of the supplied values and has a standard dev of the distance between the supplied values.
        /// Enforces the upper and lower limits, if supplied.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="limitLow"></param>
        /// <param name="limitHigh"></param>
        /// <returns></returns>
        protected double Cross(double a, double b, double? limitLow = null, double? limitHigh = null)
        {
            //var spin = Random.NextDouble();

            //if (spin < 0.4)
            //    return a;

            //if (spin > 0.4 && spin < 0.9)
            //    return b;

            var midpoint = Midpoint(a, b);
            var distance = Distance(a, b);

            double result;

            do
            {
                result = NextGaussian(midpoint, distance);
            }
            //Enforce bounds, if any
            while ((limitLow.HasValue && result < limitLow) || (limitHigh.HasValue && result > limitHigh));

            return result;
        }
    }
}
