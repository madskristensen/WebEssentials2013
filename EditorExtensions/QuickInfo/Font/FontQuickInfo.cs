using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class FontQuickInfo : IQuickInfoSource
    {
        private ITextBuffer _buffer;
        private static InstalledFontCollection fonts = new InstalledFontCollection();
        private List<string> _allowed = new List<string>() { "FONT", "FONT-FAMILY" };

        public FontQuickInfo(ITextBuffer subjectBuffer)
        {
            _buffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (session == null || qiContent == null)
                return;

            // Map the trigger point down to our buffer.
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            var tree = CssEditorDocument.FromTextBuffer(_buffer);
            ParseItem item = tree.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null || !item.IsValid)
                return;

            Declaration dec = item.FindType<Declaration>();
            if (dec == null || !dec.IsValid || !_allowed.Contains(dec.PropertyName.Text.ToUpperInvariant()))
                return;

            string fontName = item.Text.Trim('\'', '"');

            if (fonts.Families.SingleOrDefault(f => f.Name.Equals(fontName, StringComparison.OrdinalIgnoreCase)) != null)
            {
                FontFamily font = new FontFamily(fontName);

                applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(item.Start, item.Length, SpanTrackingMode.EdgeNegative);
                qiContent.Add(CreateFontPreview(font, 10));
                qiContent.Add(CreateFontPreview(font, 11));
                qiContent.Add(CreateFontPreview(font, 12));
                qiContent.Add(CreateFontPreview(font, 14));
                qiContent.Add(CreateFontPreview(font, 25));
                qiContent.Add(CreateFontPreview(font, 40));
            }
        }

        private static UIElement CreateFontPreview(FontFamily font, double size)
        {
            return new TextBlock()
            {
                Text = font.Source + " (" + size + "px)",
                FontFamily = font,
                FontSize = size,
            };
        }

        private bool m_isDisposed;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }
}
