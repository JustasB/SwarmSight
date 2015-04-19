using System;
using System.Windows.Forms.DataVisualization.Charting;

namespace SwarmVision.Stats
{
    public class TTestResults
    {
        public TTestResult TTest { get; set; }

        public string FirstSeriesName { get; set; }
        public string SecondSeriesName { get; set; }

        public int FirstSeriesCount { get; set; }
        public int SecondSeriesCount { get; set; }


        public double FirstSeriesStandardDeviation
        {
            get { return Math.Sqrt(TTest.FirstSeriesVariance); }
        }

        public double SecondSeriesStandardDeviation
        {
            get { return Math.Sqrt(TTest.SecondSeriesVariance); }
        }


        public double MeanDifference
        {
            get { return TTest.SecondSeriesMean - TTest.FirstSeriesMean; }
        }

        public double PercentMeanDifference
        {
            get { return MeanDifference/TTest.FirstSeriesMean; }
        }

        public double FirstSeriesStandardError
        {
            get { return FirstSeriesStandardDeviation/Math.Sqrt(FirstSeriesCount); }
        }

        public double SecondSeriesStandardError
        {
            get { return SecondSeriesStandardDeviation/Math.Sqrt(SecondSeriesCount); }
        }

        public double FirstSeries95ConfidenceBound
        {
            get { return FirstSeriesStandardError*1.96; }
        }

        public double SecondSeries95ConfidenceBound
        {
            get { return SecondSeriesStandardError*1.96; }
        }
    }
}