using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Web;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IDropHandlerProvider))]
    [DropFormat("StylesheetDrop")]
    [DropFormat("CF_VSSTGPROJECTITEMS")]
    [Name("StylesheetDropDropHandler")]
    [ContentType("CSS")]
    [ContentType("LESS")]
    [Order(Before = "DefaultFileDropHandler")]
    internal class StylesheetDropHandlerProvider : IDropHandlerProvider
    {
        public IDropHandler GetAssociatedDropHandler(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty<StylesheetDropHandler>(() => new StylesheetDropHandler(view));
        }
    }

    internal class StylesheetDropHandler : IDropHandler
    {
        IWpfTextView _view;
        private readonly HashSet<string> _allowedFileExtensions = new HashSet<string> { ".css", ".less", ".sass", ".scss" };
        private string _filename;
        private string _targetFileName;
        const string _cssImport = "@import url('{0}');";
        const string _lessImport = "@import '{0}';";

        public StylesheetDropHandler(IWpfTextView view)
        {
            this._view = view;
            _targetFileName = Microsoft.Web.Editor.TextBufferExtensions.GetFileName(view.TextBuffer);
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            string reference = FileHelpers.RelativePath(EditorExtensionsPackage.DTE.ActiveDocument.FullName, _filename);

            if (reference.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase))
            {
                reference = Path.Combine(ProjectHelpers.GetRootFolder(),
                    Path.Combine(new Uri(reference).Segments).TrimStart('/'));
            }

            reference = HttpUtility.UrlPathEncode(FileHelpers.RelativePath(_targetFileName, reference));

            string import = Path.GetExtension(_filename).Equals(".less", StringComparison.OrdinalIgnoreCase) ? _lessImport : _cssImport;
            _view.TextBuffer.Insert(dragDropInfo.VirtualBufferPosition.Position.Position, string.Format(CultureInfo.CurrentCulture, import, reference));

            return DragDropPointerEffects.Copy;
        }

        public void HandleDragCanceled()
        {

        }

        public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            _filename = FontDropHandler.GetImageFilename(dragDropInfo);

            if (string.IsNullOrEmpty(_filename))
                return false;

            return this._allowedFileExtensions.Contains(Path.GetExtension(_filename));
        }
    }
}
