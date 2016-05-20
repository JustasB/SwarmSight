//using SwarmSight.HeadPartsTracking.Algorithms;
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
    /// Interaction logic for ReceptiveField.xaml
    /// </summary>
    public partial class ReceptiveField : UserControl
    {
        public Point initialDims;
        public ReceptiveField()
        {
            InitializeComponent();
            initialDims = new Point(Width, Height);
        }

        public event EventHandler<EventArgs> Moved;
        public event EventHandler<EventArgs> Scaled;
        public event EventHandler<EventArgs> Rotated;

        public Point LeftBase;
        public Point RightBase;
        public Point Position
        {
            get { return gridHead.TranslatePoint(new Point(0, 0), Canvas); }
            set
            {
                var x = value.X - gridHead.Margin.Left + Canvas.Margin.Left;
                var y = value.Y - gridHead.Margin.Top + Canvas.Margin.Top;
                
                Margin = MarginWithinBounds(x, y);
            }
        }

        private Thickness MarginWithinBounds(double x, double y)
        {
            x = Math.Max(Canvas.Margin.Left, Math.Min(Canvas.Margin.Left + Canvas.ActualWidth - Width, x));
            y = Math.Max(Canvas.Margin.Top, Math.Min(Canvas.Margin.Top + Canvas.ActualHeight - Height, y));

            return new Thickness(x, y, 0, 0);
        }

        public double Angle
        {
            get { return gridTransform.Angle; }
            set { gridTransform.Angle = value; }
        }
        public Point Scale
        {
            get { return new Point(gridScale.ScaleX, gridScale.ScaleY); }
            set { gridScale.ScaleX = value.X; gridScale.ScaleY = value.Y; }
        }
        public Point Dimensions
        {
            get { return new Point(gridHead.ActualWidth, gridHead.ActualHeight); }
            set
            {
                Width = value.X + gridHead.Margin.Left + gridHead.Margin.Right;
                Height = value.Y + gridHead.Margin.Top + gridHead.Margin.Bottom;

                Width = Math.Max(50, Math.Min(Canvas.ActualWidth- gridHead.Margin.Left*2, Width));
                Height = Math.Max(50, Math.Min(Canvas.ActualHeight- gridHead.Margin.Left*2, Height));
            }
        }

        public Point mouseOffset;
        public bool mouseCaptured = false;
        public double startAngle = 0;
        public Thickness startPos;
        public Point startDims;
        public Point startScale;
        internal Image Canvas;

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture((UIElement)sender);
            mouseCaptured = true;
            startPos = Margin;
            startAngle = gridTransform.Angle;
            startScale = new Point(gridScale.ScaleX, gridScale.ScaleY);
            startDims = new Point(Width, Height);
            mouseOffset = Mouse.GetPosition(Canvas);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            mouseCaptured = false;
        }

        private void SCALE_move(object sender, MouseEventArgs e)
        {
            if (mouseCaptured)
            {
                var pos = Mouse.GetPosition(Canvas);
                gridScale.ScaleY = Math.Min(5, Math.Max(0.1, startScale.Y + (pos.Y - mouseOffset.Y) / 100));
                gridScale.ScaleX = Math.Min(5, Math.Max(0.1, startScale.X + (pos.X - mouseOffset.X) / 100));
                
                var maxScale = Math.Max(gridScale.ScaleX, gridScale.ScaleY);
                
                Width = initialDims.X * maxScale;
                Height = initialDims.Y * maxScale;

                var dimDiff = new Point(Width - startDims.X, Height - startDims.Y);

                Margin = new Thickness(startPos.Left - dimDiff.X / 2.0, startPos.Top - dimDiff.Y / 2.0, 0, 0);

                if (Scaled != null)
                    Scaled(this, e);
            }
        }

        private void ROTATE_move(object sender, MouseEventArgs e)
        {
            if (mouseCaptured)
            {
                var pos = Mouse.GetPosition(Canvas);
                gridTransform.Angle = startAngle + pos.X - mouseOffset.X;

                if (Rotated != null)
                    Rotated(this, e);
            }
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
        

        private void imgLeftBase_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void imgRightBase_MouseMove(object sender, MouseEventArgs e)
        {

        }
    }
}
