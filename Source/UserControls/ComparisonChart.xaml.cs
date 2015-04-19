using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SwarmVision.UserControls
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
            //return;
            SetupChart();

            UpdateChart(100, 10, 200, 20);
        }

        private void SetupChart()
        {
            model = new PlotModel()
            {
                
            };

            var axis = new OxyPlot.Axes.CategoryAxis()
            {

            };

            axis.Labels.AddRange(new[] { "Video A", "Video B" });

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

            series.Items.Add(new ErrorColumnItem(avgA, errA));
            series.Items.Add(new ErrorColumnItem(avgB, errB));

            model.InvalidatePlot(true);
        }
    }
}
