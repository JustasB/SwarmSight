using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.ILSpy;
using SwarmVision.Filters;
using SwarmVision.HeadPartsTracking.Models;

namespace SwarmVision.HeadPartsTracking.Algorithms
{
    public abstract class GeneticAlgoBase<T> where T: IDisposable, new()
    {
        protected ThreadSafeRandom Random = new ThreadSafeRandom(1);
        protected Dictionary<T, double> Generation = new Dictionary<T, double>();
        protected int ParentCount = 0;
        public int GenerationSize = 10;
        public int NumberOfGenerations = 2;
        public double MutationProbability = 0.1;
        public double MutationRange = 0.01;
        protected double PercentRandom = 50;
        protected FrameCollection Target;
        protected double FractionToPrune = 0.70;
        protected double InitialFitness = -1;
        public bool SortDescending = true;
        public bool ValidateRandom = true;
        public double BestRecentFitness;
        public int CurrentGeneration;

        public Stopwatch SearchTime = new Stopwatch();
        public Stopwatch ComputeFitnessTime = new Stopwatch();
        public Stopwatch PreProcessTime = new Stopwatch();
        public Stopwatch RandomMemberTime = new Stopwatch();
        public Stopwatch ValidChildTime = new Stopwatch();

        protected abstract T CreateChild(T parent1, T parent2);
        protected bool ValidChildWrapper(T child)
        {
            ValidChildTime.Start();

            var result = ValidChild(child);

            ValidChildTime.Stop();

            return result;
        }
        protected abstract bool ValidChild(T child);
        protected abstract T SelectLocation();
        protected abstract T CreateNewRandomMember();

        public abstract void ComputeFitness();
        public virtual T Mutated(T individual) { return new T(); }

        private int frame = 0;

        public T Search(FrameCollection target)
        {
            SearchTime.Restart();
            ComputeFitnessTime.Reset();
            RandomMemberTime.Reset();
            ValidChildTime.Reset();

            Target = target;

            PreProcessTime.Restart();
            PreProcessTarget();
            PreProcessTime.Stop();

            return default(T);

            //Reset the fitness of each member on new frame
            for(var i = 0; i < Generation.Count; i++)
                Generation[Generation.ElementAt(i).Key] = InitialFitness;

            //Create new generation
            for (CurrentGeneration = 0; CurrentGeneration < NumberOfGenerations; CurrentGeneration++)
            {
                //Add random members as potential parents or initially
                var randomCount = Generation.Count == 0 ? 
                    GenerationSize : 
                    (PercentRandom / 100.0) * GenerationSize;

                RandomMemberTime.Start();
                for (var i = 0; i < randomCount && Generation.Count < GenerationSize; i++)
                {
                    var newItem = CreateNewRandomMember();

                    if ((!ValidateRandom || ValidChildWrapper(newItem)) && !Generation.ContainsKey(newItem))
                        Generation.Add(newItem, InitialFitness);
                    //else
                    //    i--; //Try again
                }
                RandomMemberTime.Stop();

                ParentCount = Generation.Count;

                //Fill the rest with children
                var children = new T[Math.Max(0, GenerationSize - Generation.Count)];

                Parallel.For(0, children.Length, c =>
                {
                    var childValid = false;
                    var child = default(T);
                    var attempts = 0;

                    do
                    {
                        //Pick two random survivors (parents)
                        T randomSurvivor2;
                        T randomSurvivor1;

                        SelectParents(out randomSurvivor1, out randomSurvivor2);

                        //Don't cross with self
                        if (randomSurvivor1.Equals(randomSurvivor2))
                            continue;

                        child = CreateChild(randomSurvivor1, randomSurvivor2);

                        //Don't add invalid locations or already exists
                        if (!Generation.ContainsKey(child) &&
                            ValidChildWrapper(child))
                            childValid = true;

                        attempts++;
                    }
                    while (!childValid && attempts < 3);

                    children[c] = childValid ? child : default(T);
                });

                foreach (var child in children)
                    if(child != null)
                        Generation.Add(child, InitialFitness);
                
                //Mutate a fraction of individuals
                var mutants = new T[Generation.Count];
                Parallel.For(0, Generation.Count, i => 
                {
                    if (Random.NextDouble() < MutationProbability)
                    {
                        var mutant = Mutated(Generation.ElementAt(i).Key);

                        if(ValidChildWrapper(mutant))
                            mutants[i] = mutant;
                    }
                });

                Generation.AddRange(mutants
                    .AsParallel()
                    .Where(m => m != null)
                    .Select(m => new KeyValuePair<T, double>(m, InitialFitness))
                );

                //Evaluate their fitness
                ComputeFitnessTime.Start();
                ComputeFitness();
                ComputeFitnessTime.Stop();

                //Determine how many to keep after pruning
                var keep = (int)((1 - FractionToPrune) * GenerationSize);

                //Sort by fitness
                if (SortDescending)
                    Generation = Generation
                        .OrderByDescending(i => i.Value)
                        .ToDictionary(i => i.Key, i => i.Value);
                else
                    Generation = Generation
                        .OrderBy(i => i.Value)
                        .ToDictionary(i => i.Key, i => i.Value);

                //Dispose of about to be pruned members
                Parallel.For(0, Generation.Count, i => {
                    Generation.ElementAt(i).Key.Dispose();
                });

                //Prune
                Generation = Generation
                    .Take(keep)
                    .ToDictionary(i => i.Key, i => i.Value);

            }

            frame++;

            SearchTime.Stop();

            return SelectLocation();
        }

