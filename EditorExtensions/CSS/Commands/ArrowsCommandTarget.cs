using System;
using System.Globalization;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Editor = Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Css
{
    class ArrowsCommandTarget : IOleCommandTarget
    {
        private ITextView _textView;
        private IOleCommandTarget _nextCommandTarget;

        public ArrowsCommandTarget(IVsTextView adapter, ITextView textView)
        {
            this._textView = textView;
            ErrorHandler.ThrowOnFailure(adapter.AddCommandFilter(this, out _nextCommandTarget));
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
            {
                switch (nCmdID)
                {
                    case 2400:
                        if (Move(Direction.Down))
                            return VSConstants.S_OK;
                        break;

                    case 2401:
                        if (Move(Direction.Up))
                            return VSConstants.S_OK;
                        break;
                }
            }

            return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private enum Direction
        {
            Up,
            Down
        }

        private bool Move(Direction direction)
        {
            if (Editor.WebEditor.Host == null)
                return false;

            var point = _textView.BufferGraph.MapDownToInsertionPoint(_textView.Caret.Position.BufferPosition, PointTrackingMode.Positive, ts => ts.ContentType.IsOfType(Editor.CssContentTypeDefinition.CssContentType));
            if (point == null)
                return false;

            var tree = CssEditorDocument.FromTextBuffer(point.Value.Snapshot.TextBuffer);
            ParseItem item = tree.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null)
                return false;

            NumericalValue unit = item.FindType<NumericalValue>();
            if (unit != null)
            {
                return HandleUnits(direction, unit, point.Value.Snapshot);
            }

            HexColorValue hex = item.FindType<HexColorValue>();
            if (hex != null)
            {
                return HandleHex(direction, hex, point.Value.Snapshot);
            }

            return false;
        }

        private static bool HandleUnits(Direction direction, NumericalValue item, ITextSnapshot snapshot)
        {
            float value;
            if (!float.TryParse(item.Number.Text, out value))
                return false;

            if (!AreWithinLimits(direction, value, item))
                return true;

            var span = new SnapshotSpan(snapshot, item.Number.Start, item.Number.Length);
            float delta = GetDelta(item.Number.Text);
            string format = item.Number.Text.Contains(".") ? "#.#0" : string.Empty;
            if (NumberDecimalPlaces(item.Number.Text) == 1)
                format = "F1";

            if (direction == Direction.Down)
                UpdateSpan(span, (value - delta).ToString(format, CultureInfo.InvariantCulture), "Decrease value");
            else
                UpdateSpan(span, (value + delta).ToString(format, CultureInfo.InvariantCulture), "Increase value");

            return true;
        }

        private static int NumberDecimalPlaces(string value)
        {
            int s = value.IndexOf(".", StringComparison.CurrentCulture) + 1; // the first numbers plus decimal point
            if (s == 0)                     // No decimal point
                return 0;

            return value.Length - s;     //total length minus beginning numbers and decimal = number of decimal points
        }

        private static bool AreWithinLimits(Direction direction, float number, NumericalValue item)
        {
            UnitType type = GetUnitType(item);
            switch (type)
            {
                case UnitType.Angle:
                    return (direction == Direction.Up) ? number < 360 : number > -360;

                //case UnitType.Percentage:
                //    return (direction == Direction.Up) ? number < 100 : number > 0;

                // Larger than zero
                case UnitType.Grid:
                case UnitType.Frequency:
                case UnitType.Resolution:
                case UnitType.Time:
                    return (direction == Direction.Down) ? number > 0 : true;

                case UnitType.Percentage:
                case UnitType.Length:
                case UnitType.Viewport:
                    return true;
            }

            FunctionColor func = item.FindType<FunctionColor>();
            if (func != null)
            {
                if (func.FunctionName.Text.StartsWith("rgb", StringComparison.Ordinal))
                {
                    if (direction == Direction.Up)
                        return number < 255;
                    else
                        return number > 0;
                }

                if (func.FunctionName.Text.StartsWith("hsl", StringComparison.Ordinal))
                {
                    if (direction == Direction.Up)
                        return number < 360;
                    else
                        return number > 0;
                }
            }

            return true;
        }

        private static UnitType GetUnitType(ParseItem valueItem)
        {
            UnitValue unitValue = valueItem as UnitValue;

            return (unitValue != null) ? unitValue.UnitType : UnitType.Unknown;
        }

        private static bool HandleHex(Direction direction, HexColorValue item, ITextSnapshot snapshot)
        {
            var model = ColorParser.TryParseColor(item.Text, ColorParser.Options.None);

            if (model != null)
            {
                var span = new SnapshotSpan(snapshot, item.Start, item.Length);

                if (direction == Direction.Down && model.HslLightness > 0)
                {
                    model.Format = Editor.ColorFormat.RgbHex3;
                    UpdateSpan(span, Editor.ColorFormatter.FormatColor(model.Darken(), model.Format), "Darken color");
                }
                else if (direction == Direction.Up && model.HslLightness < 1)
                {
                    model.Format = Editor.ColorFormat.RgbHex3;
                    UpdateSpan(span, Editor.ColorFormatter.FormatColor(model.Brighten(), model.Format), "Brighten color");
                }

                return true;
            }

            return false;
        }

        private static float GetDelta(string value)
        {
            int decimals = NumberDecimalPlaces(value);
            if (decimals > 0)
            {
                if (decimals > 1)
                    return 0.01F;
                else
                    return 0.1F;
            }

            return 1F;
        }

        private static void UpdateSpan(SnapshotSpan span, string result, string undoTitle)
        {
            if (result.Length > 1)
                result = result.TrimStart('0');

            using (WebEssentialsPackage.UndoContext(undoTitle))
                span.Snapshot.TextBuffer.Replace(span, result);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    switch (prgCmds[i].cmdID)
                    {
                        case 2401: // Up
                        case 2400: // Down
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                    }
                }
            }

            return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}