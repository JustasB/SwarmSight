namespace SwarmSight.Filters
{
    public static class AccordExtensions
    {
        public static double[] ToAccordInput(this Frame target)
        {
            var result = new double[target.Height * target.Width];

            for (var row = 0; row < target.Height; row++)
                for (var col = 0; col < target.Width; col++)
                {
                    var pixValue = target.PixelBytes[row * target.Stride + 3 * col];
                    var normalized = pixValue / 255.0;

                    result[row * target.Width + col] = normalized;
                }

            return result;
        }
    }
}
