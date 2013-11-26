using System.Windows.Media;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal static class ColorModelExtensions
    {
        private const float _factor = 0.025F;

        public static ColorModel Brighten(this ColorModel color)
        {
            if ((color.HslLightness + _factor) < 1)
            {
                color.HslLightness += _factor;
            }

            return color;
        }

        public static ColorModel Darken(this ColorModel color)
        {
            if ((color.HslLightness - _factor) > 0)
            {
                color.HslLightness -= _factor;
            }

            return color;
        }

        public static ColorModel Invert(this ColorModel color)
        {
            ColorModel model = new ColorModel()
            {
                Red = ~(byte)color.Red,
                Green = ~(byte)color.Green,
                Blue = ~(byte)color.Blue
            };

            return model;

        }

        public static SolidColorBrush ToBrush(this ColorModel color)
        {
            Color c = Color.FromRgb(
                (byte)color.Red,
                (byte)color.Green,
                (byte)color.Blue
                );

            SolidColorBrush brush = new SolidColorBrush(c);
            brush.Freeze();
            return brush;
        }
    }
}
