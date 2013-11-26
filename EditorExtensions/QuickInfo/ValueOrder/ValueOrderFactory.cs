using System.Collections.Generic;
using System.Globalization;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal static class ValueOrderFactory
    {
        public delegate void AddSignatures(ISignatureHelpSession session, IList<ISignature> signatures, Declaration dec, ITrackingSpan span);

        public static AddSignatures GetMethod(Declaration dec)
        {
            switch (dec.PropertyName.Text.ToLowerInvariant())
            {
                case "margin":
                case "padding":
                case "border-width":
                case "outline-width":
                    return Margins;

                case "border-radius":
                    return Corners;

                case "border":
                    return Borders;

                case "font":
                    return Fonts;

                case "columns":
                    return Columns;
            }

            return null;
        }

        private static void Margins(ISignatureHelpSession session, IList<ISignature> signatures, Declaration dec, ITrackingSpan span)
        {
            string value1 = "3px";
            string value2 = "4px";
            string value3 = "5px";
            string value4 = "6px";

            if (dec.Values.Count > 0)
            {
                value1 = dec.Values[0].Text;
                value2 = dec.Values.Count > 1 ? dec.Values[1].Text : value2;
                value3 = dec.Values.Count > 2 ? dec.Values[2].Text : value3;
                value4 = dec.Values.Count > 3 ? dec.Values[3].Text : value4;
            }

            ValueOrderSignature signature1 = new ValueOrderSignature(
                string.Format(CultureInfo.CurrentCulture, "div {{ {0}: {1} {2} {3} {4}; }} ", dec.PropertyName.Text, value1, value2, value3, value4),
                string.Format(CultureInfo.CurrentCulture, "[top={0}] [right={1}] [bottom={2}] [left={3}]", value1, value2, value3, value4),
                span, session);

            ValueOrderSignature signature2 = new ValueOrderSignature(
                string.Format(CultureInfo.CurrentCulture, "div {{ {0}: {1} {2} {3}; }} ", dec.PropertyName.Text, value1, value2, value3),
                string.Format(CultureInfo.CurrentCulture, "[top={0}] [right and left={1}] [bottom={2}]", value1, value2, value3),
                span, session);

            ValueOrderSignature signature3 = new ValueOrderSignature(
                string.Format(CultureInfo.CurrentCulture, "div {{ {0}: {1} {2}; }} ", dec.PropertyName.Text, value1, value2),
                string.Format(CultureInfo.CurrentCulture, "[top and bottom={0}] [right and left={1}]", value1, value2),
                span, session);

            ValueOrderSignature signature4 = new ValueOrderSignature(
                string.Format(CultureInfo.CurrentCulture, "div {{ {0}: {1}; }} ", dec.PropertyName.Text, value1),
                string.Format(CultureInfo.CurrentCulture, "[top and right and bottom and left={0}]", value1),
                span, session);

            signatures.Add(signature1);
            signatures.Add(signature2);
            signatures.Add(signature3);
            signatures.Add(signature4);
        }

        private static void Corners(ISignatureHelpSession session, IList<ISignature> signatures, Declaration dec, ITrackingSpan span)
        {
            string value1 = "3px";
            string value2 = "4px";
            string value3 = "5px";
            string value4 = "6px";

            if (dec.Values.Count > 0)
            {
                value1 = dec.Values[0].Text;
                value2 = dec.Values.Count > 1 ? dec.Values[1].Text : value2;
                value3 = dec.Values.Count > 2 ? dec.Values[2].Text : value3;
                value4 = dec.Values.Count > 3 ? dec.Values[3].Text : value4;

            }
            ValueOrderSignature signature1 = new ValueOrderSignature(
                string.Format(CultureInfo.CurrentCulture, "div {{ {0}: {1} {2} {3} {4}; }} ", dec.PropertyName.Text, value1, value2, value3, value4),
                string.Format(CultureInfo.CurrentCulture, "[top-left={0}] [top-right={1}] [bottom-right={2}] [bottom-left={3}]", value1, value2, value3, value4),
                span, session);

            ValueOrderSignature signature2 = new ValueOrderSignature(
                string.Format(CultureInfo.CurrentCulture, "div {{ {0}: {1} {2} {3}; }} ", dec.PropertyName.Text, value1, value2, value3),
                string.Format(CultureInfo.CurrentCulture, "[top-left={0}] [top-right and bottom-left={1}] [bottom-right={2}]", value1, value2, value3),
                span, session);

            ValueOrderSignature signature3 = new ValueOrderSignature(
                string.Format(CultureInfo.CurrentCulture, "div {{ {0}: {1} {2}; }} ", dec.PropertyName.Text, value1, value2),
                string.Format(CultureInfo.CurrentCulture, "[top-left and bottom-right={0}] [top-right and bottom-left={1}]", value1, value2),
                span, session);

            ValueOrderSignature signature4 = new ValueOrderSignature(
                string.Format(CultureInfo.CurrentCulture, "div {{ {0}: {1}; }} ", dec.PropertyName.Text, value1),
                string.Format(CultureInfo.CurrentCulture, "[top-left and top-right and bottom-right and bottom-left={0}]", value1),
                span, session);

            signatures.Add(signature1);
            signatures.Add(signature2);
            signatures.Add(signature3);
            signatures.Add(signature4);
        }

        private static void Borders(ISignatureHelpSession session, IList<ISignature> signatures, Declaration dec, ITrackingSpan span)
        {
            ValueOrderSignature signature1 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": 1px solid red; } ",
                "[border-width=1px] [border-style=solid] [border-color=red]",
                span, session);

            ValueOrderSignature signature2 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": 1px solid; } ",
                "[border-width=1px] [border-style=solid] [border-color='color']",
                span, session);

            ValueOrderSignature signature3 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": solid; } ",
                "[border-width=3px] [border-style=solid] [border-color='color']",
                span, session);

            signatures.Add(signature1);
            signatures.Add(signature2);
            signatures.Add(signature3);
        }

        private static void Fonts(ISignatureHelpSession session, IList<ISignature> signatures, Declaration dec, ITrackingSpan span)
        {
            ValueOrderSignature signature1 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": italic small-caps bold 13px/150% Arial; } ",
                "[font-style=italic] [font-variant=small-caps] [font-weight=bold] [font-size=12px]/[line-height=150%] [font-family=Arial]",
                span, session);

            ValueOrderSignature signature2 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": small-caps bold 13px/150% Arial; } ",
                "[font-variant=small-caps] [font-weight=bold] [font-size=12px]/[line-height=150%] [font-family=Arial]",
                span, session);

            ValueOrderSignature signature3 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": bold 13px/150% Arial; } ",
                "[font-weight=bold] [font-size=12px]/[line-height=150%] [font-family=Arial]",
                span, session);

            ValueOrderSignature signature4 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": 13px/150% Arial; } ",
                "[font-size=12px]/[line-height=150%] [font-family=Arial]",
                span, session);

            ValueOrderSignature signature5 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": 13px Arial; } ",
                "[font-size=13px] [font-family=Arial]",
                span, session);

            signatures.Add(signature5);
            signatures.Add(signature4);
            signatures.Add(signature3);
            signatures.Add(signature2);
            signatures.Add(signature1);
        }

        private static void Columns(ISignatureHelpSession session, IList<ISignature> signatures, Declaration dec, ITrackingSpan span)
        {
            ValueOrderSignature signature1 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": 12em; } ",
                "[column-width=12em] [column-count=auto]",
                span, session);

            ValueOrderSignature signature2 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": auto 12em; } ",
                "[column-width=12em] [column-count=auto]",
                span, session);

            ValueOrderSignature signature3 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": auto; } ",
                "[column-width=auto] [column-count=auto]",
                span, session);

            ValueOrderSignature signature4 = new ValueOrderSignature(
                "div { " + dec.PropertyName.Text + ": 2; } ",
                "[column-width=auto] [column-count=2]",
                span, session);

            signatures.Add(signature1);
            signatures.Add(signature2);
            signatures.Add(signature3);
            signatures.Add(signature4);
        }
    }
}
