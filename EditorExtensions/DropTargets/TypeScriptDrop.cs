using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IDropHandlerProvider))]
    [DropFormat("CF_VSSTGPROJECTITEMS")]
    [Name("TypeScriptDropHandler")]
    [ContentType("TypeScript")]
    [Order(Before = "DefaultFileDropHandler")]
    internal class TypeScriptDropHandlerProvider : IDropHandlerProvider
    {
        public IDropHandler GetAssociatedDropHandler(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty<TypeScriptDropHandler>(() => new TypeScriptDropHandler(view));
        }
    }

    internal class TypeScriptDropHandler : IDropHandler
    {
        IWpfTextView _view;
        private readonly List<string> _imageExtensions = new List<string> { ".ts", ".js" };
        private string _imageFilename;
        string _background = "/// <reference path=\"{0}\" />";

        public TypeScriptDropHandler(IWpfTextView view)
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

            reference = reference.Trim('/');
            string comment = string.Format(_background, reference);

            _view.TextBuffer.Insert(0, comment + Environment.NewLine);

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