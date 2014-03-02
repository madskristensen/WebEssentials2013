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
                    if (color.IsSystemColor)
                        entry.FilterType = CompletionEntryFilterTypes.NeverVisible;
                    else
                        entry.IconSource = GetColorSwatch(entry.DisplayText);
                }
            }
        }

        private ImageSource GetColorSwatch(string value)
        {
            Color color = (Color)ColorConverter.ConvertFromString(value);

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