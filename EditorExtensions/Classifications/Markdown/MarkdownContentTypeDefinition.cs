using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Exports the ScSS content type and file extension
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

        /// <summary>
        /// Exports the markdown file extension
        /// </summary>
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
