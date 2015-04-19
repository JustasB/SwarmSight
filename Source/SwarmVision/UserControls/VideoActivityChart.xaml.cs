using OxyPlot.Wpf;
using SwarmVision.Stats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SwarmVision.UserControls
{
    /// <summary>
    /// Interaction logic for VideoActivityChart.xaml
    /// </summary>
    public partial class VideoActivityChart : UserControl
    {
        public List<Point> Activity
        {
            get
            {
                return _activity
                    .Where(p => p.X >= LowerBound && p.X <= UpperBound)
                    .ToList();
            }
        }

        public int LowerBound
        {
            get { return (int) (Math.Round(SelectionBeginPercent*_activity.Count, 0)); }
        }

        public int UpperBound
        {
            get { return (int) (Math.Round(SelectionEndPercent*_activity.Count, 0)); }
        }

        public double SelectionBeginPercent
        {
            get
            {
                var selectionX = selectionRectangle.Margin.Left;
                var chartBeginX = chartPlaceholder.Margin.Left;
                var chartWidth = chartPlaceholder.Width;

                return (selectionX - chartBeginX)/chartWidth;
            }
        }

        public double SelectionEndPercent
        {
            get
            {
                var selectionX = selectionRectangle.Margin.Left + selectionRectangle.Width;
                var chartBeginX = chartPlaceholder.Margin.Left;
                var chartWidth = chartPlaceholder.Width;

                return (selectionX - chartBeginX)/chartWidth;
            }
        }

        private List<Point> _activity;

        private ChartModel _chart;

        public EventHandler<EventArgs> UseCurrentClicked;
        public EventHandler<EventArgs> SelectionChanged;

        public VideoActivityChart()
        {
            InitializeComponent();

            SetupChart();

            Dispatcher.ShutdownStarted += (sender, args) => _chart.KeepRefreshing = false;
        }

        public void AddPointsToChart(List<Point> points)
        {
            _activity = new List<Point>(points);

            _chart.Clear();
            _activity.ForEach(p => _chart.AddPoint((int) p.X, (int) p.Y));

            UpdateBoundsLabels();
        }

        private void SetupChart()
        {
            _chart = new ChartModel();
            _activity = new List<Point>(100);

            chartPlaceholder.Children.Add(new PlotView()
                {
                    Model = _chart.MyModel,
                    Width = chartPlaceholder.Width,
                    Height = chartPlaceholder.Height,
                });

            //Restrict handles to not exceed chart bounds
            leftHandle.MinimumPosition = rightHandle.MinimumPosition = (int) chartPlaceholder.Margin.Left;
            leftHandle.MaximumPosition =
                rightHandle.MaximumPosition = (int) chartPlaceholder.Margin.Left + (int) chartPlaceholder.Width;

            leftHandle.HandleMoved += (sender, args) =>
                {
                    //Restrict handles to not exceed each other
                    rightHandle.MinimumPosition = leftHandle.Position;

                    UpdateSelectionRectangle();
                };

            rightHandle.HandleMoved += (sender, args) =>
                {
                    leftHandle.MaximumPosition = rightHandle.Position;

                    UpdateSelectionRectangle();
                };
        }

        private void UpdateSelectionRectangle()
        {
            //Update the selection rectangle bounds to track the handles
            var newMargin = selectionRectangle.Margin;
            newMargin.Left = leftHandle.Position;
            selectionRectangle.Margin = newMargin;

            selectionRectangle.Width = Math.Max(2, rightHandle.Position - leftHandle.Position);

            UpdateBoundsLabels();

            if (SelectionChanged != null)
                SelectionChanged(this, null);
        }


        private void UpdateBoundsLabels()
        {
            lblStartFrame.Content = "Start Frame: " + (LowerBound + 1);
            lblEndFrame.Content = "End Frame: " + (UpperBound + 1);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (UseCurrentClicked != null)
                UseCurrentClicked(this, null);
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog();

            var result = ofd.ShowDialog();

            if (result == false)
                return;

            txtFileName.Text = ofd.FileName;

            ReadValuesFromFile();
        }

        private void ReadValuesFromFile()
        {
            if (!File.Exists(txtFileName.Text) || !txtFileName.Text.EndsWith(".csv"))
            {
                MessageBox.Show("Please select a valid CSV file");
                return;
            }

            try
            {
                var parsedActivity = File
                    .ReadAllLines(txtFileName.Text)
                    .Skip(1) //Header
                    .Select(line =>
                        {
                            var spitLine = line.Split(',');
                            var x = int.Parse(spitLine[0]); //Frame number
                            var y = int.Parse(spitLine[1]); //Changed pixels
                            return new Point(x, y);
                        })
                    .ToList();

                AddPointsToChart(parsedActivity);
            }
            catch
            {
                MessageBox.Show(
                    "Could not read CSV file. Please make sure the first column has frame numbers and the second column has the changed pixels.");
            }
        }

        private void txtFileName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ReadValuesFromFile();
            }
        }
    }
}