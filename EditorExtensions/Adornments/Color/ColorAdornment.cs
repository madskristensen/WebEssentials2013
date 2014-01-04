using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal sealed class ColorAdornment : Border
    {
        private static SolidColorBrush _borderColor = OptionHelpers.BackgroundColor.Invert().ToBrush();

        internal ColorAdornment(ColorTag colorTag, ITextView view)
        {
            this.Padding = new Thickness(0);
            this.BorderThickness = new Thickness(1);
            this.Margin = new Thickness(0, 0, 2, 3);
            this.Width = OptionHelpers.FontSize;
            this.Height = this.Width;
            this.Cursor = System.Windows.Input.Cursors.Arrow;
            this.MouseUp += delegate { ColorAdornmentMouseUp(view); };

            Update(colorTag);
        }

        private static void ColorAdornmentMouseUp(ITextView view)
        {
            try
            {
                CssCompletionController.FromView(view).OnShowMemberList(filterList: true);
            }
            catch
            { }
        }

        internal void Update(ColorTag colorTag)
        {
            this.Background = new SolidColorBrush(colorTag.Color);

            if (!HasContrastToBackground(colorTag.Color))
            {
                this.BorderThickness = new Thickness(1);
                this.BorderBrush = _borderColor;
            }
            else
            {
                this.BorderThickness = new Thickness(0);
                this.BorderBrush = this.Background;
            }
        }

        private static bool HasContrastToBackground(Color color)
        {
            // The color is very transparent (alpha channel)
            if (color.A < 13)
            {
                return false;
            }

            var b = OptionHelpers.BackgroundColor;
            double bBrightness = b.Red * 299 + b.Green * 587 + b.Blue * 114;
            double cBrightness = color.R * 299 + color.G * 587 + color.B * 114;
            double distance = Math.Abs(cBrightness - bBrightness) / 1000;

            return distance > 20;
        }
    }
}
