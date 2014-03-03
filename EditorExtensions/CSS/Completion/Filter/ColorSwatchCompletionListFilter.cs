using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssCompletionListFilter))]
    [Name("Color Swatch Filter")]
    internal class ColorSwatchCompletionListFilter : ICssCompletionListFilter
    {
        public void FilterCompletionList(IList<CssCompletionEntry> completions, CssCompletionContext context)
        {
            if (context.ContextType != CssCompletionContextType.PropertyValue)
                return;

            // Check if the property name has the word color in it. Schema only list colors for these properties.
            Declaration dec = context.ContextItem.FindType<Declaration>();
            if (dec == null || dec.PropertyName == null || !dec.PropertyNameText.Contains("color"))
                return;

            foreach (CssCompletionEntry entry in completions)
            {
                var color = System.Drawing.Color.FromName(entry.DisplayText);

                if (color.IsKnownColor)
                {
                    entry.IconSource = GetColorSwatch(color);
                }
                else if (entry.DisplayText.Contains("grey") || entry.DisplayText.Contains("3D"))
                {
                    // Hides old IE entries from poluting Intellisense.
                    entry.FilterType = CompletionEntryFilterTypes.NeverVisible;
                }
            }
        }

        public static ImageSource GetColorSwatch(System.Drawing.Color value)
        {
            Color color = Color.FromArgb(value.A, value.R, value.G, value.B);

            GeometryGroup group = new GeometryGroup();
            group.Children.Add(new RectangleGeometry(new Rect(new Size(50, 50))));

            GeometryDrawing drawing = new GeometryDrawing()
            {
                Geometry = group,
                Brush = new SolidColorBrush(color),
                Pen = new Pen(Brushes.Transparent, 10),
            };

            DrawingImage image = new DrawingImage(drawing);
            image.Freeze();

            return image;
        }
    }
}