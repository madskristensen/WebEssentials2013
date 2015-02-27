using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions.RazorZen
{
    public class RazorZenContentTypeDefinition
    {
        public const string RazorZenContentType = "RazorZen";

        /// <summary>
        /// Exports the RazorZen HTML content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(RazorZenContentType)]
        [BaseDefinition("plaintext")]
        public ContentTypeDefinition IRazorZenContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(RazorZenContentType)]
        [FileExtension(".cszen")]
        public FileExtensionToContentTypeDefinition RazorZenFileExtension { get; set; }
    }
}