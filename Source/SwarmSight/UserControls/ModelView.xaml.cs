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
using SwarmSight.HeadPartsTracking;

namespace SwarmSight.UserControls
{
    /// <summary>
    /// Interaction logic for ModelView.xaml
    /// </summary>
    public partial class ModelView : UserControl
    {
        public ModelView()
        {
            InitializeComponent();
        }

        internal void Show(EfficientTipAndPERdetector.TipAndPERResult processorResult)
        {
            if (processorResult?.Left?.Tip != null)
            {
                leftLine.X1 = processorResult.Left.Tip.StandardPoint.X;
                leftLine.Y1 = processorResult.Left.Tip.StandardPoint.Y;

                leftLine.X2 = processorResult.Left.Base.StandardPoint.X;
                leftLine.Y2 = processorResult.Left.Base.StandardPoint.Y;

                leftAngle.Content = processorResult.Left.Angle.ToString("N0") + "°";
            }

            if (processorResult?.Right?.Tip != null)
            {
                rightLine.X1 = processorResult.Right.Tip.StandardPoint.X;
                rightLine.Y1 = processorResult.Right.Tip.StandardPoint.Y;

                rightLine.X2 = processorResult.Right.Base.StandardPoint.X;
                rightLine.Y2 = processorResult.Right.Base.StandardPoint.Y;

                rightAngle.Content = (-processorResult.Right.Angle).ToString("N0") + "°";
            }

            if(processorResult?.Proboscis != null)
            {
                prob.X2 = processorResult.Proboscis.Tip.StandardPoint.X;
                prob.Y2 = processorResult.Proboscis.Tip.StandardPoint.Y;

                probLength.Content = 100 - 4 * processorResult.Proboscis.Tip.StandardPoint.Y;
            }
        }
    }
}
