using Settings;
using SwarmSight.HeadPartsTracking.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SwarmSight.Helpers
{
    public class ExclusionZoneManager
    {
        public ProcessorWindow window;
        public event EventHandler Changed;

        public void AddClicked()
        {
            window.Stop();

            if (Changed != null)
                Changed(this, null);

            if (!AppSettings.Default.AddExclusionMsgSeen)
            {
                MessageBox.Show("Draw a polygon around the area you want the antenna sensor to ignore");

                AppSettings.Default.AddExclusionMsgSeen = true;
                AppSettings.Default.SaveAsync();
            }

            StartDrawingExclusionZone();
        }

        private void StartDrawingExclusionZone()
        {
            window.Cursor = Cursors.Cross;
            startingPoint = null;
            allLines = new List<Line>();

            //Makes the shim clickable
            window.exclusionShim.Visibility = Visibility.Visible;
        }

        private void StopDrawingExclusionZone()
        {
            window.btnRemoveExclusion.Visibility = Visibility.Visible;
            window.Cursor = Cursors.Arrow;
            startingPoint = null;

            //Makes the shim un-clickable
            window.exclusionShim.Visibility = Visibility.Hidden;

            var poly = new Polygon()
            {
                Stroke = polyColor,
                Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)),
                Points = new PointCollection
                (
                    allLines
                    .Select(l => new Point(l.X1, l.Y1))
                    .ToList()
                )
            };

            poly.MouseUp += Poly_MouseUp;

            exclusionZones.Add(poly);
            window.videoGrid.Children.Add(poly);
            allLines.ForEach(l => window.videoGrid.Children.Remove(l));

            SaveToSettings();
        }

        private void SaveToSettings()
        {
            AppSettings.Default.ExclusionZones =
                string.Join("|", exclusionZones.Select(z => string.Join(";", z.Points.Select(p => { p.Offset(-10,-10); var pv = window.ToVideoCoordinates(p); return pv.X + "," + pv.Y; }))));

            AppSettings.Default.SaveAsync();
        }

        bool isDrawingExclusion = false;
        Point? startingPoint;
        Line currentLine;
        List<Line> allLines = new List<Line>();
        Brush polyColor = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        List<Polygon> exclusionZones = new List<Polygon>();
        public void MouseDown()
        {
            var pos = Mouse.GetPosition(window.videoGrid);

            if (isDrawingExclusion) // and clicked
            {
                isDrawingExclusion = false;
                Mouse.Capture(null);

                //Finish polygon
                if (pos.Distance(startingPoint.Value) <= 10)
                {
                    currentLine.X2 = startingPoint.Value.X;
                    currentLine.Y2 = startingPoint.Value.Y;
                    currentLine = null;
                    startingPoint = null;

                    StopDrawingExclusionZone();

                    return;
                }
            }

            //Start a new line
            if (!isDrawingExclusion)
            {
                isDrawingExclusion = true;
                Mouse.Capture(window.exclusionShim);

                currentLine = new Line()
                {
                    X1 = pos.X,
                    X2 = pos.X,
                    Y1 = pos.Y,
                    Y2 = pos.Y,
                    Stroke = polyColor
                };

                window.videoGrid.Children.Add(currentLine);
                allLines.Add(currentLine);

                if (startingPoint == null)
                    startingPoint = pos;
            }

        }

        public void MouseMove()
        {
            if (!isDrawingExclusion)
                return;

            var pos = Mouse.GetPosition(window.videoGrid);

            currentLine.X2 = pos.X;
            currentLine.Y2 = pos.Y;

        }
        
        public void RemoveClicked()
        {
            window.Stop();

            MessageBox.Show("Click on an exclusion zone polygon while pressing 'SHIFT' to remove it.");
        }

        private void Poly_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                window.Stop();

                if (Changed != null)
                    Changed(this, null);

                var target = (Polygon)sender;

                window.videoGrid.Children.Remove(target);
                exclusionZones.Remove(target);

                if (exclusionZones.Count == 0)
                    window.btnRemoveExclusion.Visibility = Visibility.Hidden;

                SaveToSettings();
            }
        }

        public void LoadFromSettings()
        {
            if (window?.Pipeline?.VideoInfo == null)
                return;
                
            //Remove existing polys
            exclusionZones.ForEach(z => window.videoGrid.Children.Remove(z));
            exclusionZones.Clear();

            var zones = AppSettings.Default.ExclusionZones.Split('|');

            foreach (var zone in zones)
            {
                if (string.IsNullOrWhiteSpace(zone))
                    continue;

                var points = zone.Split(';');

                var poly = new Polygon()
                {
                    Stroke = polyColor,
                    Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0))
                };

                
                foreach (var point in points)
                {
                    var xy = point.Split(',');
                    var p = new System.Drawing.Point(int.Parse(xy[0]), int.Parse(xy[1]));
                    var canvasp = window.ToCanvasCoordinates(p);
                    canvasp.Offset(10,10);

                    poly.Points.Add(canvasp);
                }

                poly.MouseUp += Poly_MouseUp;
                exclusionZones.Add(poly);
                window.videoGrid.Children.Add(poly);

            }

            window.btnAddExclusion.Visibility = Visibility.Visible;
            window.btnRemoveExclusion.Visibility = zones.Length > 0 ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
