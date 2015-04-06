using System.Collections.Generic;
using System.Threading;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace SwarmVision
{
    public class MainViewModel
    {
        private readonly LineSeries _series;
        private readonly LinearAxis _Xaxis;
        private LinkedList<DataPoint> pointQueue = new LinkedList<DataPoint>();
        private Thread queueMonitor;

        public MainViewModel()
        {
            MyModel = new PlotModel()
            {
                IsLegendVisible = false,
                PlotAreaBorderThickness = new OxyThickness(0),
            };

            _series = new LineSeries("Changed Pixels")
            {
                Color = OxyColor.FromRgb(0,0,0),
                StrokeThickness = 0.5,
            };

            _Xaxis = new LinearAxis()
            {
                IsAxisVisible = false,
                Position = AxisPosition.Bottom,
                MaximumPadding = 0,
                MinimumPadding = 0,
            };

            var logAxis = new LinearAxis()
            {
                IsAxisVisible = false,
                Minimum = 1,
                UseSuperExponentialFormat = true,
                MaximumPadding = 0.5,
                MinimumPadding = 0,
            };

            MyModel.Axes.Add(_Xaxis);
            MyModel.Axes.Add(logAxis);

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

        public void AddPoint(int x, int y)
        {
            pointQueue.AddLast(new DataPoint(x, y));

            if (queueMonitor == null)
            {
                queueMonitor = new Thread(() =>
                {
                    while (true)
                    {
                        while (pointQueue.Count > 0)
                        {
                            var point = pointQueue.First.Value;
                            _series.Points.Add(point);
                            pointQueue.Remove(point);
                        }

                        MyModel.InvalidatePlot(true);

                        Thread.Sleep(20);
                    }
                });

                queueMonitor.Start();
            }
        }

        public PlotModel MyModel { get; private set; }

        public void Stop()
        {
            try { queueMonitor.Abort(); }
            catch {}

            queueMonitor = null;
        }

        internal void Clear()
        {
            _series.Points.Clear();

            MyModel.InvalidatePlot(true);
        }

        internal void ClearAfter(int xInclusive)
        {
            _series.Points.RemoveAll(p => p.X >= xInclusive);

            MyModel.InvalidatePlot(true);
        }
    }
}