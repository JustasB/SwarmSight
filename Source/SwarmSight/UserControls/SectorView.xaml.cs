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
using Frame = SwarmSight.Filters.Frame;
using System.Drawing;
using SwarmSight.Filters;
using Color = System.Drawing.Color;

namespace SwarmSight.UserControls
{
    /// <summary>
    /// Interaction logic for SectorView.xaml
    /// </summary>
    public partial class SectorView : UserControl
    {
        public SectorView()
        {
            InitializeComponent();
        }

        WriteableBitmap canvasBuffer;
        Frame clearFrame;
        internal void Show(EfficientTipAndPERdetector.TipAndPERResult processorResult)
        {
            var width = (int)image.Width;
            var height = (int)image.Height;

            //Create WriteableBitmap the first time
            if (canvasBuffer == null)
            {
                clearFrame = new Frame(width, height);
                
                canvasBuffer = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
                image.Source = canvasBuffer;
            }

            using (var gfx = Graphics.FromImage(clearFrame.Bitmap))
            {
                gfx.Clear(Color.White);
            }

            if (processorResult?.Left?.SectorCounts != null)
            {
                clearFrame.MarkSectors(
                    sectors: processorResult.Left.SectorCounts,
                    headCtrX: width / 2,
                    headCtrY: height / 2,
                    headHeight: height / 2,
                    headAngle: 0,
                    color: Color.Black,
                    isRight: false
                );

                leftDomSec.Content = processorResult.Left.DominantSector;
                leftSecMode.Content = processorResult.Left.TopAngle + "°";
            }
            else
            {
                leftDomSec.Content = "-";
                leftSecMode.Content = "-";
            }



            if (processorResult?.Right?.SectorCounts != null)
            {
                clearFrame.MarkSectors(
                    sectors: processorResult.Right.SectorCounts,
                    headCtrX: width / 2,
                    headCtrY: height / 2,
                    headHeight: height / 2,
                    headAngle: 0,
                    color: Color.Black,
                    isRight: true
                );

                rightDomSec.Content = processorResult.Right.DominantSector;
                rightSecMode.Content = processorResult.Right.TopAngle + "°";
            }
            else
            {
                rightDomSec.Content = "-";
                rightSecMode.Content = "-";
            }

            clearFrame.CopyToWriteableBitmap(canvasBuffer);
        }
    }
}
