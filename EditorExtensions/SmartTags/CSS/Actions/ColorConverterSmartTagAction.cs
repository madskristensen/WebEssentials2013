using System;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class ColorConverterSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private ParseItem _item;
        private ColorFormat _format;
        private string _displayText;
        private ColorModel _colorModel;
        private static string[] _colorNames = Enum.GetNames(typeof(System.Drawing.KnownColor));

        public ColorConverterSmartTagAction(ITrackingSpan span, ParseItem item, ColorModel colorModel, ColorFormat format)
        {
            _span = span;
            _item = item;
            _format = format;
            _colorModel = colorModel;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/palette.png", UriKind.RelativeOrAbsolute));
            }

            SetDisplayText();
        }

        public override string DisplayText
        {
            get { return _displayText; }
        }

        public override void Invoke()
        {
            var value = _item.Text;

            switch (_format)
            {
                case ColorFormat.Name:
                    value = GetNamedColor(_colorModel.Color);
                    break;

                case ColorFormat.Hsl:
                    value = FormatHslColor(_colorModel); //ColorFormatter.FormatColorAs(_colorModel, ColorFormat.Hsl);
                    break;

                case ColorFormat.Rgb:
                    _colorModel.Format = ColorFormat.Rgb;
                    value = ColorFormatter.FormatColor(_colorModel, ColorFormat.Rgb);
                    break;

                case ColorFormat.RgbHex3:
                case ColorFormat.RgbHex6:
                    _colorModel.Format = ColorFormat.RgbHex3;
                    value = ColorFormatter.FormatColor(_colorModel, ColorFormat.RgbHex3);
                    break;
            }

            _span.TextBuffer.Replace(_span.GetSpan(_span.TextBuffer.CurrentSnapshot), value);
        }

        private void SetDisplayText()
        {
            string format = "Convert to {0}";
            switch (_format)
            {
                case ColorFormat.Rgb:
                    _displayText = string.Format(CultureInfo.InvariantCulture, format, ColorFormatter.FormatColor(_colorModel, ColorFormat.Rgb));
                    break;

                case ColorFormat.Hsl:
                    _displayText = string.Format(CultureInfo.InvariantCulture, format, FormatHslColor(_colorModel)); //string.Format(CultureInfo.InvariantCulture, format, ColorFormatter.FormatColorAs(_colorModel, ColorFormat.Hsl));
                    break;

                case ColorFormat.Name:
                    _displayText = string.Format(CultureInfo.InvariantCulture, format, GetNamedColor(_colorModel.Color));
                    break;

                case ColorFormat.RgbHex3:
                case ColorFormat.RgbHex6:
                    _displayText = string.Format(CultureInfo.InvariantCulture, format, ColorFormatter.FormatColor(_colorModel, ColorFormat.RgbHex3));
                    break;
            }
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
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
