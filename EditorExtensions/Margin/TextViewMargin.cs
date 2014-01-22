using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Margin
{
    public class TextViewMargin : CompilingMarginBase
    {
        private IWpfTextViewHost _previewTextHost;
        readonly string _previewContentType;

        public TextViewMargin(string targetContentType, ITextDocument document)
            : base(WESettings.Instance.ForContentType<IMarginSettings>(document.TextBuffer.ContentType), document)
        {
            _previewContentType = targetContentType;
        }

        protected override FrameworkElement CreatePreviewControl(double width)
        {
            _previewTextHost = CreateTextViewHost(_previewContentType);
            _previewTextHost.TextView.VisualElement.MinWidth = width;
            _previewTextHost.TextView.VisualElement.HorizontalAlignment = HorizontalAlignment.Stretch;
            _previewTextHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
            _previewTextHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
            _previewTextHost.TextView.VisualElement.KeyDown += TextView_KeyUp;

            return _previewTextHost.HostControl;
        }

        private static IWpfTextViewHost CreateTextViewHost(string contentType)
        {
            var componentModel = ProjectHelpers.GetComponentModel();
            var service = componentModel.GetService<IContentTypeRegistryService>();
            var type = service.GetContentType(contentType);

            var textBufferFactory = componentModel.GetService<ITextBufferFactoryService>();
            var textViewFactory = componentModel.GetService<ITextEditorFactoryService>();
            var textRoles = contentType == "JavaScript" ? new[] { PredefinedTextViewRoles.Document } : new[] { PredefinedTextViewRoles.Document, PredefinedTextViewRoles.Interactive };
            ITextBuffer textBuffer = textBufferFactory.CreateTextBuffer(string.Empty, type);
            ITextViewRoleSet roles = textViewFactory.CreateTextViewRoleSet(textRoles);
            IWpfTextView textView = textViewFactory.CreateTextView(textBuffer, roles);
            IWpfTextViewHost host = textViewFactory.CreateTextViewHost(textView, false);

            return host;
        }


        void TextView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                Clipboard.SetText(_previewTextHost.TextView.TextBuffer.CurrentSnapshot.GetText(_previewTextHost.TextView.Selection.Start.Position.Position, _previewTextHost.TextView.Selection.End.Position.Position - _previewTextHost.TextView.Selection.Start.Position.Position));
            else if (e.Key == Key.PageDown)
                _previewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Down);
            else if (e.Key == Key.PageUp)
                _previewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Up);
            else if (e.Key == Key.Down)
                _previewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Down);
            else if (e.Key == Key.Up)
                _previewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Up);
            else if (e.Key == Key.Home)
                _previewTextHost.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(_previewTextHost.TextView.TextBuffer.CurrentSnapshot, 0, 0));
            else if (e.Key == Key.End)
                _previewTextHost.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(_previewTextHost.TextView.TextBuffer.CurrentSnapshot, _previewTextHost.TextView.TextBuffer.CurrentSnapshot.Length, 0));
        }

        protected void SetText(string text)
        {
            if (!Settings.ShowPreviewPane)
                return;

            // Prevents race conditions when the file is saved before the preview is open.
            if (_previewTextHost == null) return;

            if (string.IsNullOrEmpty(text))
            {
                _previewTextHost.HostControl.Opacity = 0.3;
                return;
            }
            int position = _previewTextHost.TextView.TextViewLines.FirstVisibleLine.Extent.Start.Position;
            _previewTextHost.TextView.TextBuffer.SetText(text);

            try
            {
                _previewTextHost.HostControl.Opacity = 1;
                _previewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, _previewTextHost.TextView.TextSnapshot.GetLineNumberFromPosition(position));
                _previewTextHost.TextView.ViewScroller.ScrollViewportHorizontallyByPixels(-9999);
            }
            catch
            {
                // Threading issues when called from TypeScript
            }
        }

        protected override void UpdateMargin(CompilerResult result)
        {
            if (result.IsSuccess)
                SetText(result.Result);
            else
                SetText("/*\r\n\r\nCompilation Error. \r\nSee error list for details\r\n"
                      + string.Join("\r\n", result.Errors.Select(e => e.Message))
                      + "\r\n\r\n*/");
        }
    }
}
