using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Exports the Robots.txt content type and file extension
    /// </summary>
    public class RobotsTxtContentTypeDefinition
    {
        public const string RobotsTxtContentType = "robotstxt";

        /// <summary>
        /// Exports the Robots.txt content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(RobotsTxtContentType)]
        [BaseDefinition("text")]
        public ContentTypeDefinition IMarkdownContentType { get; set; }

        /// <summary>
        /// Exports the txt file extension
        /// </summary>
        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(RobotsTxtContentType)]
        [FileExtension(".txt")]
        public FileExtensionToContentTypeDefinition IRobotsTxtFileExtension { get; set; }
    }
}