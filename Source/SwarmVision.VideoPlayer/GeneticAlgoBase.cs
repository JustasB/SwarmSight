using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SwarmVision.VideoPlayer
{
    public abstract class GeneticAlgoBase<T>
    {
        protected Random Random = new Random();
        protected Dictionary<T, double> Generation = new Dictionary<T, double>();
        protected int SurvivorCount = 0;
        protected int GenerationSize = 30;
        protected int NumberOfGenerations = 10;
        protected double PercentRandom = 0.05;
        protected Frame Target;
        protected Frame Pattern;
        protected int PercentPruneLow = 20;
        protected int PercentPruneHigh = 80;

        protected abstract T CreateChild(T parent1, T parent2);
        protected abstract bool ValidChild(T child);
        protected abstract T SelectLocation();
        protected abstract double ComputeFitness(T individual);
        protected abstract T CreateNewRandomMember();

        public T GeneticSearch(Frame target, Frame pattern)
        {
            //Create new generation
            for (var g = 0; g < NumberOfGenerations; g++)
            {
                //If there are existing members
                if (Generation.Count > 0)
                {
                    SurvivorCount = Generation.Count;

                    //Leave room to random members
                    while (Generation.Count < (int)((1-PercentRandom) * GenerationSize))
                    {
                        //Pick two random survivors (parents)
                        T randomSurvivor1;
                        T randomSurvivor2;

                        SelectParents(out randomSurvivor1, out randomSurvivor2);

                        var child = CreateChild(randomSurvivor1, randomSurvivor2);

                        //Don't add invalid locations or already exists
                        if (!Generation.ContainsKey(child) && ValidChild(child))
                            Generation.Add(child, 0);
                    }
                }

                //Fill rest with randoms (and initially too)
                while (Generation.Count < GenerationSize)
                {
                    var newItem = CreateNewRandomMember();

                    if (ValidChild(newItem) && !Generation.ContainsKey(newItem))
                        Generation.Add(newItem, 0);
                }

                //Evaluate fitness
                for (var i = 0; i < GenerationSize; i++)
                {
                    var individual = Generation.ElementAt(i);

                    //If fitness already computed, don't recompute
                    if (individual.Value > -1)
                        continue;

                    var fitness = ComputeFitness(individual.Key);

                    Generation[individual.Key] = fitness;
                }

                //Prune least fit X-Y% of population
                Generation = Generation
                    .OrderBy(i => i.Value)
                    .Take((int)(Random.Next(PercentPruneLow, PercentPruneHigh+1) / 100.0 * GenerationSize))
                    .ToDictionary(i => i.Key, i => i.Value);
            }

            return SelectLocation();
        }

        protected virtual void SelectParents(out T parent1, out T parent2)
        {
            parent1 = Generation.ElementAt(Random.Next(SurvivorCount)).Key;
            parent2 = Generation.ElementAt(Random.Next(SurvivorCount)).Key;
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
    }
}
