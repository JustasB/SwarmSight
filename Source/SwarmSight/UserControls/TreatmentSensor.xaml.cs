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

namespace SwarmSight.UserControls
{
    /// <summary>
    /// Interaction logic for TreatmentSensor.xaml
    /// </summary>
    public partial class TreatmentSensor : UserControl
    {
        public TreatmentSensor()
        {
            InitializeComponent();
        }
        
        public Image Canvas;
        public event EventHandler<EventArgs> Moved;

        public string SensorValue
        {
            get { return (string)brightness.Content; }
            set { brightness.Content = value; }
        }

        public Point Position
        {
            get { return sensor.TranslatePoint(new Point(1, 1), Canvas); }
            set
            {
                var x = -1 + value.X - ellipseGrid.Margin.Left - sensor.Margin.Left + Canvas.Margin.Left;
                var y = -1 + value.Y - ellipseGrid.Margin.Top - sensor.Margin.Top + Canvas.Margin.Top;

                Margin = MarginWithinBounds(x, y);
            }
        }

        private Thickness MarginWithinBounds(double x, double y)
        {
            x = Math.Max(Canvas.Margin.Left-Width/2+1, Math.Min(Canvas.Margin.Left + Canvas.ActualWidth - Width / 2-1, x));
            y = Math.Max(Canvas.Margin.Top-Height/2-6, Math.Min(Canvas.Margin.Top + Canvas.ActualHeight - Height / 2-7, y));

            return new Thickness(x, y, 0, 0);
        }

        public Point mouseOffset;
        public bool mouseCaptured = false;
        public Thickness startPos;

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture((UIElement)sender);
            mouseCaptured = true;
            startPos = Margin;
            mouseOffset = Mouse.GetPosition(Canvas);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            mouseCaptured = false;
        }


        private void TRANSLATE_move(object sender, MouseEventArgs e)
        {
            if (mouseCaptured)
            {
                var pos = Mouse.GetPosition(Canvas);

                var x = startPos.Left + pos.X - mouseOffset.X;
                var y = startPos.Top + pos.Y - mouseOffset.Y;

                Margin = MarginWithinBounds(x, y);

                if (Moved != null)
                    Moved(this, e);
            }
        }
    }
}
