using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for AtennaSensor.xaml
    /// </summary>
    public partial class AntennaSensor : UserControl
    {
        public double CanvasScale = 1.0;

        public AntennaSensor()
        {
            InitializeComponent();            
        }

        public event EventHandler<EventArgs> MouseDown;
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

        private Point DimsWithinBounds(double w, double h)
        {
            w = Math.Max(50, Math.Min(Canvas.ActualWidth, w));
            h = Math.Max(50, Math.Min(Canvas.ActualHeight, h));

            return new Point(w,h);
        }

        private Thickness MarginWithinBounds(double x, double y)
        {
            x = Math.Max(Canvas.Margin.Left, Math.Min(Canvas.Margin.Left + Canvas.ActualWidth - Width, x));
            y = Math.Max(Canvas.Margin.Top, Math.Min(Canvas.Margin.Top + Canvas.ActualHeight - Height, y));

            return new Thickness(x, y, 0, 0);
        }

        public void ChangeCanvasScale(double newCanvasScale)
        {
            if (Canvas == null)
                return;

            //xy changes
            Position = new Point(Position.X / CanvasScale * newCanvasScale, Position.Y / CanvasScale * newCanvasScale);

            //dimentions change
            Dimensions = new Point(Dimensions.X / CanvasScale * newCanvasScale, Dimensions.Y / CanvasScale * newCanvasScale);
        }

        public double Angle { get; set; }
        public double Scale { get; private set; }

        public Point Dimensions
        {
            get { return new Point(gridHead.ActualWidth, gridHead.ActualHeight); }
            set
            {
                Width = value.X + gridHead.Margin.Left + gridHead.Margin.Right;
                Height = value.Y + gridHead.Margin.Top + gridHead.Margin.Bottom;

                Width = Math.Max(50, Math.Min(Canvas.ActualWidth - gridHead.Margin.Left * 2, Width));
                Height = Math.Max(50, Math.Min(Canvas.ActualHeight - gridHead.Margin.Top * 2, Height));
            }
        }

        public Point mouseOffset;
        public bool mouseCaptured = false;
        public string changeType = "";
        public double startAngle = 0;
        public Point startPos;
        public Point startDims;
        internal Image Canvas;

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture((UIElement)sender);
            mouseCaptured = true;
            changeType = "";
            startPos = Position;
            startAngle = Angle;
            startDims = Dimensions;
            mouseOffset = Mouse.GetPosition(Canvas);

            if (MouseDown != null)
                MouseDown(this, null);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            mouseCaptured = false;
            changeType = "";
        }

        private void SCALE_move(object sender, MouseEventArgs e)
        {
            if (mouseCaptured && (changeType == "" || changeType == "scale"))
            {
                changeType = "scale";

                var pos = Mouse.GetPosition(Canvas);
                
                var xMoved = pos.X - mouseOffset.X;
                var yMoved = pos.Y - mouseOffset.Y;

                var maxMoved = Math.Max(xMoved, yMoved);

                Position = new Point(
                    startPos.X - maxMoved,
                    startPos.Y - maxMoved
                );

                Dimensions = new Point(
                    startDims.X + 2*maxMoved,
                    startDims.Y + 2*maxMoved
                );

                Scale = Dimensions.X / CanvasScale / 100.0;

                if (Scaled != null)
                    Scaled(this, e);
            }
        }

        private void ROTATE_move(object sender, MouseEventArgs e)
        {
            if (mouseCaptured && (changeType == "" || changeType == "rotate"))
            {
                changeType = "rotate";
                
                var pos = Mouse.GetPosition(Canvas);

                Angle = startAngle + pos.X - mouseOffset.X;

                if (Rotated != null)
                    Rotated(this, e);
            }
        }

        private void TRANSLATE_move(object sender, MouseEventArgs e)
        {
            if (mouseCaptured && (changeType == "" || changeType == "translate"))
            {
                changeType = "translate";

                var pos = Mouse.GetPosition(Canvas);

                var x = pos.X - mouseOffset.X;
                var y = pos.Y - mouseOffset.Y;

                Position = new Point(startPos.X + x, startPos.Y + y);

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
