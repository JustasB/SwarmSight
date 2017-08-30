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
using SwarmSight.Filters;

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
                leftLine.X1 = processorResult.Left.Tip.ModelPoint.X;
                leftLine.Y1 = processorResult.Left.Tip.ModelPoint.Y;

                leftLine.X2 = processorResult.Left.Base.ModelPoint.X;
                leftLine.Y2 = processorResult.Left.Base.ModelPoint.Y;

                leftAngle.Content = processorResult.Left.Angle.ToString("N0") + "°";
            }

            if (processorResult?.Right?.Tip != null)
            {
                rightLine.X1 = processorResult.Right.Tip.ModelPoint.X;
                rightLine.Y1 = processorResult.Right.Tip.ModelPoint.Y;

                rightLine.X2 = processorResult.Right.Base.ModelPoint.X;
                rightLine.Y2 = processorResult.Right.Base.ModelPoint.Y;

                rightAngle.Content = (-processorResult.Right.Angle).ToString("N0") + "°";
            }

            if(processorResult?.Proboscis != null)
            {
                prob.X2 = processorResult.Proboscis.Tip.ModelPoint.X;
                prob.Y2 = processorResult.Proboscis.Tip.ModelPoint.Y;

                probLength.Content = processorResult.Proboscis.Length.Rounded();
            }
        }
    }
}
