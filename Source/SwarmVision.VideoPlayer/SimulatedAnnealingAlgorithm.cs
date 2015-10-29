using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using SwarmVision.Filters;
using SwarmVision.Models;
using SwarmVision.Hardware;

namespace SwarmVision.VideoPlayer
{
    public class SimulatedAnnealingAlgorithm
    {
        public double StartTemperature = 1000;
        public double CoolRate = 0.03;
        public double StopTemperature = 1.0;

        private Frame Target;
        private Random Random = new Random(1);
        private double _currentTemperature;
        private HeadModel _currentSolution;
        private double _currentBestEnergy;

        public HeadModel Search(Frame target)
        {
            Target = target;

            if (_currentSolution == null)
            {
                _currentTemperature = StartTemperature;
                _currentSolution = RandomSolution();
                _currentBestEnergy = Evaluate(_currentSolution);
            }
            else
            {
                _currentTemperature = StartTemperature*0.1;
            }

            var i = 0;

            while (_currentTemperature > StopTemperature)
            {
                var neighbour = NeighbourOf(_currentSolution);
                var neighbourEnergy = Evaluate(neighbour);

                if (neighbourEnergy < _currentBestEnergy)
                {
                    _currentSolution.View.Dispose();
                    _currentSolution = neighbour;
                    _currentBestEnergy = neighbourEnergy;
                }
                else
                {
                    var energyDistance = _currentBestEnergy/neighbourEnergy;
                    var acceptanceP = (_currentTemperature/StartTemperature + energyDistance)*0.5*0.2;

                    if (acceptanceP > Random.NextDouble())
                    {
                        _currentSolution.View.Dispose();
                        _currentSolution = neighbour;
                        _currentBestEnergy = neighbourEnergy;
                    }
                }

                _currentTemperature *= (1 - CoolRate);
                i++;

                Debug.WriteLine(i + " " + _currentBestEnergy);
            }

            Target.DrawFrame((Frame)_currentSolution.View, (int) _currentSolution.Origin.X, (int) _currentSolution.Origin.Y);
            _currentSolution.View.Dispose();


            return _currentSolution;
        }

        public HeadModel RandomSolution()
        {
            //HEAD
            var result = new HeadModel
            {
                Origin = new Point
                (
                    Random.Next(0, Target.Width - HeadView.Width),
                    Random.Next(0, Target.Height - HeadView.Height)
                ),
                AngleIndex = Random.NextDouble(),
            };

            result.Scale = Random.NextDouble()*(result.ScaleMax - result.ScaleMin) + result.ScaleMin;

            //PROBOSCIS - MAIN SEGMENT
            result.Proboscis.Proboscis.AngleIndex = Random.NextDouble();
            result.Proboscis.Proboscis.Length = Random.Next
            (
                (int)result.Proboscis.Proboscis.LengthMin, 
                (int)result.Proboscis.Proboscis.LengthMax
            );

            //PROBOSCIS - TONGUE
            result.Proboscis.Tongue.AngleIndex = Random.NextDouble();
            result.Proboscis.Tongue.Length = Random.Next
            (
                (int)result.Proboscis.Tongue.LengthMin,
                (int)result.Proboscis.Tongue.LengthMax
            );

            //MANDIBLE
            result.Mandible.AngleIndex = Random.NextDouble();
            result.Mandible.Length = Random.Next
            (
                (int)result.Mandible.LengthMin,
                (int)result.Mandible.LengthMax
            );

            //ANTENA - LEFT - ROOT
            result.LeftAntena.Root.AngleIndex = Random.NextDouble();
            result.LeftAntena.Root.Length = Random.Next
            (
                (int)result.LeftAntena.Root.LengthMin,
                (int)result.LeftAntena.Root.LengthMax
            );

            //ANTENA - LEFT - TIP
            result.LeftAntena.Tip.AngleIndex = Random.NextDouble();
            result.LeftAntena.Tip.Length = Random.Next
            (
                (int)result.LeftAntena.Tip.LengthMin,
                (int)result.LeftAntena.Tip.LengthMax
            );

            //ANTENA - RIGHT - ROOT
            result.RightAntena.Root.AngleIndex = Random.NextDouble();
            result.RightAntena.Root.Length = Random.Next
            (
                (int)result.RightAntena.Root.LengthMin,
                (int)result.RightAntena.Root.LengthMax
            );

            //ANTENA - RIGHT - TIP
            result.RightAntena.Tip.AngleIndex = Random.NextDouble();
            result.RightAntena.Tip.Length = Random.Next
            (
                (int)result.RightAntena.Tip.LengthMin,
                (int)result.RightAntena.Tip.LengthMax
            );

            return result;
        }

        public double Evaluate(HeadModel candidate)
        {
            if (candidate.View == null)
            {
                candidate.View = CreateHeadView(candidate);
            }

            var result = Target.AverageColorDifference((Frame)candidate.View, (int)candidate.Origin.X, (int)candidate.Origin.Y);

            //Adjust for scale
            result /= candidate.Scale;

            return result;
        }

        protected Frame CreateHeadView(HeadModel head)
        {
            using (var raw = new HeadView(head).Draw(GPU.UseGPU))
            using (var edged = raw.EdgeFilter())
            {
                return edged.RotateScale(head.Angle, head.Scale);
            }
        }

        public HeadModel NeighbourOf(HeadModel candidate)
        {
            //Alter a random one of X, Y, scale, or angle

            var neighbour = candidate.Clone();
            var alterationIndex = Random.Next(0, 4);
            
            switch (alterationIndex)
            {
                case 0:
                    neighbour.Origin.X = NextGaussian(neighbour.Origin.X, Target.Width/4.0, 0, Target.Width);
                    break;
                case 1:
                    neighbour.Origin.Y = NextGaussian(neighbour.Origin.Y, Target.Height/4.0, 0, Target.Height);
                    break;
                case 2:
                    neighbour.AngleIndex = NextGaussian(neighbour.AngleIndex, 0.25, 0, 1);
                    break;
                default:
                    neighbour.Scale = NextGaussian(neighbour.Scale, 0.1, neighbour.ScaleMin, neighbour.ScaleMax);
                    break;
            }

            return neighbour;
        }

        protected double NextGaussian(double mu, double sigma, double lowLimit, double highLimit)
        {
            double result;
            var attempts = 0;

            do
            {
                result = NextGaussian(mu, sigma);

                attempts++;
            }
            while (attempts < 100 && (lowLimit > result || result > highLimit));

            return attempts >= 100 ? mu : result;
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
