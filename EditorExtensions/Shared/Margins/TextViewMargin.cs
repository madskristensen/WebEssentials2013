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
        protected IWpfTextViewHost _previewTextHost;
        protected IWpfTextView _sourceView;
        protected readonly string _previewContentType;
        protected string _targetFileName;

        public TextViewMargin(string targetContentType, ITextDocument document, IWpfTextView sourceView)
            : base(WESettings.Instance.ForContentType<IMarginSettings>(document.TextBuffer.ContentType), document)
        {
            _sourceView = sourceView;
            _previewContentType = targetContentType;
        }

        protected override FrameworkElement CreatePreviewControl()
        {
            _previewTextHost = CreateTextViewHost(_previewContentType);
            _previewTextHost.TextView.VisualElement.HorizontalAlignment = HorizontalAlignment.Stretch;
            _previewTextHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
            _previewTextHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
            _previewTextHost.TextView.VisualElement.KeyDown += TextView_KeyUp;
            _previewTextHost.TextView.VisualElement.ContextMenu = GetContextMenu();
            _previewTextHost.TextView.VisualElement.PreviewMouseRightButtonUp += new MouseButtonEventHandler(OnMenuClicked);

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
            if (e.Key == Key.PageDown)
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
            _targetFileName = null;

            if (result.IsSuccess)
            {
                _targetFileName = result.TargetFileName;
                SetText(result.Result);
            }
            else
                SetText("/*\r\n\r\nCompilation Error. \r\nSee error list for details\r\n"
                      + string.Join("\r\n", result.Errors.Select(e => e.Message))
                      + "\r\n\r\n*/");
        }

        private void OnMenuClicked(object sender, RoutedEventArgs e)
        {
            ContextMenuService.GetContextMenu(_previewTextHost.TextView.VisualElement).IsOpen = true;
        }

        protected virtual ContextMenu GetContextMenu()
        {
            var menu = new ContextMenu();

            SetupCommands(menu);

            menu.Items.Add(new MenuItem()
            {
                Command = ApplicationCommands.Copy,
                CommandTarget = menu
            });

            menu.Items.Add(new MenuItem()
            {
                Command = ApplicationCommands.SelectAll,
                CommandTarget = menu
            });

            return menu;
        }

        protected void SetupCommands(ContextMenu menu)
        {
            var copyCommand = new CommandBinding(ApplicationCommands.Copy,
                (sender, e) =>
                {
                    string contentString = string.Join(Environment.NewLine, _previewTextHost.TextView.Selection.SelectedSpans.Select(s => s.GetText()));
                    Clipboard.SetDataObject(contentString);

                    e.Handled = true;
                },
                (sender, e) =>
                {
                    e.CanExecute = !_previewTextHost.TextView.Selection.IsEmpty;

                    e.Handled = true;
                });

            var selectAllCommand = new CommandBinding(ApplicationCommands.SelectAll,
                (sender, e) =>
                {
                    _previewTextHost.TextView.Selection.Select(new SnapshotSpan(_previewTextHost.TextView.TextBuffer.CurrentSnapshot, 0, _previewTextHost.TextView.TextBuffer.CurrentSnapshot.GetText().Length - 1), false);

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
            _previewTextHost.TextView.VisualElement.CommandBindings.Add(copyCommand);
            _previewTextHost.TextView.VisualElement.CommandBindings.Add(selectAllCommand);
        }
    }
}
