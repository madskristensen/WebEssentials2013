using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

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
        private readonly List<string> _imageExtensions = new List<string> { ".css", ".less", ".sass", ".scss" };
        private string _imageFilename;
        string _background = "@import url('{0}');";

        public StylesheetDropHandler(IWpfTextView view)
        {
            this._view = view;
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            string reference = FileHelpers.RelativePath(EditorExtensionsPackage.DTE.ActiveDocument.FullName, _imageFilename);

            if (reference.StartsWith("http://localhost:"))
            {
                int index = reference.IndexOf('/', 24);
                if (index > -1)
                    reference = reference.Substring(index).ToLowerInvariant();
            }

            _view.TextBuffer.Insert(dragDropInfo.VirtualBufferPosition.Position.Position, string.Format(_background, reference));

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
            _imageFilename = FontDropHandler.GetImageFilename(dragDropInfo);

            if (!string.IsNullOrEmpty(_imageFilename))
            {
                string fileExtension = Path.GetExtension(_imageFilename).ToLowerInvariant();
                if (this._imageExtensions.Contains(fileExtension))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
