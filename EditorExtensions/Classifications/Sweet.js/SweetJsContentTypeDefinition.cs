using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    public class SweetJsContentTypeDefinition
    {
        public const string SweetJsContentType = "sweetjs";

        /// <summary>
        /// Exports the Sweet.js content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(SweetJsContentType)]
        [BaseDefinition("JavaScript")]
        public ContentTypeDefinition ISweetJsContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(SweetJsContentType)]
        [FileExtension(".sjs")]
        public FileExtensionToContentTypeDefinition SweetJsFileExtension { get; set; }

    }
}
