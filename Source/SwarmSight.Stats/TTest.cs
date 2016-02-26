using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;

namespace SwarmSight.Stats
{
    public class TTest
    {
        private static Chart _chart;
        public static bool Busy { get; private set; }

        /// <summary>
        /// Compute the student's t-test statistics for two series of numbers
        /// </summary>
        public static TTestResults Perform(List<double> listA, List<double> listB)
        {
            try
            {
                Busy = true;

                if (_chart == null)
                    _chart = new Chart();

                _chart.Series.Clear();

                AddSeriesToChart(_chart, listA, "A");
                AddSeriesToChart(_chart, listB, "B");

                var tTest = _chart.DataManipulator.Statistics.TTestUnequalVariances(0, 0.05, "A", "B");

                return new TTestResults
                    {
                        TTest = tTest,
                        FirstSeriesName = "A",
                        SecondSeriesName = "B",
                        FirstSeriesCount = listA.Count(),
                        SecondSeriesCount = listB.Count()
                    };
            }
            finally
            {
                Busy = false;
            }
        }

        /// <summary>
        ///     Add points to the Chart object for later t-testing
        /// </summary>
        private static void AddSeriesToChart(Chart chart, List<double> dataPoints, string seriesName)
        {
            var s = new Series(seriesName);

            var i = 0;
            foreach (var point in dataPoints)
            {
                s.Points.Add(new DataPoint
                    {
                        XValue = i,
                        YValues = new[] {point}
                    });

                i++;
            }

            chart.Series.Add(s);
        }
    }
}