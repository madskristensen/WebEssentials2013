using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using MadsKristensen.EditorExtensions.Images;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Html
{
    [Export(typeof(IDropHandlerProvider))]
    [DropFormat("FileDrop")]
    [DropFormat("CF_VSSTGPROJECTITEMS")]
    [Name("MarkdownImageDropHandler")]
    // Issue #1745: https://github.com/madskristensen/WebEssentials2013/issues/1745
    // Since the functionality for html is now provided out of the box by Visual Studio 
    // this handler can handle only Markdown.
    [ContentType(Markdown.MarkdownContentTypeDefinition.MarkdownContentType)]
    // Visual studio Update 4 provide the HtmlViewFileDropHandlerProvider to 
    // handle drag & drop images into html documents out of the box.
    // Adding [Order(Before = "HtmlViewFileDropHandlerProvider")] will ensure 
    // that this provider is inserted BEFORE both the HtmlViewFileDropHandlerProvider 
    // AND the DefaultFileDropHandler. 
    // DO NOT REMOVE THIS!
    // It may seem uncecessary since now the ContentType has been set explicitly to Markdown, 
    // but apparently visual studio get confused by content type inheritance or does not 
    // handle it correcly.
    [Order(Before = "DefaultFileDropHandler")]
    [Order(Before = "HtmlViewFileDropHandlerProvider")]
    public class MarkdownImageDropHandlerProvider : IDropHandlerProvider
    {
        public IDropHandler GetAssociatedDropHandler(IWpfTextView wpfTextView)
        {
            return wpfTextView.Properties.GetOrCreateSingletonProperty(() => new MarkdownImageDropHandler(wpfTextView));
        }
    }

    public class MarkdownImageDropHandler : IDropHandler
    {
        static readonly HashSet<string> _imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".bmp", ".png", ".gif", ".svg", ".tif", ".tiff" };

        readonly IWpfTextView _view;

        string _imageFilename;

        public MarkdownImageDropHandler(IWpfTextView view)
        {
            _view = view;
        }

        //[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "img")]
        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            _view.InsertLinkToImageFile(_imageFilename);
            return DragDropPointerEffects.Link;
        }

        public void HandleDragCanceled() { }
        public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo) { return DragDropPointerEffects.Link; }
        public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo) { return DragDropPointerEffects.Link; }

        public bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            _imageFilename = dragDropInfo.GetFilePath();

            if (string.IsNullOrEmpty(_imageFilename))
                return false;

            if (_imageExtensions.Contains(Path.GetExtension(_imageFilename)))
                return true;

            return false;
        }
    }
}
