using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace SwarmVision.Stats
{
    public class ChartModel
    {
        public bool KeepRefreshing = true;

        private readonly LineSeries _series;
        private readonly LinearAxis _Xaxis;
        private LinkedList<DataPoint> pointQueue = new LinkedList<DataPoint>();
        private Thread queueMonitor;

        public ChartModel()
        {
            MyModel = new PlotModel()
            {
                IsLegendVisible = false,
                PlotAreaBorderThickness = new OxyThickness(0),
            };

            _series = new LineSeries()
            {
                Color = OxyColor.FromRgb(0, 0, 0),
                StrokeThickness = 0.5,
                Title = "Changed Pixels"
            };

            _Xaxis = new LinearAxis()
            {
                IsAxisVisible = false,
                Position = AxisPosition.Bottom,
                MaximumPadding = 0,
                MinimumPadding = 0,
            };

            var logAxis = new LogarithmicAxis()
            {
                IsAxisVisible = false,
                Minimum = 1,
                UseSuperExponentialFormat = true,
                MaximumPadding = 0.1,
                MinimumPadding = 0,
            };

            MyModel.Axes.Add(_Xaxis);
            MyModel.Axes.Add(logAxis);

            MyModel.Updated += (sender, args) =>
            {
                lock (_pointBuffer)
                {
                    foreach (var pt in _pointBuffer)
                    {
                        _series.Points.Add(pt);
                    }

                    _pointBuffer.Clear();
                }
            };

            this.MyModel.Series.Add(_series);
        }

        public void SetRange(int startRange, int endRange)
        {
            Clear();

            _Xaxis.Minimum = startRange;
            _Xaxis.Maximum = endRange;


            MyModel.InvalidatePlot(true);
        }

        private static readonly object Lockpad = new object();

        private static object lockpad = new object();

        private readonly List<DataPoint> _pointBuffer = new List<DataPoint>();
        public void AddPoint(int x, int y)
        {
            lock (_pointBuffer)
            {
                _pointBuffer.Add(new DataPoint(x, y));
            }

            if (queueMonitor != null)
                return;

            lock (lockpad)
            {
                if (queueMonitor != null)
                    return;

                queueMonitor = new Thread(() =>
                {
                    while (KeepRefreshing)
                    {
                        Thread.Sleep(150);

                        MyModel.InvalidatePlot(true);
                    }
                })
                { IsBackground = true };

                queueMonitor.Start();
            }
        }

        public PlotModel MyModel { get; private set; }

        public void Stop()
        {
            try
            {
                queueMonitor.Abort();
            }
            catch
            {
            }

            queueMonitor = null;
        }

        public void Clear()
        {
            _series.Points.Clear();

            MyModel.InvalidatePlot(true);
        }

        public void ClearAfter(int xInclusive)
        {
            _series.Points.RemoveAll(p => p.X >= xInclusive);

            MyModel.InvalidatePlot(true);
        }
    }
}