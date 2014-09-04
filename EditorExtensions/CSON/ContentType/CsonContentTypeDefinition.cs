using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.CoffeeScript
{
    /// <summary>
    /// Exports the CSON content type and file extension
    /// </summary>
    public class CsonContentTypeDefinition
    {
        public const string CsonContentType = "CSON";

        /// <summary>
        /// Exports the CSON content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(CsonContentType)]
        [BaseDefinition("CoffeeScript")]
        public ContentTypeDefinition ICsonContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(CsonContentType)]
        [FileExtension(".cson")]
        public FileExtensionToContentTypeDefinition CsonFileExtension { get; set; }
    }
}