        public virtual void PreProcessTarget() {}

        public Dictionary<string, double[]> SearchTimings()
        {
            var total = (double) SearchTime.ElapsedMilliseconds;

            var result = new Dictionary<string, double[]>
            {
                { "Total", new[] { total, 1.0 } },
                { "Random", new[] { RandomMemberTime.ElapsedMilliseconds, RandomMemberTime.ElapsedMilliseconds/total } },
                { "Fitness", new[] { ComputeFitnessTime.ElapsedMilliseconds, ComputeFitnessTime.ElapsedMilliseconds/total } },
                { "ValidChild", new[] { ValidChildTime.ElapsedMilliseconds, ValidChildTime.ElapsedMilliseconds/total } },
                { "PreProcess", new[] { PreProcessTime.ElapsedMilliseconds, PreProcessTime.ElapsedMilliseconds/total } },
            };

            foreach (var i in result)
            {
                Debug.WriteLine(i.Key + ": " + result[i.Key][0] + " ms, " + Math.Round(result[i.Key][1]*100, 0) + " %");
            }

            return result;
        }

        public void SetTarget(FrameCollection target)
        {
            Target = target;
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
        public double Cross(double a, double b, double? limitLow = null, double? limitHigh = null)
        {
            if (limitLow.HasValue && limitHigh.HasValue && limitLow > limitHigh)
                return limitLow.Value;

            var midpoint = Midpoint(a, b);
            var distance = Math.Max(1, Distance(a, b));

            var result = NextGaussian(midpoint, distance / 2);

            if (limitLow.HasValue)
                result = Math.Max(limitLow.Value, result);

            if (limitHigh.HasValue)
                result = Math.Min(limitHigh.Value, result);

            return result;
        }

        public double MutateValue(double value, double low, double high)
        {
            var factor = 1 + (Random.NextDouble() * 2 - 1) * MutationRange;

            var newValue = value * factor;

            if (newValue < low || newValue > high)
                return value;

            return newValue;
        }

        protected AngleInDegrees Cross(AngleInDegrees a, AngleInDegrees b)
        {
            return new AngleInDegrees(Cross(a.Value, b.Value, a.Min, a.Max), a.Min, a.Max);
        }

        protected MinMaxDouble Cross(MinMaxDouble a, MinMaxDouble b)
        {
            return new MinMaxDouble(Cross(a, b, a.Min, a.Max), a.Min, a.Max);
        }
        
        protected AngleInDegrees MutateValue(AngleInDegrees a)
        {
            return new AngleInDegrees(MutateValue(a.Value, a.Min, a.Max), a.Min, a.Max);
        }

        protected MinMaxDouble MutateValue(MinMaxDouble a)
        {
            return new MinMaxDouble(MutateValue(a.Value, a.Min, a.Max), a.Min, a.Max);
        }
    }
}
