using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        private readonly HashSet<string> _imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".css", ".less", ".sass", ".scss" };
        private string _filename;
        const string _cssImport = "@import url('{0}');";
        const string _lessImport = "@import '{0}';";

        public StylesheetDropHandler(IWpfTextView view)
        {
            this._view = view;
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            string reference = FileHelpers.RelativePath(EditorExtensionsPackage.DTE.ActiveDocument.FullName, _filename);

            if (reference.StartsWith("http://localhost:"))
            {
                int index = reference.IndexOf('/', 24);
                if (index > -1)
                    reference = reference.Substring(index).ToLowerInvariant();
            }
            reference = HttpUtility.UrlPathEncode(reference);

            string import = Path.GetExtension(_filename).Equals(".less", StringComparison.OrdinalIgnoreCase) ? _lessImport : _cssImport;
            _view.TextBuffer.Insert(dragDropInfo.VirtualBufferPosition.Position.Position, string.Format(import, reference));

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

            return this._imageExtensions.Contains(Path.GetExtension(_filename));
        }
    }
}
