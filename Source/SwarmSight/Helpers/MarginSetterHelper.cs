using System.Windows;

namespace SwarmSight.Helpers
{
    public static class MarginSetterHelper
    {
        public static void SetTopMargin(this FrameworkElement element, double newValue)
        {
            var newMargin = element.Margin;

            newMargin.Top = newValue;

            element.Margin = newMargin;
        }

        public static void SetBottomMargin(this FrameworkElement element, double newValue)
        {
            var newMargin = element.Margin;

            newMargin.Bottom = newValue;

            element.Margin = newMargin;
        }

        public static void SetLeftMargin(this FrameworkElement element, double newValue)
        {
            var newMargin = element.Margin;

            newMargin.Left = newValue;

            element.Margin = newMargin;
        }

        public static void SetRightMargin(this FrameworkElement element, double newValue)
        {
            var newMargin = element.Margin;

            newMargin.Right = newValue;

            element.Margin = newMargin;
        }
    }
}