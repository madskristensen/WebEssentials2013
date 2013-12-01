using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssSmartTagProvider))]
    [Name("HexColorConverterSmartTagProvider")]
    internal class HexColorConverterSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(HexColorValue); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            HexColorValue hex = (HexColorValue)item;

            if (!item.IsValid)
                yield break;

            ColorModel model = ColorParser.TryParseColor(hex.Text, ColorParser.Options.None);
            if (model != null)
            {
                if (ColorConverterSmartTagAction.GetNamedColor(model.Color) != null)
                {
                    yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.Name);
                }

                if (model.Format == ColorFormat.RgbHex6)
                {
                    model.Format = ColorFormat.RgbHex3;
                    string hex3 = ColorFormatter.FormatColor(model, ColorFormat.RgbHex3);

                    if (hex3.Length == 4)
                    {
                        yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.RgbHex3);
                    }
                }

                yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.Rgb);
                yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.Hsl);
            }
        }
    }

    [Export(typeof(ICssSmartTagProvider))]
    [Name("RgbColorConverterSmartTagProvider")]
    internal class RgbColorConverterSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(FunctionColor); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            FunctionColor function = (FunctionColor)item;

            if (!function.IsValid)
                yield break;

            ColorModel model = ColorParser.TryParseColor(function.Text, ColorParser.Options.AllowAlpha);
            if (model != null)
            {
                // Don't convert RGBA and HSLA to HEX or named color
                if (model.Alpha == 1)
                {
                    if (ColorConverterSmartTagAction.GetNamedColor(model.Color) != null)
                    {
                        yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.Name);
                    }

                    yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.RgbHex3);
                }

                if (model.Format == ColorFormat.Rgb)
                {
                    yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.Hsl);
                }
                else if (model.Format == ColorFormat.Hsl)
                {
                    yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.Rgb);
                }
            }
        }
    }

    [Export(typeof(ICssSmartTagProvider))]
    [Name("NamedColorConverterSmartTagProvider")]
    internal class NamedColorConverterSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(TokenItem); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            TokenItem token = (TokenItem)item;
            if (!token.IsValid || token.TokenType != CssTokenType.Identifier || token.FindType<Declaration>() == null)
                yield break;

            var color = Color.FromName(token.Text);
            if (color.IsNamedColor)
            {
                ColorModel model = ColorParser.TryParseColor(token.Text, ColorParser.Options.AllowNames);
                if (model != null)
                {
                    yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.RgbHex3);
                    yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.Rgb);
                    yield return new ColorConverterSmartTagAction(itemTrackingSpan, model, ColorFormat.Hsl);
                }
            }
        }
    }
}
