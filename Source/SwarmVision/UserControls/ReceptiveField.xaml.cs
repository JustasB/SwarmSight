using SwarmVision.HeadPartsTracking.Algorithms;
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
        public Point Position { get { return gridHead.TranslatePoint(new Point(0, 0), Canvas); } }
        public double Angle {  get { return gridTransform.Angle; } }
        public Point Scale {  get { return new Point(gridScale.ScaleX, gridScale.ScaleY); } }
        public Point Dimensions { get { return new Point(gridHead.ActualWidth, gridHead.ActualHeight); } }

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
                Margin = new Thickness(startPos.Left + pos.X - mouseOffset.X, startPos.Top + pos.Y - mouseOffset.Y, 0, 0);
                
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
