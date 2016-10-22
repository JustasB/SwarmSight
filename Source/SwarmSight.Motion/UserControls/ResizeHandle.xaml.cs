using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SwarmSight.UserControls
{
    public enum State
    {
        Stationary,
        Hovered,
        HoveredHeld,
        UnhoveredHeld,
    }

    public enum Orientation
    {
        HorizontalMovement,
        VerticalMovement,
        BiDimentionalMovement,
    }

    public partial class ResizeHandle
    {
        /// <summary>
        /// The position of the center of the handle (Y only if BiDimentional)
        /// </summary>
        public int Position
        {
            get
            {
                if (Orientation == Orientation.HorizontalMovement)
                    return (int) (Margin.Left + Width/2);

                else
                    return (int) (Margin.Top + Height/2);
            }
        }

        /// <summary>
        /// Gets the X,Y location of the handle (BiDimensional orientation only)
        /// </summary>
        public Point PositionBiDimentional
        {
            get
            {
                return new Point
                    (
                    (int) (Margin.Left + Width/2),
                    (int) (Margin.Top + Height/2)
                    );
            }
        }

        public State State { get; private set; }
        public Orientation Orientation { get; set; }

        /// <summary>
        /// The smallest left (horizontal orientation) or top (vertical) value that this ResizeHandle can move. Unrestricted by default.
        /// </summary>
        public int? MinimumPosition { get; set; }

        /// <summary>
        /// The largest left (horizontal orientation) or top (vertical) value that this ResizeHandle can move. Unrestricted by default.
        /// </summary>
        public int? MaximumPosition { get; set; }

        public int? MinimumPositionBiDimY { get; set; }
        public int? MaximumPositionBiDimY { get; set; }

        public event EventHandler<EventArgs> HandleMoved;

        public ResizeHandle()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            //Correctly rotate the handle for vertical movement
            if (Orientation == Orientation.VerticalMovement)
            {
                gradientRotator.Angle = 0;
            }

            if (Orientation == Orientation.BiDimentionalMovement)
            {
                //No handle visible in BiDim case
                theBorder.Background = new SolidColorBrush(Colors.Blue);
            }

            //Don't attach events during design time
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            var mainWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault();

            Mouse.AddPreviewMouseDownHandler(this, OnLocalMouseDown);
            Mouse.AddMouseLeaveHandler(this, OnLocalMouseLeave);
            Mouse.AddMouseEnterHandler(this, OnLocalMouseEnter);
            Mouse.AddPreviewMouseMoveHandler(mainWindow, OnGlobalMouseMove);
            Mouse.AddPreviewMouseUpHandler(mainWindow, OnGlobalMouseUp);
        }

        private void OnLocalMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            if (State == State.Stationary)
                SetStateAndRender(State.Hovered);

            else if (State == State.UnhoveredHeld)
                SetStateAndRender(State.HoveredHeld);
        }

        private void OnLocalMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            mouseButtonEventArgs.Handled = false;

            SetStateAndRender(State.HoveredHeld);
        }

        private void OnLocalMouseLeave(object sender, MouseEventArgs mouseEventArgs)
        {
            if (State == State.HoveredHeld)
                SetStateAndRender(State.UnhoveredHeld);

            else if (State == State.Hovered)
                SetStateAndRender(State.Stationary);
        }

        private void OnGlobalMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            if (State == State.HoveredHeld || State == State.UnhoveredHeld)
                MoveHandleToMouseLocation(mouseEventArgs.GetPosition(Parent as UIElement));
        }

        private void OnGlobalMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (State == State.HoveredHeld)
                SetStateAndRender(State.Hovered);

            else if (State == State.UnhoveredHeld)
                SetStateAndRender(State.Stationary);
        }

        private void MoveHandleToMouseLocation(Point mousePosition)
        {
            var newMargin = Margin;

            if (Orientation == Orientation.HorizontalMovement)
            {
                //Handle is centered around the mouse
                var proposedPosition = mousePosition.X - ActualWidth/2;

                //Make sure the handle *center* does not exceed the bounds
                newMargin.Left = Math.Min((MaximumPosition ?? Int32.MaxValue) - ActualWidth/2,
                                          Math.Max((MinimumPosition ?? Int32.MinValue) - ActualWidth/2, proposedPosition)
                    );
            }

            else if (Orientation == Orientation.VerticalMovement)
            {
                var proposedPosition = mousePosition.Y - ActualHeight/2;

                newMargin.Top = Math.Min((MaximumPosition ?? Int32.MaxValue) - ActualHeight/2,
                                         Math.Max((MinimumPosition ?? Int32.MinValue) - ActualHeight/2, proposedPosition)
                    );
            }

            else //BiDimensional
            {
                newMargin.Left = Math.Min((MaximumPosition ?? Int32.MaxValue),
                                          Math.Max((MinimumPosition ?? Int32.MinValue), mousePosition.X)
                    );

                newMargin.Top = Math.Min((MaximumPositionBiDimY ?? Int32.MaxValue),
                                         Math.Max((MinimumPositionBiDimY ?? Int32.MinValue), mousePosition.Y)
                    );
            }

            Margin = newMargin;

            if (HandleMoved != null)
                HandleMoved(this, null);
        }

        private void SetStateAndRender(State newState)
        {
            State = newState;

            if (State == State.Stationary)
                Mouse.OverrideCursor = Cursors.Arrow;

            else
            {
                if (Orientation == Orientation.HorizontalMovement)
                    Mouse.OverrideCursor = Cursors.SizeWE;

                else if (Orientation == Orientation.VerticalMovement)
                    Mouse.OverrideCursor = Cursors.SizeNS;

                else
                    Mouse.OverrideCursor = Cursors.SizeAll;
            }
        }
    }
}