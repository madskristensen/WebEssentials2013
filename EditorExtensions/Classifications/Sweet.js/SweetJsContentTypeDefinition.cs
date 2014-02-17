using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Classifications.Sweet.js
{
    public class SweetJsContentTypeDefinition
    {
        public const string SweetJsContentType = "sweetjs";

        /// <summary>
        /// Exports the WebVTT HTML content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(SweetJsContentType)]
        [BaseDefinition("plaintext")]
        public ContentTypeDefinition ISweetJsContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(SweetJsContentType)]
        [FileExtension(".sjs")]
        public FileExtensionToContentTypeDefinition SweetJsFileExtension { get; set; }

    }
}
