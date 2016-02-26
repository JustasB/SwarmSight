using SwarmSight.Helpers;
using System;

namespace SwarmSight.UserControls
{
    public partial class RegionOfInterest
    {
        public event EventHandler<EventArgs> RegionChanged;

        public RegionOfInterest()
        {
            InitializeComponent();

            Loaded += (sender, args) => SetupHandles();

            rightH.HandleMoved += (sender, args) => UpdateSelection();
            leftH.HandleMoved += (sender, args) => UpdateSelection();
            topH.HandleMoved += (sender, args) => UpdateSelection();
            bottomH.HandleMoved += (sender, args) => UpdateSelection();
            middleH.HandleMoved += (sender, args) => UpdateSelection();
        }

        public double LeftPercent
        {
            get { return leftH.Position/ActualWidth; }
        }

        public double TopPercent
        {
            get { return topH.Position/ActualHeight; }
        }

        public double RightPercent
        {
            get { return rightH.Position/ActualWidth; }
        }

        public double BottomPercent
        {
            get { return bottomH.Position/ActualHeight; }
        }

        private void SetupHandles()
        {
            //Position right & bottom handles
            rightH.SetLeftMargin(ActualWidth - rightH.ActualWidth);
            bottomH.SetTopMargin(ActualHeight - bottomH.ActualHeight);

            //Don't move past the borders of the container
            leftH.MinimumPosition = topH.MinimumPosition = 0;
            bottomH.MaximumPosition = (int) ActualHeight;
            rightH.MaximumPosition = (int) ActualWidth;

            UpdateSelection(dueToEvent: false);
        }

        private void UpdateSelection(bool dueToEvent = true)
        {
            //Set selection margins
            selection.SetLeftMargin(leftH.Position);
            selection.SetTopMargin(topH.Position);
            middleH.SetLeftMargin(leftH.Position);
            middleH.SetTopMargin(topH.Position);

            //Selection dimensions
            selection.Width = middleH.Width = rightH.Position - leftH.Position;
            selection.Height = middleH.Height = bottomH.Position - topH.Position;

            //Crop right & left handles by top & bottom ones
            rightH.SetTopMargin(topH.Position);
            leftH.SetTopMargin(topH.Position);
            rightH.Height = leftH.Height = bottomH.Position - topH.Position;

            //Crop by right & left
            bottomH.SetLeftMargin(leftH.Position);
            topH.SetLeftMargin(leftH.Position);
            bottomH.Width = topH.Width = rightH.Position - leftH.Position;

            //Bind movements to not exceed complement handles
            rightH.MinimumPosition = leftH.Position;
            leftH.MaximumPosition = rightH.Position;
            topH.MaximumPosition = bottomH.Position;
            bottomH.MinimumPosition = topH.Position;

            if (RegionChanged != null && dueToEvent)
                RegionChanged(this, null);
        }
    }
}