using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Margin
{
    public class TextViewMargin : CompilingMarginBase
    {
        private readonly string _previewContentType;

        protected IWpfTextViewHost PreviewTextHost { get; set; }
        protected IWpfTextView SourceTextView { get; set; }

        public TextViewMargin(string targetContentType, ITextDocument document, IWpfTextView sourceView)
            : base(WESettings.Instance.ForContentType<IMarginSettings>(document.TextBuffer.ContentType), document)
        {
            SourceTextView = sourceView;
            _previewContentType = targetContentType;
        }

        protected override FrameworkElement CreatePreviewControl()
        {
            if (_previewContentType == null)
                return null;

            PreviewTextHost = CreateTextViewHost(_previewContentType);
            PreviewTextHost.TextView.VisualElement.HorizontalAlignment = HorizontalAlignment.Stretch;
            PreviewTextHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
            PreviewTextHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
            PreviewTextHost.TextView.VisualElement.KeyDown += TextView_KeyUp;
            PreviewTextHost.TextView.VisualElement.PreviewMouseRightButtonUp += new MouseButtonEventHandler(OnMenuClicked);

            var menu = new ContextMenu();

            SetupCommands(menu);
            menu.Items.Add(new MenuItem() { Command = ApplicationCommands.Copy });
            menu.Items.Add(new MenuItem() { Command = ApplicationCommands.SelectAll });
            AddSpecialItems(menu);

            PreviewTextHost.TextView.VisualElement.ContextMenu = menu;

            return PreviewTextHost.HostControl;
        }

        private static IWpfTextViewHost CreateTextViewHost(string contentType)
        {
            if (contentType == null)
                return null;

            var componentModel = ProjectHelpers.GetComponentModel();
            var service = componentModel.GetService<IContentTypeRegistryService>();
            var type = service.GetContentType(contentType);

            var textBufferFactory = componentModel.GetService<ITextBufferFactoryService>();
            var textViewFactory = componentModel.GetService<ITextEditorFactoryService>();
            var textRoles = contentType == "JavaScript" ? PredefinedTextViewRoles.Document : PredefinedTextViewRoles.Interactive;
            ITextBuffer textBuffer = textBufferFactory.CreateTextBuffer(string.Empty, type);
            ITextViewRoleSet roles = textViewFactory.CreateTextViewRoleSet(textRoles);
            IWpfTextView textView = textViewFactory.CreateTextView(textBuffer, roles);
            IWpfTextViewHost host = textViewFactory.CreateTextViewHost(textView, false);

            return host;
        }

        void TextView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.PageDown)
                PreviewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Down);
            else if (e.Key == Key.PageUp)
                PreviewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Up);
            else if (e.Key == Key.Down)
                PreviewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Down);
            else if (e.Key == Key.Up)
                PreviewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Up);
            else if (e.Key == Key.Home)
                PreviewTextHost.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(PreviewTextHost.TextView.TextBuffer.CurrentSnapshot, 0, 0));
            else if (e.Key == Key.End)
                PreviewTextHost.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(PreviewTextHost.TextView.TextBuffer.CurrentSnapshot, PreviewTextHost.TextView.TextBuffer.CurrentSnapshot.Length, 0));
        }

        protected void SetText(string text)
        {
            // Prevents race conditions when the file is saved before the preview is open.
            if (!Settings.ShowPreviewPane || PreviewTextHost == null)
                return;

            try
            {

                if (string.IsNullOrEmpty(text))
                {
                    PreviewTextHost.HostControl.Opacity = 0.3;
                    return;
                }

                int position = PreviewTextHost.TextView.TextViewLines.FirstVisibleLine.Extent.Start.Position;
                PreviewTextHost.TextView.TextBuffer.SetText(text);
                PreviewTextHost.HostControl.Opacity = 1;
                PreviewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, PreviewTextHost.TextView.TextSnapshot.GetLineNumberFromPosition(position));
                PreviewTextHost.TextView.ViewScroller.ScrollViewportHorizontallyByPixels(-9999);
            }
            catch
            {
                // Threading issues when called from TypeScript
            }
        }

        protected override void UpdateMargin(CompilerResult result)
        {
            if (result.IsSuccess)
            {
                SetText(result.Result);
            }
            else
                SetText("/*\r\n\r\nCompilation Error occurred (see error list to navigate to the error location):\r\n"
                      + string.Join("\r\n", result.Errors.Select(e => "Error found" + (e.Line > 0 ? " at line " + e.Line + (e.Column > 0 ? ", column " + e.Column : "") : "") + ":\r\n" + e.FullMessage))
                      + "\r\n\r\n*/");
        }

        private void OnMenuClicked(object sender, RoutedEventArgs e)
        {
            ContextMenuService.GetContextMenu(PreviewTextHost.TextView.VisualElement).IsOpen = true;
        }

        protected virtual void AddSpecialItems(ItemsControl menu) { }

        protected void SetupCommands(UIElement menu)
        {
            var copyCommand = new CommandBinding(ApplicationCommands.Copy,
                (sender, e) =>
                {
                    string contentString = string.Join(Environment.NewLine, PreviewTextHost.TextView.Selection.SelectedSpans.Select(s => s.GetText()));
                    Clipboard.SetDataObject(contentString);

                    e.Handled = true;
                },
                (sender, e) =>
                {
                    e.CanExecute = !PreviewTextHost.TextView.Selection.IsEmpty;

                    e.Handled = true;
                });

            var selectAllCommand = new CommandBinding(ApplicationCommands.SelectAll,
                (sender, e) =>
                {
                    PreviewTextHost.TextView.Selection.Select(new SnapshotSpan(PreviewTextHost.TextView.TextBuffer.CurrentSnapshot, 0, PreviewTextHost.TextView.TextBuffer.CurrentSnapshot.GetText().Length - 1), false);

                    e.Handled = true;
                },
                (sender, e) =>
                {
                    e.CanExecute = true;

                    e.Handled = true;
                });

            menu.CommandBindings.Add(copyCommand);
            menu.CommandBindings.Add(selectAllCommand);

            // For key gestures.
            PreviewTextHost.TextView.VisualElement.CommandBindings.Add(copyCommand);
            PreviewTextHost.TextView.VisualElement.CommandBindings.Add(selectAllCommand);
        }
    }
}
