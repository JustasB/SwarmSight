using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SwarmVision
{
    public partial class TemplateView : UserControl
    {
        public Dictionary<string, string> TemplatePaths = new Dictionary<string, string>()
        {
            { "Center of Mouth", "Assets/HeadFront.jpg" },
            { "Back of the Head", "Assets/HeadBack.jpg" },
            { "Proboscis Tip", "Assets/Proboscis.jpg" },
            { "Left Antena Tip", "Assets/LeftTip.jpg" },
            { "Left Antena Joint", "Assets/LeftJoint.jpg" },
            { "Right Antena Tip", "Assets/RightTip.jpg" },
            { "Right Antena Joint", "Assets/RightJoint.jpg" },
        };

        public static TemplateView Current;

        public int BurstSize = 5;
        public int CurrentPartIndex = 0;
        public int CurrentBurstPosition = 0;

        public string CurrentPartName
        {
            get { return TemplatePaths.ElementAt(CurrentPartIndex).Key; }
        }

        public string CurrentPartPath
        {
            get { return TemplatePaths.ElementAt(CurrentPartIndex).Value; }
        }

        public bool AtBatchStart
        {
            get { return CurrentPartIndex == 0 && AtBurstStart; }
        }
        public bool AtBurstStart
        {
            get { return CurrentBurstPosition == 0; }
        }

        public void AdvancePart()
        {
            CurrentPartIndex = (CurrentPartIndex + 1)%TemplatePaths.Count;
        }

        public void AdvanceBurst()
        {
            CurrentBurstPosition = (CurrentBurstPosition + 1)%BurstSize;

            if(AtBurstStart)
                AdvancePart();
        }

        public void Restart()
        {
            CurrentPartIndex = 0;
            CurrentBurstPosition = 0;
        }

        public void ShowNextAction()
        {
            AdvanceBurst();
            UpdateView();
        }

        public TemplateView()
        {
            Current = this;

            InitializeComponent();

            UpdateView();
        }

        public void UpdateView()
        {
            lblTask.Content = "Task: Find the " + CurrentPartName;
            partImage.Source = new BitmapImage(new Uri(CurrentPartPath, UriKind.Relative));
        }
    }
}
