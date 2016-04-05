using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SwarmSight
{
    public partial class TemplateView : UserControl
    {
        public Dictionary<string, string> TemplatePaths = new Dictionary<string, string>()
        {
            { "Left Mandible Base", "Assets/LeftMandibleBase.jpg" },
            { "Left Mandible Tip", "Assets/LeftMandibleTip.jpg" },

            { "Right Mandible Base", "Assets/RightMandibleBase.jpg" },
            { "Right Mandible Tip", "Assets/RightMandibleTip.jpg" },

            { "Dorsal Edge of the Right Eye", "Assets/RightEyeEdge.jpg" },
            { "Dorsal Edge of the Left Eye", "Assets/LeftEyeEdge.jpg" },

            { "Left Flagellum Tip", "Assets/LeftTip.jpg" },
            { "Left Flagellum Base", "Assets/LeftJoint.jpg" },
            { "Left Scape", "Assets/LeftBase.jpg" },

            { "Right Flagellum Tip", "Assets/RightTip.jpg" },
            { "Right Flagellum Base", "Assets/RightJoint.jpg" },
            { "Right Scape", "Assets/RightBase.jpg" },

            { "Tip of Proboscis", "Assets/Proboscis.jpg" },
        };

        public double Angle {  get { return rotateTransform.Angle; } set { rotateTransform.Angle = value; } }
        public static TemplateView Current;

        public int CurrentPartIndex = 0;

        public string CurrentPartName
        {
            get { return TemplatePaths.ElementAt(CurrentPartIndex).Key; }
        }

        public string CurrentPartPath
        {
            get { return TemplatePaths.ElementAt(CurrentPartIndex).Value; }
        }

        public TemplateView()
        {
            Current = this;

            InitializeComponent();

            UpdateView();
        }

        public void UpdateView()
        {
            lblTask.Text = "Task: Find the " + CurrentPartName;
            partImage.Source = new BitmapImage(new Uri(CurrentPartPath, UriKind.Relative));
        }
    }
}
