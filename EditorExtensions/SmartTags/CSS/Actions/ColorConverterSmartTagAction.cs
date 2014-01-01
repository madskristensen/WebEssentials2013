using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class ColorConverterSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private ColorFormat _format;
        private string _displayText;
        private ColorModel _colorModel;
        private static string[] _colorNames = Enum.GetNames(typeof(System.Drawing.KnownColor));

        public ColorConverterSmartTagAction(ITrackingSpan span, ColorModel colorModel, ColorFormat format)
        {
            _span = span;

            if (format == ColorFormat.RgbHex6)
                format = ColorFormat.RgbHex3;

            _format = format;
            _colorModel = colorModel.Clone();
            _colorModel.Format = format;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/palette.png", UriKind.RelativeOrAbsolute));
            }

            _displayText = "Convert to " + GetColorString(_format, _colorModel);
        }

        private static string GetColorString(ColorFormat format, ColorModel model)
        {
            switch (format)
            {
                case ColorFormat.Name:
                    return GetNamedColor(model.Color);

                case ColorFormat.Hsl:
                    return FormatHslColor(model); //ColorFormatter.FormatColorAs(_colorModel, ColorFormat.Hsl);

                case ColorFormat.Rgb:
                case ColorFormat.RgbHex3:
                case ColorFormat.RgbHex6:
                    return ColorFormatter.FormatColor(model, format);

                default:
                    throw new InvalidEnumArgumentException("format", (int)format, typeof(ColorFormat));
            }
        }

        public override string DisplayText
        {
            get { return _displayText; }
        }

        public override void Invoke()
        {
            _span.TextBuffer.Replace(_span.GetSpan(_span.TextBuffer.CurrentSnapshot), GetColorString(_format, _colorModel));
        }

        #region Fixed Hsl conversion
        private static string FormatHslColor(ColorModel colorModel)
        {
            if (colorModel.Alpha < 1)
            {
                // HSL can't specify alpha
                return FormatHslaColor(colorModel);
            }
            else
            {
                HslColor hsl = colorModel.HSL;

                return string.Format(CultureInfo.InvariantCulture, "hsl({0}, {1}%, {2}%)",
                    Math.Round(hsl.Hue * 360, 1),
                    Math.Round(hsl.Saturation * 100, 1),
                    Math.Round(hsl.Lightness * 100, 1));
            }
        }

        private static string FormatHslaColor(ColorModel colorModel)
        {
            if (colorModel.Alpha >= 1)
            {
                // No need to specify alpha
                return FormatHslColor(colorModel);
            }
            else
            {
                HslColor hsl = colorModel.HSL;

                return string.Format(CultureInfo.InvariantCulture, "hsla({0}, {1}%, {2}%, {3:n2})",
                    Math.Round(hsl.Hue * 360, 1),
                    Math.Round(hsl.Saturation * 100, 1),
                    Math.Round(hsl.Lightness * 100, 1),
                    colorModel.Alpha);
            }
        }
        #endregion

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        public static string GetNamedColor(Color color)
        {
            foreach (string name in _colorNames)
            {
                System.Drawing.Color known = System.Drawing.Color.FromName(name);
                if (!known.IsSystemColor && color.R == known.R && color.G == known.G && color.B == known.B && color.A == known.A)
                {
                    return known.Name.ToLowerInvariant();
                }
            }

            return null;
        }
    }
}