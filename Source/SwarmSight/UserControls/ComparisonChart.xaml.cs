using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System.Windows.Controls;

namespace SwarmSight.UserControls
{
    /// <summary>
    /// Interaction logic for ComparisonChart.xaml
    /// </summary>
    public partial class ComparisonChart : UserControl
    {
        private PlotModel model;
        private ErrorColumnSeries series;


        public ComparisonChart()
        {
            InitializeComponent();

            SetupChart();
        }

        private void SetupChart()
        {
            model = new PlotModel()
                {
                };

            var linearAxis = new OxyPlot.Axes.LinearAxis()
                {
                    MinimumPadding = 0,
                    MaximumPadding = 0.1,
                };

            var axis = new OxyPlot.Axes.CategoryAxis()
                {
                };

            axis.Labels.AddRange(new[] {"Video A", "Video B"});

            model.Axes.Add(linearAxis);
            model.Axes.Add(axis);

            series = new ErrorColumnSeries()
                {
                    StrokeThickness = 1,
                };

            model.Series.Add(series);

            chart.Children.Add(new PlotView()
                {
                    Model = model,
                    Width = chart.Width,
                    Height = chart.Height,
                });
        }

        public void UpdateChart(double avgA, double errA, double avgB, double errB)
        {
            series.Items.Clear();

            series.Items.Add(new ErrorColumnItem(avgA, errA) {Color = OxyColors.Aqua});
            series.Items.Add(new ErrorColumnItem(avgB, errB) {Color = OxyColors.Aqua});

            model.InvalidatePlot(true);
        }
    }
}