using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Exports the Markdown content type and file extension
    /// </summary>
    public class MarkdownContentTypeDefinition
    {
        public const string MarkdownContentType = "markdown";

        /// <summary>
        /// Exports the Markdown HTML content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(MarkdownContentType)]
        [BaseDefinition("htmlx")]
        public ContentTypeDefinition IMarkdownContentType { get; set; }

        // All of these extensions must also be registered in registry.pkgdef.
        // See https://twitter.com/Schabse/status/390280700043472896 and http://blogs.msdn.com/b/noahric/archive/2010/03/01/new-extension-css-is-less.aspx

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(MarkdownContentType)]
        [FileExtension(".md")]
        public FileExtensionToContentTypeDefinition IMDFileExtension { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(MarkdownContentType)]
        [FileExtension(".mdown")]
        public FileExtensionToContentTypeDefinition IMDownFileExtension { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(MarkdownContentType)]
        [FileExtension(".markdown")]
        public FileExtensionToContentTypeDefinition IMarkDownFileExtension { get; set; }
    }
}
