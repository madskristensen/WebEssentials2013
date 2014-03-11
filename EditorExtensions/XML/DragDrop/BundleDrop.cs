using System;
using System.ComponentModel.Composition;
using System.Globalization;
using MadsKristensen.EditorExtensions.Css;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IDropHandlerProvider))]
    [DropFormat("CF_VSSTGPROJECTITEMS")]
    [Name("BundleDropDropHandler")]
    [ContentType("XML")]
    [Order(Before = "DefaultFileDropHandler")]
    internal class BundleDropHandlerProvider : IDropHandlerProvider
    {
        public IDropHandler GetAssociatedDropHandler(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty<BundleDropHandler>(() => new BundleDropHandler(view));
        }
    }

    internal class BundleDropHandler : IDropHandler
    {
        private IWpfTextView _view;
        private string _draggedFilename;
        private string _format = Environment.NewLine + "\t<file>/{0}</file>";

        public BundleDropHandler(IWpfTextView view)
        {
            this._view = view;
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            string reference = FileHelpers.RelativePath(ProjectHelpers.GetRootFolder(), _draggedFilename);

            if (reference.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase))
            {
                int index = reference.IndexOf('/', 20);
                if (index > -1)
                    reference = reference.Substring(index + 1).ToLowerInvariant();
            }

            _view.TextBuffer.Insert(dragDropInfo.VirtualBufferPosition.Position.Position, string.Format(CultureInfo.CurrentCulture, _format, reference));

            return DragDropPointerEffects.Copy;
        }

        public void HandleDragCanceled() { }

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
            _draggedFilename = FontDropHandler.GetImageFilename(dragDropInfo);

            return true;
        }
    }
}