using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SwarmVision.VideoPlayer
{
    public abstract class GeneticAlgoBase<T> where T: IDisposable
    {
        protected Random Random = new Random();
        protected Dictionary<T, double> Generation = new Dictionary<T, double>();
        protected int SurvivorCount = 0;
        protected int GenerationSize = 30;
        protected int NumberOfGenerations = 10;
        protected double PercentRandom = 0.05;
        protected Frame Target;
        protected int PercentPruneLow = 20;
        protected int PercentPruneHigh = 80;
        protected double InitialFitness = -1;

        protected abstract T CreateChild(T parent1, T parent2);
        protected abstract bool ValidChild(T child);
        protected abstract T SelectLocation();
        protected abstract double ComputeFitness(T individual);
        protected abstract T CreateNewRandomMember();

        public T GeneticSearch(Frame target)
        {
            Target = target;

            //Reset the fitness of each member on new frame
            for (var i = 0; i < Generation.Count; i++)
                Generation[Generation.ElementAt(i).Key] = InitialFitness;

            //Create new generation
            for (var g = 0; g < NumberOfGenerations; g++)
            {
                //If there are existing members
                if (Generation.Count > 0)
                {
                    SurvivorCount = Generation.Count;

                    //Leave room to random members
                    while (Generation.Count < (int)((1 - PercentRandom) * GenerationSize))
                    {
                        //Pick two random survivors (parents)
                        T randomSurvivor1;
                        T randomSurvivor2;

                        SelectParents(out randomSurvivor1, out randomSurvivor2);

                        var child = CreateChild(randomSurvivor1, randomSurvivor2);

                        //Don't add invalid locations or already exists
                        if (!Generation.ContainsKey(child) && ValidChild(child))
                            Generation.Add(child, InitialFitness);
                    }
                }

                //Fill rest with randoms (and initially too)
                while (Generation.Count < GenerationSize)
                {
                    var newItem = CreateNewRandomMember();

                    if (ValidChild(newItem) && !Generation.ContainsKey(newItem))
                        Generation.Add(newItem, InitialFitness);
                }

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

                //Prune least fit X-Y% of population
                var keep = (int) (Random.Next(PercentPruneLow, PercentPruneHigh + 1)/100.0*GenerationSize);

                //Sort by fitness
                Generation = Generation
                    .OrderBy(i => i.Value)
                    .ToDictionary(i => i.Key, i => i.Value);

                //Dispose of any about to be pruned members
                for (var i = keep; i < Generation.Count; i++)
                    Generation.ElementAt(i).Key.Dispose();

                Generation = Generation
                    .Take(keep)
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
